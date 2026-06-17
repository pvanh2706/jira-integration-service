using System.Text.Json;

namespace JiraIntegrationService.Api.Common;

public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException exception)
        {
            var traceId = TraceId.From(context);

            _logger.LogWarning(
                exception,
                "Handled API exception. ErrorCode: {ErrorCode}. TraceId: {TraceId}",
                exception.ErrorCode,
                traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse(
                exception.ErrorCode,
                exception.Message,
                traceId);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception exception)
        {
            var traceId = TraceId.From(context);

            _logger.LogError(
                exception,
                "Unhandled exception. ErrorCode: {ErrorCode}. TraceId: {TraceId}",
                ErrorCodes.InternalError,
                traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse(
                ErrorCodes.InternalError,
                "An unexpected error occurred.",
                traceId);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
