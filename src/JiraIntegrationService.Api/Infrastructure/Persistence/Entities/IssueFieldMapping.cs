namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class IssueFieldMapping
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int? IssueTypeMappingId { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public string JiraField { get; set; } = string.Empty;

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
