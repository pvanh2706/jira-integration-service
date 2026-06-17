namespace JiraIntegrationService.Api.Application.Configuration.Models;

public sealed record JiraCredentialConfig(
    int Id,
    int ProductId,
    string Username,
    string Password,
    string AuthType = "Basic");
