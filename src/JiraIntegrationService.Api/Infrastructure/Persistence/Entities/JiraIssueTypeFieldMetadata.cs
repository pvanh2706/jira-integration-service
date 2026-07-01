namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class JiraIssueTypeFieldMetadata
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int IssueTypeMappingId { get; set; }

    public string FieldId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool Required { get; set; }

    public string? SchemaType { get; set; }

    public string? SchemaItems { get; set; }

    public string? SchemaSystem { get; set; }

    public string? SchemaCustom { get; set; }

    public int? SchemaCustomId { get; set; }

    public bool HasDefaultValue { get; set; }

    public string? DefaultValueJson { get; set; }

    public string? AutoCompleteUrl { get; set; }

    public string? OperationsJson { get; set; }

    public string? AllowedValuesJson { get; set; }

    public string RecommendedValueType { get; set; } = string.Empty;

    public string RecommendedValueShape { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public IssueTypeMapping IssueTypeMapping { get; set; } = null!;
}
