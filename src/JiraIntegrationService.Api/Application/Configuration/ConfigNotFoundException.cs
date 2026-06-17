using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Application.Configuration;

public sealed class ConfigNotFoundException : ApiException
{
    public ConfigNotFoundException(string message)
        : base(ErrorCodes.ConfigNotFound, message, StatusCodes.Status404NotFound)
    {
    }
}
