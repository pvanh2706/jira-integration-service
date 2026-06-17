namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class StatusMapping
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int? IssueTypeMappingId { get; set; }

    public string StandardStatus { get; set; } = string.Empty;

    public string JiraStatusName { get; set; } = string.Empty;

    public string? JiraTransitionId { get; set; }

    public string? JiraTransitionName { get; set; }

    public bool IsActive { get; set; }

    public Product Product { get; set; } = null!;

    public IssueTypeMapping? IssueTypeMapping { get; set; }
}
