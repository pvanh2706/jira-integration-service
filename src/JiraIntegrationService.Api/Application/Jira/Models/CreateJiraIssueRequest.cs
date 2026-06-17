namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record CreateJiraIssueRequest(
    string ProjectKey,
    string? IssueTypeName,
    string Summary,
    string? Description = null,
    string? PriorityName = null,
    string? ReporterName = null,
    string? AssigneeName = null,
    IReadOnlyDictionary<string, object?>? CustomFields = null,
    string? IssueTypeId = null,
    string? ParentKey = null,
    IReadOnlyList<string>? ComponentIds = null,
    IReadOnlyList<JiraWorklogEntry>? Worklogs = null);

public sealed record JiraWorklogEntry(
    string Started,
    string TimeSpent,
    string? Comment = null);
