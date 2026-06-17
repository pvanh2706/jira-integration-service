using System.Text.Json;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public sealed class SourcePathResolver : ISourcePathResolver
{
    public bool TryResolve(JsonElement data, string sourcePath, out JsonElement value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var segments = sourcePath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        var current = data;
        var index = string.Equals(segments[0], "data", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        for (; index < segments.Length; index++)
        {
            if (current.ValueKind != JsonValueKind.Object
                || !TryGetPropertyCaseInsensitive(current, segments[index], out current))
            {
                return false;
            }
        }

        value = current.Clone();
        return true;
    }

    private static bool TryGetPropertyCaseInsensitive(
        JsonElement element,
        string propertyName,
        out JsonElement property)
    {
        if (element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        foreach (var item in element.EnumerateObject())
        {
            if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = item.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
