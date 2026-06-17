namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record JiraConnectionConfig(
    string BaseUrl,
    string ApiBasePath,
    string Version,
    string AuthType,
    string Username,
    string PasswordOrToken);
