namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class IssueFieldMappingTemplate
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int IssueTypeMappingId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public IssueTypeMapping IssueTypeMapping { get; set; } = null!;
}
