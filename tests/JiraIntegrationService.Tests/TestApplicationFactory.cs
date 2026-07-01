using JiraIntegrationService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JiraIntegrationService.Tests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly IReadOnlyDictionary<string, string?>? _configurationOverrides;
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"jira-integration-tests-{Guid.NewGuid():N}.db");

    public TestApplicationFactory(
        Action<IServiceCollection>? configureTestServices = null,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _configureTestServices = configureTestServices;
        _configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
                ["InternalAuth:Token"] = "test-token"
            };

            if (_configurationOverrides is not null)
            {
                foreach (var item in _configurationOverrides)
                {
                    settings[item.Key] = item.Value;
                }
            }

            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_databasePath}");
            });

            _configureTestServices?.Invoke(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // The application no longer seeds default data (that now lives in
        // scripts/insert-product-config.template.sql). Migrations run during host
        // startup above, so the schema exists here; seed the baseline EAS config
        // that the integration tests assume is present.
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DefaultConfigurationSeeder.SeedEasDefaultsAsync(dbContext).GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // SQLite can briefly keep the file handle open on Windows after host disposal.
            }
        }
    }
}
