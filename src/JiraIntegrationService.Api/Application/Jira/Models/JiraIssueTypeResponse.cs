namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record JiraIssueTypeResponse(
    string Id,
    string Name,
    string? Description,
    bool Subtask);
