using JiraIntegrationService.Api.Application.Admin.Models;

namespace JiraIntegrationService.Api.Application.Admin;

public interface IAdminConfigurationService
{
    Task<IReadOnlyList<ProductAdminResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default);

    Task<ProductAdminResponse> GetProductAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<ProductAdminResponse> CreateProductAsync(
        CreateProductAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductAdminResponse> UpdateProductAsync(
        string code,
        UpdateProductAdminRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteProductAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<JiraCredentialAdminResponse> GetCredentialAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    Task<JiraCredentialAdminResponse> UpsertCredentialAsync(
        string productCode,
        UpsertJiraCredentialAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IssueTypeMappingAdminResponse>> GetIssueTypesAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    Task<IssueTypeMappingAdminResponse> CreateIssueTypeAsync(
        string productCode,
        CreateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<IssueTypeMappingAdminResponse> UpdateIssueTypeAsync(
        string productCode,
        string issueTypeCode,
        UpdateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IssueFieldMappingAdminResponse>> GetFieldMappingsAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default);

    Task<IssueFieldMappingAdminResponse> CreateFieldMappingAsync(
        string productCode,
        string issueTypeCode,
        UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<IssueFieldMappingAdminResponse> UpdateFieldMappingAsync(
        int id,
        UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteFieldMappingAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusMappingAdminResponse>> GetStatusMappingsAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default);

    Task<StatusMappingAdminResponse> CreateStatusMappingAsync(
        string productCode,
        string issueTypeCode,
        UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<StatusMappingAdminResponse> UpdateStatusMappingAsync(
        int id,
        UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteStatusMappingAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ValidateCreateIssueConfigAdminResponse> ValidateCreateIssueConfigAsync(
        string productCode,
        ValidateCreateIssueConfigAdminRequest request,
        CancellationToken cancellationToken = default);
}
