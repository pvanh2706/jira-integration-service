namespace JiraIntegrationService.Api.Common;

public class ApiException : Exception
{
    public ApiException(string errorCode, string message, int statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public int StatusCode { get; }
}
