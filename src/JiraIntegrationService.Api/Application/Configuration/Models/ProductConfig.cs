namespace JiraIntegrationService.Api.Application.Configuration.Models;

public sealed record ProductConfig(
    int Id,
    string Code,
    string Name,
    string JiraProjectKey,
    string JiraBaseUrl = "",
    string JiraApiBasePath = "/rest/api/2",
    string JiraVersion = "ServerV2");
