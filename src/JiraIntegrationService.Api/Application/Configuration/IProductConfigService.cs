using JiraIntegrationService.Api.Application.Configuration.Models;

namespace JiraIntegrationService.Api.Application.Configuration;

public interface IProductConfigService
{
    Task<ProductConfig> GetProductAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    Task<JiraCredentialConfig> GetJiraCredentialAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    Task<IssueTypeConfig> GetIssueTypeAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsAsync(
        string productCode,
        string? issueTypeCode,
        CancellationToken cancellationToken = default);

    Task<StatusTransitionConfig> GetStatusTransitionAsync(
        string productCode,
        string? issueTypeCode,
        string standardStatus,
        CancellationToken cancellationToken = default);

    Task<string> MapJiraStatusToStandardStatusAsync(
        string productCode,
        string? issueTypeCode,
        string jiraStatusName,
        CancellationToken cancellationToken = default);
}
