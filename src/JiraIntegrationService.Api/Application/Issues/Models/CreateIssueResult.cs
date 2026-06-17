namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed record CreateIssueResult(
    string JiraIssueId,
    string JiraIssueKey);
