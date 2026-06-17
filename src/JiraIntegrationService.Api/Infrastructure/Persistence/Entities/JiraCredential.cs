namespace JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

public sealed class JiraCredential
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string AuthType { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordOrToken { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
