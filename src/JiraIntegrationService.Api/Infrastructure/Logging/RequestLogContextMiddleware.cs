using JiraIntegrationService.Api.Common;
using Serilog.Context;

namespace JiraIntegrationService.Api.Infrastructure.Logging;

public sealed class RequestLogContextMiddleware
{
    public const string TraceIdItemKey = "TraceId";

    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = TraceId.From(context);
        context.Items[TraceIdItemKey] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            await _next(context);
        }
    }
}
