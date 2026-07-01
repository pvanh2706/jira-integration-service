using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Issues.Mapping;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Tests.Application.Issues.Mapping;

public sealed class JiraIssuePayloadBuilderTests
{
    [Fact]
    public void BuildCreateIssueRequest_WhenMappingsAreValid_ShouldBuildJiraRequest()
    {
        using var document = JsonDocument.Parse("""
            {
              "title": "Cannot login",
              "priority": "High",
              "customer": {
                "code": "C001"
              },
              "components": ["Core"]
            }
            """);
        var builder = CreateBuilder();

        var request = builder.BuildCreateIssueRequest(
            Product,
            IssueType,
            [
                CreateMapping("data.title", "summary", isRequired: true, sortOrder: 1),
                CreateMapping("data.priority", "priority", valueShape: "name", sortOrder: 2),
                CreateMapping("data.customer.code", "customfield_10010", sortOrder: 3),
                CreateMapping("data.components", "components", valueType: "array", valueShape: "arrayOfName", sortOrder: 4)
            ],
            document.RootElement);

        Assert.Equal("CRM", request.ProjectKey);
        Assert.Equal("10001", request.IssueTypeId);
        Assert.Equal("Cannot login", request.Summary);
        Assert.Equal("High", request.PriorityName);
        Assert.NotNull(request.CustomFields);

        Assert.Equal("C001", request.CustomFields["customfield_10010"]);

        var components = Assert.IsAssignableFrom<IReadOnlyList<Dictionary<string, object?>>>(request.CustomFields["components"]);
        Assert.Equal("Core", components[0]["name"]);
    }

    [Fact]
    public void BuildCreateIssueRequest_WhenStandardFieldsAreMapped_ShouldSetCreateRequestProperties()
    {
        using var document = JsonDocument.Parse("""
            {
              "summary": "Fix login",
              "assignee": "anh.phamviet",
              "parentKey": "EAS-38560"
            }
            """);
        var builder = CreateBuilder();

        var request = builder.BuildCreateIssueRequest(
            Product,
            IssueType,
            [
                CreateMapping("data.summary", "summary", isRequired: true, sortOrder: 1),
                CreateMapping("data.description", "description", defaultValue: "Default description", sortOrder: 2),
                CreateMapping("data.assignee", "assignee", sortOrder: 3),
                CreateMapping("data.parentKey", "parentKey", sortOrder: 4),
                CreateMapping("data.componentIds", "componentIds", valueType: "array", defaultValue: """["15690"]""", sortOrder: 5),
                CreateMapping("data.worklogs", "worklogs", valueType: "array", defaultValue: """[{"started":"2024-11-25T15:05:00.000+0000","timeSpent":"4.7h","comment":"Work"}]""", sortOrder: 6),
                CreateMapping("data.customFields.customfield_12815", "customfield_12815", valueType: "object", defaultValue: """{"value":"Development"}""", sortOrder: 7)
            ],
            document.RootElement);

        Assert.Equal("Fix login", request.Summary);
        Assert.Equal("Default description", request.Description);
        Assert.Equal("anh.phamviet", request.AssigneeName);
        Assert.Equal("EAS-38560", request.ParentKey);
        Assert.Equal(["15690"], request.ComponentIds);

        var worklog = Assert.Single(request.Worklogs!);
        Assert.Equal("2024-11-25T15:05:00.000+0000", worklog.Started);
        Assert.Equal("4.7h", worklog.TimeSpent);
        Assert.Equal("Work", worklog.Comment);

        var customField = Assert.IsType<JsonElement>(request.CustomFields!["customfield_12815"]);
        Assert.Equal("Development", customField.GetProperty("value").GetString());
    }

    [Fact]
    public void BuildCreateIssueRequest_WhenWorklogsUseJiraAddShape_ShouldUnwrapWorklogs()
    {
        using var document = JsonDocument.Parse("""
            {
              "summary": "Fix login",
              "worklogs": [
                {
                  "add": {
                    "started": "2024-11-25T15:05:00.000+0000",
                    "timeSpent": "4.7h",
                    "comment": "Xu ly bug v4.56.0"
                  }
                }
              ]
            }
            """);
        var builder = CreateBuilder();

        var request = builder.BuildCreateIssueRequest(
            Product,
            IssueType,
            [
                CreateMapping("data.summary", "summary", isRequired: true, sortOrder: 1),
                CreateMapping("data.worklogs", "worklogs", valueType: "array", sortOrder: 2)
            ],
            document.RootElement);

        var worklog = Assert.Single(request.Worklogs!);
        Assert.Equal("2024-11-25T15:05:00.000+0000", worklog.Started);
        Assert.Equal("4.7h", worklog.TimeSpent);
        Assert.Equal("Xu ly bug v4.56.0", worklog.Comment);
    }

    [Fact]
    public void BuildCreateIssueRequest_WhenRequiredValueIsMissing_ShouldThrowValidationError()
    {
        using var document = JsonDocument.Parse("""{ "title": "Cannot login" }""");
        var builder = CreateBuilder();

        var exception = Assert.Throws<RequestValidationException>(() =>
            builder.BuildCreateIssueRequest(
                Product,
                IssueType,
                [
                    CreateMapping("data.title", "summary", isRequired: true),
                    CreateMapping("data.customer.code", "customfield_10010", isRequired: true)
                ],
                document.RootElement));

        Assert.Contains("data.customer.code", exception.Message);
    }

    private static readonly ProductConfig Product = new(
        Id: 1,
        Code: "CRM",
        Name: "CRM",
        JiraProjectKey: "CRM",
        JiraBaseUrl: "https://jira.example.com",
        JiraApiBasePath: "/rest/api/2",
        JiraVersion: "ServerV2");

    private static readonly IssueTypeConfig IssueType = new(
        Id: 1,
        ProductId: 1,
        IssueTypeCode: "BUG",
        JiraIssueTypeName: "Bug",
        JiraIssueTypeId: "10001");

    private static JiraIssuePayloadBuilder CreateBuilder()
    {
        return new JiraIssuePayloadBuilder(
            new SourcePathResolver(),
            new JiraFieldValueBuilder());
    }

    private static FieldMappingConfig CreateMapping(
        string sourcePath,
        string jiraField,
        string valueType = "string",
        string valueShape = "raw",
        bool isRequired = false,
        string? defaultValue = null,
        int sortOrder = 0)
    {
        return new FieldMappingConfig(
            Id: sortOrder,
            ProductId: 1,
            IssueTypeMappingId: 1,
            SourceField: sourcePath,
            JiraField: jiraField,
            IsRequired: isRequired,
            DefaultValue: defaultValue,
            ValueType: valueType,
            ValueShape: valueShape,
            SortOrder: sortOrder);
    }
}
