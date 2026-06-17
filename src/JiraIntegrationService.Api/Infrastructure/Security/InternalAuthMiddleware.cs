using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Options;
using Microsoft.Extensions.Options;

namespace JiraIntegrationService.Api.Infrastructure.Security;

public sealed class InternalAuthMiddleware
{
    public const string HeaderName = "X-Internal-Auth";

    private const string UnauthorizedMessage = "Unauthorized request.";

    private readonly RequestDelegate _next;
    private readonly InternalAuthOptions _options;
    private readonly ILogger<InternalAuthMiddleware> _logger;

    public InternalAuthMiddleware(
        RequestDelegate next,
        IOptions<InternalAuthOptions> options,
        ILogger<InternalAuthMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldBypass(context))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogError(
                "Internal auth token is not configured. ErrorCode: {ErrorCode}. TraceId: {TraceId}",
                ErrorCodes.AuthError,
                TraceId.From(context));
            await WriteUnauthorizedAsync(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedToken)
            || providedToken.Count != 1
            || !string.Equals(providedToken[0], _options.Token, StringComparison.Ordinal))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool ShouldBypass(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var response = new ApiErrorResponse(
            ErrorCodes.AuthError,
            UnauthorizedMessage,
            TraceId.From(context));

        await context.Response.WriteAsJsonAsync(response);
    }
}
