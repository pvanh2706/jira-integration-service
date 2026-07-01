namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class IssueFieldMapping
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int? IssueTypeMappingId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string JiraField { get; set; } = string.Empty;

    public string? JiraFieldName { get; set; }

    public string? JiraFieldDescription { get; set; }

    public string? JiraSchemaType { get; set; }

    public string? JiraSchemaItems { get; set; }

    public string? JiraSchemaSystem { get; set; }

    public string? JiraSchemaCustom { get; set; }

    public string? JiraAllowedValuesJson { get; set; }

    public string? JiraDefaultValueJson { get; set; }

    public string? JiraAutoCompleteUrl { get; set; }

    public string ValueType { get; set; } = string.Empty;

    public string ValueShape { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string? DefaultValue { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public string? TransformConfigJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public IssueTypeMapping? IssueTypeMapping { get; set; }
}
