using System.Net;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JiraIntegrationService.Tests.Common;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenApiException_ShouldReturnErrorCodeFromException()
    {
        static Task ThrowHandledException(HttpContext _)
        {
            throw new ConfigNotFoundException("Config was not found.");
        }

        var logger = new CapturingLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(ThrowHandledException, logger);

        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-config"
        };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        var root = json.RootElement;

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(ErrorCodes.ConfigNotFound, root.GetProperty("errorCode").GetString());
        Assert.Equal("Config was not found.", root.GetProperty("message").GetString());
        Assert.Equal("trace-config", root.GetProperty("traceId").GetString());
        Assert.Contains(logger.Entries, entry =>
            entry.Properties.TryGetValue("ErrorCode", out var errorCode)
            && errorCode?.ToString() == ErrorCodes.ConfigNotFound
            && entry.Properties.TryGetValue("TraceId", out var traceId)
            && traceId?.ToString() == "trace-config");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturnStandardErrorResponse()
    {
        static Task ThrowUnhandledException(HttpContext _)
        {
            throw new InvalidOperationException("Boom");
        }

        var logger = new CapturingLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(ThrowUnhandledException, logger);

        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-exception"
        };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        var root = json.RootElement;

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(ErrorCodes.InternalError, root.GetProperty("errorCode").GetString());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("message").GetString());
        Assert.Equal("trace-exception", root.GetProperty("traceId").GetString());
        Assert.Contains(logger.Entries, entry =>
            entry.Properties.TryGetValue("ErrorCode", out var errorCode)
            && errorCode?.ToString() == ErrorCodes.InternalError
            && entry.Properties.TryGetValue("TraceId", out var traceId)
            && traceId?.ToString() == "trace-exception");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<CapturedLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IEnumerable<KeyValuePair<string, object?>>;

            Entries.Add(new CapturedLogEntry(
                logLevel,
                properties?.ToDictionary(
                    property => property.Key,
                    property => property.Value)
                ?? new Dictionary<string, object?>()));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record CapturedLogEntry(
        LogLevel LogLevel,
        IReadOnlyDictionary<string, object?> Properties);
}
