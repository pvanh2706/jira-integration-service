namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class Product
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string JiraProjectKey { get; set; } = string.Empty;

    public string JiraBaseUrl { get; set; } = string.Empty;

    public string JiraApiBasePath { get; set; } = string.Empty;

    public string JiraVersion { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<JiraCredential> JiraCredentials { get; set; } = [];

    public ICollection<IssueTypeMapping> IssueTypeMappings { get; set; } = [];

    public ICollection<IssueFieldMapping> IssueFieldMappings { get; set; } = [];

    public ICollection<IssueFieldMappingTemplate> IssueFieldMappingTemplates { get; set; } = [];

    public ICollection<JiraIssueTypeFieldMetadata> JiraIssueTypeFieldMetadata { get; set; } = [];

    public ICollection<StatusMapping> StatusMappings { get; set; } = [];
}
