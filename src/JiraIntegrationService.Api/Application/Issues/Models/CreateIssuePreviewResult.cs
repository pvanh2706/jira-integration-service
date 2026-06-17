using JiraIntegrationService.Api.Application.Jira.Models;

namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed record CreateIssuePreviewResult(
    CreateJiraIssueRequest JiraRequest);
