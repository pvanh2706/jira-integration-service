using System.Net;
using System.Text.Json;

namespace JiraIntegrationService.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_WithoutInternalAuthToken_ShouldReturnStandardSuccessResponse()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("OK", root.GetProperty("data").GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }
}
