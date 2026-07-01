using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public sealed class JiraIssuePayloadBuilder : IJiraIssuePayloadBuilder
{
    private readonly ISourcePathResolver _sourcePathResolver;
    private readonly IJiraFieldValueBuilder _jiraFieldValueBuilder;

    public JiraIssuePayloadBuilder(
        ISourcePathResolver sourcePathResolver,
        IJiraFieldValueBuilder jiraFieldValueBuilder)
    {
        _sourcePathResolver = sourcePathResolver;
        _jiraFieldValueBuilder = jiraFieldValueBuilder;
    }

    public CreateJiraIssueRequest BuildCreateIssueRequest(
        ProductConfig product,
        IssueTypeConfig issueType,
        IReadOnlyList<FieldMappingConfig> fieldMappings,
        JsonElement data)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(issueType);
        ArgumentNullException.ThrowIfNull(fieldMappings);

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var mapping in fieldMappings.OrderBy(item => item.SortOrder).ThenBy(item => item.SourcePath))
        {
            var value = ResolveValue(data, mapping);
            var builtValue = _jiraFieldValueBuilder.BuildValue(mapping, value);

            if (builtValue is null)
            {
                if (mapping.IsRequired)
                {
                    throw new RequestValidationException(
                        $"Field '{mapping.SourcePath}' is required.");
                }

                continue;
            }

            fields[mapping.JiraField] = builtValue;
        }

        if (!fields.TryGetValue("summary", out var summaryValue) || summaryValue is null)
        {
            throw new RequestValidationException("A mapping to Jira field 'summary' is required.");
        }

        var summary = summaryValue as string ?? summaryValue.ToString();
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new RequestValidationException("A mapping to Jira field 'summary' must produce a value.");
        }

        fields.Remove("summary");
        var description = ExtractOptionalString(fields, "description");
        var priorityName = ExtractOptionalNamedValue(fields, "priority");
        var reporterName = ExtractOptionalNamedValue(fields, "reporter");
        var assigneeName = ExtractOptionalNamedValue(fields, "assignee");
        var parentKey = ExtractOptionalString(fields, "parentKey");
        var componentIds = ExtractOptionalStringList(fields, "componentIds");
        var worklogs = ExtractOptionalWorklogs(fields, "worklogs");

        return new CreateJiraIssueRequest(
            ProjectKey: product.JiraProjectKey,
            IssueTypeName: issueType.JiraIssueTypeName,
            Summary: summary.Trim(),
            Description: description,
            PriorityName: priorityName,
            ReporterName: reporterName,
            AssigneeName: assigneeName,
            CustomFields: fields.Count == 0 ? null : fields,
            IssueTypeId: issueType.JiraIssueTypeId,
            ParentKey: parentKey,
            ComponentIds: componentIds,
            Worklogs: worklogs);
    }

    private JsonElement? ResolveValue(JsonElement data, FieldMappingConfig mapping)
    {
        if (_sourcePathResolver.TryResolve(data, mapping.SourcePath, out var value)
            && value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(mapping.DefaultValue))
        {
            return null;
        }

        var defaultValue = mapping.DefaultValue.Trim();
        var valueType = string.IsNullOrWhiteSpace(mapping.ValueType)
            ? "string"
            : mapping.ValueType.Trim();

        if (string.Equals(valueType, "object", StringComparison.OrdinalIgnoreCase)
            || string.Equals(valueType, "array", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(defaultValue);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                throw new RequestValidationException(
                    $"Default value for '{mapping.SourcePath}' must be valid JSON {valueType.ToLowerInvariant()}.");
            }
        }

        return JsonSerializer.SerializeToElement(defaultValue);
    }

    private static string? ExtractOptionalString(IDictionary<string, object?> fields, string fieldName)
    {
        if (!fields.Remove(fieldName, out var value) || value is null)
        {
            return null;
        }

        return ConvertToOptionalString(value, fieldName);
    }

    private static string? ExtractOptionalNamedValue(IDictionary<string, object?> fields, string fieldName)
    {
        if (!fields.Remove(fieldName, out var value) || value is null)
        {
            return null;
        }

        if (TryGetDictionaryValue(value, "name", out var nameValue))
        {
            return ConvertToOptionalString(nameValue, fieldName);
        }

        return ConvertToOptionalString(value, fieldName);
    }

    private static IReadOnlyList<string>? ExtractOptionalStringList(
        IDictionary<string, object?> fields,
        string fieldName)
    {
        if (!fields.Remove(fieldName, out var value) || value is null)
        {
            return null;
        }

        if (value is string stringValue)
        {
            var normalizedString = NormalizeOptionalString(stringValue);
            return normalizedString is null ? null : [normalizedString];
        }

        if (value is not IEnumerable<object?> values)
        {
            throw new RequestValidationException($"Field '{fieldName}' must be an array.");
        }

        var result = values
            .Select(item => ConvertToOptionalString(item, fieldName))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();

        return result.Length == 0 ? null : result;
    }

    private static IReadOnlyList<JiraWorklogEntry>? ExtractOptionalWorklogs(
        IDictionary<string, object?> fields,
        string fieldName)
    {
        if (!fields.Remove(fieldName, out var value) || value is null)
        {
            return null;
        }

        if (value is not IEnumerable<object?> values || value is string)
        {
            throw new RequestValidationException($"Field '{fieldName}' must be an array.");
        }

        var worklogs = values
            .Where(item => item is not null)
            .Select((item, index) => ConvertToWorklog(item!, $"{fieldName}[{index}]"))
            .ToArray();

        return worklogs.Length == 0 ? null : worklogs;
    }

    private static JiraWorklogEntry ConvertToWorklog(object value, string fieldName)
    {
        if (value is JsonElement jsonElement)
        {
            return ConvertJsonElementToWorklog(jsonElement, fieldName);
        }

        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            if (TryGetDictionaryValue(dictionary, "add", out var addValue) && addValue is not null)
            {
                return ConvertToWorklog(addValue, $"{fieldName}.add");
            }

            return BuildWorklog(
                TryGetDictionaryValue(dictionary, "started", out var started) ? started : null,
                TryGetDictionaryValue(dictionary, "timeSpent", out var timeSpent) ? timeSpent : null,
                TryGetDictionaryValue(dictionary, "comment", out var comment) ? comment : null,
                fieldName);
        }

        throw new RequestValidationException($"Field '{fieldName}' must be a worklog object.");
    }

    private static JiraWorklogEntry ConvertJsonElementToWorklog(JsonElement value, string fieldName)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new RequestValidationException($"Field '{fieldName}' must be a worklog object.");
        }

        if (value.TryGetProperty("add", out var addValue))
        {
            return ConvertJsonElementToWorklog(addValue, $"{fieldName}.add");
        }

        return BuildWorklog(
            value.TryGetProperty("started", out var started) ? started : null,
            value.TryGetProperty("timeSpent", out var timeSpent) ? timeSpent : null,
            value.TryGetProperty("comment", out var comment) ? comment : null,
            fieldName);
    }

    private static JiraWorklogEntry BuildWorklog(
        object? started,
        object? timeSpent,
        object? comment,
        string fieldName)
    {
        var startedValue = ConvertToOptionalString(started, $"{fieldName}.started");
        var timeSpentValue = ConvertToOptionalString(timeSpent, $"{fieldName}.timeSpent");
        var commentValue = ConvertToOptionalString(comment, $"{fieldName}.comment");

        if (startedValue is null || timeSpentValue is null)
        {
            throw new RequestValidationException(
                $"Field '{fieldName}' must contain started and timeSpent.");
        }

        return new JiraWorklogEntry(startedValue, timeSpentValue, commentValue);
    }

    private static bool TryGetDictionaryValue(object value, string key, out object? result)
    {
        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            return TryGetDictionaryValue(dictionary, key, out result);
        }

        result = null;
        return false;
    }

    private static bool TryGetDictionaryValue(
        IReadOnlyDictionary<string, object?> dictionary,
        string key,
        out object? result)
    {
        foreach (var item in dictionary)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                result = item.Value;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static string? ConvertToOptionalString(object? value, string fieldName)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => NormalizeOptionalString(jsonElement.GetString()),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => jsonElement.GetRawText(),
                _ => throw new RequestValidationException($"Field '{fieldName}' must be a string.")
            };
        }

        return NormalizeOptionalString(value.ToString());
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
