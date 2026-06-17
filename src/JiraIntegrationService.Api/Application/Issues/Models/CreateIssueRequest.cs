using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace JiraIntegrationService.Api.Application.Issues.Models;

public sealed class CreateIssueRequest
{
    [Required]
    public string? ProductCode { get; init; }

    [Required]
    public string? IssueTypeCode { get; init; }

    [Required]
    public JsonElement? Data { get; init; }
}
