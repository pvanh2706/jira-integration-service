using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Application.Jira;

public sealed class JiraClientException : ApiException
{
    public JiraClientException(string message)
        : base(ErrorCodes.JiraError, message, StatusCodes.Status502BadGateway)
    {
    }
}
