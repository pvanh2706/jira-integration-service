namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record TransitionJiraIssueRequest(
    string? JiraIssueId,
    string? JiraIssueKey,
    string TransitionId);
