namespace JiraIntegrationService.Api.Options;

public sealed class JiraOptions
{
    public const string SectionName = "Jira";

    public string BaseUrl { get; init; } = string.Empty;

    public string ApiBasePath { get; init; } = "/rest/api/2";
}
