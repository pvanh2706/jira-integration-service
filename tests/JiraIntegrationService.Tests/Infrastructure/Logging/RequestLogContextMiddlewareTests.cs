using JiraIntegrationService.Api.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;

namespace JiraIntegrationService.Tests.Infrastructure.Logging;

public sealed class RequestLogContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetTraceIdItemBeforeCallingNextMiddleware()
    {
        var nextWasCalled = false;
        var middleware = new RequestLogContextMiddleware(context =>
        {
            nextWasCalled = true;

            Assert.True(context.Items.TryGetValue(
                RequestLogContextMiddleware.TraceIdItemKey,
                out var traceId));
            Assert.Equal("trace-log", traceId);

            return Task.CompletedTask;
        });
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-log"
        };

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextWasCalled);
    }
}
