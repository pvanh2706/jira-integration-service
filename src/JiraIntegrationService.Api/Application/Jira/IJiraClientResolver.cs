namespace JiraIntegrationService.Api.Application.Jira;

public interface IJiraClientResolver
{
    IJiraClient Resolve(string jiraVersion);
}
