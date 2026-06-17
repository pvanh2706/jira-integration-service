namespace JiraIntegrationService.Api.Common;

public sealed class ApiErrorResponse
{
    public ApiErrorResponse(string errorCode, string message, string traceId)
    {
        ErrorCode = errorCode;
        Message = message;
        TraceId = traceId;
    }

    public bool Success => false;

    public string ErrorCode { get; }

    public string Message { get; }

    public string TraceId { get; }
}
