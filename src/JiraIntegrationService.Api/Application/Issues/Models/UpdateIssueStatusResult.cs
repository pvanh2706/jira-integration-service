namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed record UpdateIssueStatusResult(
    string? JiraIssueId,
    string? JiraIssueKey,
    string StandardStatus);
