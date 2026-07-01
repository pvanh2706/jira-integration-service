namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class IssueTypeMapping
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string IssueTypeCode { get; set; } = string.Empty;

    public string? JiraIssueTypeId { get; set; }

    public string? JiraIssueTypeName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<IssueFieldMapping> IssueFieldMappings { get; set; } = [];

    public ICollection<IssueFieldMappingTemplate> IssueFieldMappingTemplates { get; set; } = [];

    public ICollection<JiraIssueTypeFieldMetadata> JiraFieldMetadata { get; set; } = [];

    public ICollection<StatusMapping> StatusMappings { get; set; } = [];
}
