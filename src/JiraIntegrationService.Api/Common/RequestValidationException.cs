namespace JiraIntegrationService.Api.Common;

public sealed class RequestValidationException : ApiException
{
    public RequestValidationException(string message)
        : base(ErrorCodes.ValidationError, message, StatusCodes.Status400BadRequest)
    {
    }
}
