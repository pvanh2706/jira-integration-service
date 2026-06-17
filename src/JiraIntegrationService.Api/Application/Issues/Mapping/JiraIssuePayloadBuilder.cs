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

        return new CreateJiraIssueRequest(
            ProjectKey: product.JiraProjectKey,
            IssueTypeName: issueType.JiraIssueTypeName,
            Summary: summary.Trim(),
            CustomFields: fields.Count == 0 ? null : fields,
            IssueTypeId: issueType.JiraIssueTypeId);
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

        return JsonSerializer.SerializeToElement(mapping.DefaultValue.Trim());
    }
}
