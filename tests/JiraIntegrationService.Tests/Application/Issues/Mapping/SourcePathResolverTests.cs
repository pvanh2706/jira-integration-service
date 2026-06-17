using System.Text.Json;
using JiraIntegrationService.Api.Application.Issues.Mapping;

namespace JiraIntegrationService.Tests.Application.Issues.Mapping;

public sealed class SourcePathResolverTests
{
    [Fact]
    public void TryResolve_WhenPathIsNestedUnderData_ShouldReturnValue()
    {
        using var document = JsonDocument.Parse("""
            {
              "customer": {
                "code": "C001"
              }
            }
            """);
        var resolver = new SourcePathResolver();

        var found = resolver.TryResolve(document.RootElement, "data.customer.code", out var value);

        Assert.True(found);
        Assert.Equal("C001", value.GetString());
    }

    [Fact]
    public void TryResolve_WhenPathIsMissing_ShouldReturnFalse()
    {
        using var document = JsonDocument.Parse("""{ "customer": {} }""");
        var resolver = new SourcePathResolver();

        var found = resolver.TryResolve(document.RootElement, "data.customer.code", out _);

        Assert.False(found);
    }
}
