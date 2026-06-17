using System.Net;
using System.Text.Json;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Security;
using JiraIntegrationService.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JiraIntegrationService.Tests.Infrastructure.Security;

public sealed class InternalAuthMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenHealthRequest_ShouldBypassAuth()
    {
        var nextWasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/health", HttpMethods.Get);

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenIsMissing_ShouldReturnAuthError()
    {
        var nextWasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/issues/create", HttpMethods.Post);

        await middleware.InvokeAsync(context);

        Assert.False(nextWasCalled);
        await AssertAuthErrorResponseAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenIsWrong_ShouldReturnAuthError()
    {
        var nextWasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/issues/create", HttpMethods.Post);
        context.Request.Headers[InternalAuthMiddleware.HeaderName] = "wrong-token";

        await middleware.InvokeAsync(context);

        Assert.False(nextWasCalled);
        await AssertAuthErrorResponseAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenIsCorrect_ShouldCallNextMiddleware()
    {
        var nextWasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/issues/create", HttpMethods.Post);
        context.Request.Headers[InternalAuthMiddleware.HeaderName] = "secret-token";

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsNotApi_ShouldBypassAuth()
    {
        var nextWasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/products/CRM", HttpMethods.Get);

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static InternalAuthMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new InternalAuthMiddleware(
            next,
            Options.Create(new InternalAuthOptions { Token = "secret-token" }),
            NullLogger<InternalAuthMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path, string method)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-auth"
        };
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task AssertAuthErrorResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        var root = json.RootElement;

        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(ErrorCodes.AuthError, root.GetProperty("errorCode").GetString());
        Assert.Equal("Unauthorized request.", root.GetProperty("message").GetString());
        Assert.Equal("trace-auth", root.GetProperty("traceId").GetString());
    }
}
