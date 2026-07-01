namespace JiraIntegrationService.Api.Application.Configuration.Models;

public sealed record FieldMappingConfig(
    int Id,
    int ProductId,
    int? IssueTypeMappingId,
    string SourceField,
    string JiraField,
    bool IsRequired,
    string? DefaultValue,
    string ValueType = "string",
    string ValueShape = "raw",
    int SortOrder = 0,
    string? TransformConfigJson = null,
    string? JiraFieldName = null,
    string? JiraFieldDescription = null,
    string? JiraSchemaType = null,
    string? JiraSchemaItems = null,
    string? JiraAllowedValuesJson = null,
    string? JiraDefaultValueJson = null)
{
    public string SourcePath => SourceField;
}
