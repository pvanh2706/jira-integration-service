namespace JiraIntegrationService.Api.Application.Configuration.Models;

public sealed record StatusTransitionConfig(
    int Id,
    int ProductId,
    int? IssueTypeMappingId,
    string StandardStatus,
    string JiraStatusName,
    string? JiraTransitionId,
    string? JiraTransitionName);
