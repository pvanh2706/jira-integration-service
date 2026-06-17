using System.ComponentModel.DataAnnotations;

namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed class UpdateIssueStatusRequest
{
    [Required]
    public string? ProductCode { get; init; }

    public string? JiraIssueId { get; init; }

    public string? JiraIssueKey { get; init; }

    public string? IssueTypeCode { get; init; }

    [Required]
    public string? StandardStatus { get; init; }
}
