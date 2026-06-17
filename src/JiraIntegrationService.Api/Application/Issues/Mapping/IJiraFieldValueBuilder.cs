using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public interface IJiraFieldValueBuilder
{
    object? BuildValue(FieldMappingConfig mapping, JsonElement? value);
}
