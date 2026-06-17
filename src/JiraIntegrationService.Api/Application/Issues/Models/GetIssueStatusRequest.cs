using System.ComponentModel.DataAnnotations;

namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed class GetIssueStatusRequest
{
    [Required]
    public string? ProductCode { get; init; }

    public string? JiraIssueId { get; init; }

    public string? JiraIssueKey { get; init; }

    public string? IssueTypeCode { get; init; }
}
