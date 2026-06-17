using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Infrastructure.Jira;

public sealed class JiraClientResolver : IJiraClientResolver
{
    private readonly IJiraClient _serverV2Client;

    public JiraClientResolver(IJiraClient serverV2Client)
    {
        _serverV2Client = serverV2Client;
    }

    public IJiraClient Resolve(string jiraVersion)
    {
        if (string.Equals(jiraVersion, "ServerV2", StringComparison.OrdinalIgnoreCase))
        {
            return _serverV2Client;
        }

        throw new RequestValidationException($"Jira version '{jiraVersion}' is not supported.");
    }
}
