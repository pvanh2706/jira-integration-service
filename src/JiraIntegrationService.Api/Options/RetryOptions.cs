namespace JiraIntegrationService.Api.Options;

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    public int MaxAttempts { get; init; } = 3;

    public int DelayMilliseconds { get; init; } = 300;
}
