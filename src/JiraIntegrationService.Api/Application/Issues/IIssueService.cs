using JiraIntegrationService.Api.Application.Issues.Models;

namespace JiraIntegrationService.Api.Application.Issues;

public interface IIssueService
{
    Task<CreateIssueResult> CreateIssueAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateIssuePreviewResult> PreviewCreateIssueAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateIssueStatusResult> UpdateIssueStatusAsync(
        UpdateIssueStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<GetIssueStatusResult> GetIssueStatusAsync(
        GetIssueStatusRequest request,
        CancellationToken cancellationToken = default);
}
