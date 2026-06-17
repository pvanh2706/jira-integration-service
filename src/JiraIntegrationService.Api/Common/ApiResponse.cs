namespace JiraIntegrationService.Api.Common;

public sealed class ApiResponse<T>
{
    private ApiResponse(T data, string traceId)
    {
        Success = true;
        Data = data;
        TraceId = traceId;
    }

    public bool Success { get; }

    public T Data { get; }

    public string TraceId { get; }

    public static ApiResponse<T> Ok(T data, string traceId)
    {
        return new ApiResponse<T>(data, traceId);
    }
}
