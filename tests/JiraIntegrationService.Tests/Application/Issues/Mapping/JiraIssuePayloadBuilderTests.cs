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
        Assert.NotNull(request.CustomFields);

        var priority = Assert.IsType<Dictionary<string, object?>>(request.CustomFields!["priority"]);
        Assert.Equal("High", priority["name"]);
        Assert.Equal("C001", request.CustomFields["customfield_10010"]);

        var components = Assert.IsAssignableFrom<IReadOnlyList<Dictionary<string, object?>>>(request.CustomFields["components"]);
        Assert.Equal("Core", components[0]["name"]);
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
        int sortOrder = 0)
    {
        return new FieldMappingConfig(
            Id: sortOrder,
            ProductId: 1,
            IssueTypeMappingId: 1,
            SourceField: sourcePath,
            JiraField: jiraField,
            IsRequired: isRequired,
            DefaultValue: null,
            ValueType: valueType,
            ValueShape: valueShape,
            SortOrder: sortOrder);
    }
}
