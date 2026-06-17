using System.Text.Json;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public interface ISourcePathResolver
{
    bool TryResolve(JsonElement data, string sourcePath, out JsonElement value);
}
