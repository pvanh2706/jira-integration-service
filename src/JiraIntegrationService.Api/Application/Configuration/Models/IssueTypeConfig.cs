namespace JiraIntegrationService.Api.Application.Configuration.Models;

public sealed record IssueTypeConfig(
    int Id,
    int ProductId,
    string IssueTypeCode,
    string JiraIssueTypeName,
    string? JiraIssueTypeId = null);
