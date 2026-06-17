namespace JiraIntegrationService.Api.Options;

public sealed class InternalAuthOptions
{
    public const string SectionName = "InternalAuth";

    public string Token { get; init; } = string.Empty;
}
