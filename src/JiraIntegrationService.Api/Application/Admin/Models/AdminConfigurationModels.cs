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

public sealed record SyncIssueTypesAdminResponse(
    string ProductCode,
    int Total,
    IReadOnlyList<IssueTypeMappingAdminResponse> IssueTypes);

public sealed record JiraFieldMetadataAdminResponse(
    string FieldId,
    string Name,
    bool Required,
    string? SchemaType,
    string? SchemaItems,
    string? SchemaSystem,
    string? SchemaCustom,
    int? SchemaCustomId,
    bool HasDefaultValue,
    string? DefaultValueJson,
    string? AutoCompleteUrl,
    IReadOnlyList<string> Operations,
    IReadOnlyList<JiraAllowedValueAdminResponse> AllowedValues,
    string RecommendedValueType,
    string RecommendedValueShape,
    DateTime? UpdatedAt);

public sealed record JiraFieldsMetadataAdminResponse(
    string ProductCode,
    string IssueTypeCode,
    DateTime? UpdatedAt,
    int Total,
    IReadOnlyList<JiraFieldMetadataAdminResponse> Fields);

public sealed record JiraAllowedValueAdminResponse(
    string? Id,
    string? Key,
    string? Name,
    string? Value,
    string? Description,
    bool Disabled,
    string RawJson);

public sealed class CreateFieldMappingTemplateAdminRequest
{
    [Required]
    public string? TemplateCode { get; init; }

    [Required]
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? SourceTemplateCode { get; init; }

    public bool CopyMappings { get; init; } = true;

    public bool IsActive { get; init; } = true;
}

public sealed record FieldMappingTemplateAdminResponse(
    int Id,
    int ProductId,
    int IssueTypeMappingId,
    string TemplateCode,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    int MappingCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class UpsertIssueFieldMappingAdminRequest
{
    [Required]
    public string? SourcePath { get; init; }

    [Required]
    public string? JiraField { get; init; }

    public string? JiraFieldName { get; init; }

    public string? JiraFieldDescription { get; init; }

    public string? JiraSchemaType { get; init; }

    public string? JiraSchemaItems { get; init; }

    public string? JiraSchemaSystem { get; init; }

    public string? JiraSchemaCustom { get; init; }

    public string? JiraAllowedValuesJson { get; init; }

    public string? JiraDefaultValueJson { get; init; }

    public string? JiraAutoCompleteUrl { get; init; }

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
    string TemplateCode,
    string SourcePath,
    string JiraField,
    string? JiraFieldName,
    string? JiraFieldDescription,
    string? JiraSchemaType,
    string? JiraSchemaItems,
    string? JiraSchemaSystem,
    string? JiraSchemaCustom,
    string? JiraAllowedValuesJson,
    string? JiraDefaultValueJson,
    string? JiraAutoCompleteUrl,
    string ValueType,
    string ValueShape,
    bool IsRequired,
    string? DefaultValue,
    int SortOrder,
    bool IsActive,
    string? TransformConfigJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SetDefaultFieldMappingsAdminResponse(
    string ProductCode,
    string IssueTypeCode,
    int Total,
    IReadOnlyList<IssueFieldMappingAdminResponse> FieldMappings);

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
