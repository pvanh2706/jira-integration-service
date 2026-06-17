using System.Net;
using JiraIntegrationService.Api.Infrastructure.Security;

namespace JiraIntegrationService.Tests;

public sealed class StaticFilesTests : IDisposable
{
    private readonly string _frontendDistPath = Path.Combine(
        Path.GetTempPath(),
        $"jira-integration-web-dist-{Guid.NewGuid():N}");

    [Fact]
    public async Task FrontendRoutes_WhenDistExists_ShouldServeSpaWithoutAuth()
    {
        Directory.CreateDirectory(Path.Combine(_frontendDistPath, "assets"));
        await File.WriteAllTextAsync(
            Path.Combine(_frontendDistPath, "index.html"),
            "<!doctype html><html><body><div id=\"app\">SPA</div></body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(_frontendDistPath, "assets", "app.js"),
            "console.log('spa');");

        await using var factory = new TestApplicationFactory(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["Frontend:DistPath"] = _frontendDistPath
            });
        using var client = factory.CreateClient();

        var rootResponse = await client.GetAsync("/");
        var routeResponse = await client.GetAsync("/products/CRM");
        var assetResponse = await client.GetAsync("/assets/app.js");
        var apiResponse = await client.GetAsync("/api/admin/products");
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");
        var unknownApiResponse = await client.GetAsync("/api/unknown-route");

        Assert.Equal(HttpStatusCode.OK, rootResponse.StatusCode);
        Assert.Contains("SPA", await rootResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, routeResponse.StatusCode);
        Assert.Contains("SPA", await routeResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, assetResponse.StatusCode);
        Assert.Equal("console.log('spa');", await assetResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, apiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownApiResponse.StatusCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_frontendDistPath))
        {
            Directory.Delete(_frontendDistPath, recursive: true);
        }
    }
}
