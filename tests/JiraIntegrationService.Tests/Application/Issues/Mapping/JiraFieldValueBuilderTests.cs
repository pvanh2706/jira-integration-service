using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Issues.Mapping;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Tests.Application.Issues.Mapping;

public sealed class JiraFieldValueBuilderTests
{
    [Theory]
    [InlineData("raw")]
    [InlineData("name")]
    [InlineData("id")]
    [InlineData("value")]
    public void BuildValue_WhenScalarShapeIsConfigured_ShouldBuildExpectedValue(string valueShape)
    {
        using var document = JsonDocument.Parse("""{ "priority": "High" }""");
        var builder = new JiraFieldValueBuilder();

        var result = builder.BuildValue(
            CreateMapping(valueShape: valueShape),
            document.RootElement.GetProperty("priority"));

        if (valueShape == "raw")
        {
            Assert.Equal("High", result);
            return;
        }

        var shapedValue = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("High", shapedValue[valueShape]);
    }

    [Fact]
    public void BuildValue_WhenArrayOfNameIsConfigured_ShouldBuildObjectArray()
    {
        using var document = JsonDocument.Parse("""{ "components": ["Core", "API"] }""");
        var builder = new JiraFieldValueBuilder();

        var result = builder.BuildValue(
            CreateMapping(valueType: "array", valueShape: "arrayOfName"),
            document.RootElement.GetProperty("components"));

        var values = Assert.IsAssignableFrom<IReadOnlyList<Dictionary<string, object?>>>(result);
        Assert.Equal("Core", values[0]["name"]);
        Assert.Equal("API", values[1]["name"]);
    }

    [Fact]
    public void BuildValue_WhenNumberIsInvalid_ShouldThrowValidationError()
    {
        using var document = JsonDocument.Parse("""{ "amount": "abc" }""");
        var builder = new JiraFieldValueBuilder();

        var exception = Assert.Throws<RequestValidationException>(() =>
            builder.BuildValue(
                CreateMapping(valueType: "number"),
                document.RootElement.GetProperty("amount")));

        Assert.Contains("must be number", exception.Message);
    }

    private static FieldMappingConfig CreateMapping(
        string valueType = "string",
        string valueShape = "raw")
    {
        return new FieldMappingConfig(
            Id: 1,
            ProductId: 1,
            IssueTypeMappingId: 1,
            SourceField: "data.priority",
            JiraField: "priority",
            IsRequired: false,
            DefaultValue: null,
            ValueType: valueType,
            ValueShape: valueShape);
    }
}
