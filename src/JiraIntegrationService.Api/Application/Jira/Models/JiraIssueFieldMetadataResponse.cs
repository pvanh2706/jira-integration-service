using System.Text.Json;

namespace JiraIntegrationService.Api.Application.Jira.Models;

public sealed record JiraIssueFieldMetadataResponse(
    string FieldId,
    string Name,
    bool Required,
    JiraIssueFieldSchemaResponse Schema,
    bool HasDefaultValue,
    JsonElement? DefaultValue,
    IReadOnlyList<string> Operations,
    IReadOnlyList<JiraAllowedValueResponse> AllowedValues,
    string? AutoCompleteUrl);

public sealed record JiraIssueFieldSchemaResponse(
    string? Type,
    string? Items,
    string? System,
    string? Custom,
    int? CustomId);

public sealed record JiraAllowedValueResponse(
    string? Id,
    string? Key,
    string? Name,
    string? Value,
    string? Description,
    bool Disabled,
    JsonElement Raw);
