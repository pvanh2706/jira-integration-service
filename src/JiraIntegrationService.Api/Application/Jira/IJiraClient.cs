using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Jira.Models;

namespace JiraIntegrationService.Api.Application.Jira;

public interface IJiraClient
{
    Task<CreateJiraIssueResponse> CreateIssueAsync(
        JiraConnectionConfig connection,
        CreateJiraIssueRequest request,
        CancellationToken cancellationToken = default);

    Task<JiraIssueStatusResponse> GetIssueStatusAsync(
        JiraConnectionConfig connection,
        string? jiraIssueId,
        string? jiraIssueKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JiraTransitionResponse>> GetTransitionsAsync(
        JiraConnectionConfig connection,
        string? jiraIssueId,
        string? jiraIssueKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JiraIssueTypeResponse>> GetIssueTypesAsync(
        JiraConnectionConfig connection,
        string projectKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JiraIssueFieldMetadataResponse>> GetIssueTypeFieldsAsync(
        JiraConnectionConfig connection,
        string projectKey,
        string issueTypeId,
        CancellationToken cancellationToken = default);

    Task TransitionIssueAsync(
        JiraConnectionConfig connection,
        TransitionJiraIssueRequest request,
        CancellationToken cancellationToken = default);
}
