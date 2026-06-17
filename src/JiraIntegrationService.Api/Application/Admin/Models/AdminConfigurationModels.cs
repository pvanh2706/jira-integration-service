using System.ComponentModel.DataAnnotations;

namespace JiraIntegrationService.Api.Application.Admin.Models;

public sealed class CreateProductAdminRequest
{
    [Required]
    public string? Code { get; init; }

    [Required]
    public string? Name { get; init; }

    [Required]
    public string? JiraProjectKey { get; init; }

    [Required]
    public string? JiraBaseUrl { get; init; }

    public string? JiraApiBasePath { get; init; }

    public string? JiraVersion { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateProductAdminRequest
{
    [Required]
    public string? Name { get; init; }

    [Required]
    public string? JiraProjectKey { get; init; }

    [Required]
    public string? JiraBaseUrl { get; init; }

    public string? JiraApiBasePath { get; init; }

    public string? JiraVersion { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record ProductAdminResponse(
    int Id,
    string Code,
    string Name,
    string JiraProjectKey,
    string JiraBaseUrl,
    string JiraApiBasePath,
    string JiraVersion,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class UpsertJiraCredentialAdminRequest
{
    public string? AuthType { get; init; }

    [Required]
    public string? Username { get; init; }

    [Required]
    public string? PasswordOrToken { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record JiraCredentialAdminResponse(
    int Id,
    int ProductId,
    string AuthType,
    string Username,
    bool HasPasswordOrToken,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class CreateIssueTypeMappingAdminRequest
{
    [Required]
    public string? IssueTypeCode { get; init; }

    public string? JiraIssueTypeId { get; init; }

    public string? JiraIssueTypeName { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateIssueTypeMappingAdminRequest
{
    public string? JiraIssueTypeId { get; init; }

    public string? JiraIssueTypeName { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record IssueTypeMappingAdminResponse(
    int Id,
    int ProductId,
    string IssueTypeCode,
    string? JiraIssueTypeId,
    string? JiraIssueTypeName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class UpsertIssueFieldMappingAdminRequest
{
    [Required]
    public string? SourcePath { get; init; }

    [Required]
    public string? JiraField { get; init; }

    public string? ValueType { get; init; }

    public string? ValueShape { get; init; }

    public bool IsRequired { get; init; }

    public string? DefaultValue { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;

    public string? TransformConfigJson { get; init; }
}

public sealed record IssueFieldMappingAdminResponse(
    int Id,
    int ProductId,
    int? IssueTypeMappingId,
    string SourcePath,
    string JiraField,
    string ValueType,
    string ValueShape,
    bool IsRequired,
    string? DefaultValue,
    int SortOrder,
    bool IsActive,
    string? TransformConfigJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class UpsertStatusMappingAdminRequest
{
    [Required]
    public string? StandardStatus { get; init; }

    [Required]
    public string? JiraStatusName { get; init; }

    public string? JiraTransitionId { get; init; }

    public string? JiraTransitionName { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record StatusMappingAdminResponse(
    int Id,
    int ProductId,
    int? IssueTypeMappingId,
    string StandardStatus,
    string JiraStatusName,
    string? JiraTransitionId,
    string? JiraTransitionName,
    bool IsActive);

public sealed class ValidateCreateIssueConfigAdminRequest
{
    public string? IssueTypeCode { get; init; }
}

public sealed record ValidateCreateIssueConfigAdminResponse(
    string ProductCode,
    string? IssueTypeCode,
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record DeleteAdminResponse(bool Deleted);
