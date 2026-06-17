namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record CreateJiraIssueResponse(
    string Id,
    string Key);
