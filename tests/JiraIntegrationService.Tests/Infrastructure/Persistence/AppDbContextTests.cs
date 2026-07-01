using JiraIntegrationService.Api.Infrastructure.Persistence;
using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Tests.Infrastructure.Persistence;

public sealed class AppDbContextTests
{
    [Fact]
    public async Task Products_ShouldSeedActiveEasProduct()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var product = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(item => item.Code == "EAS");

        Assert.True(product.IsActive);
        Assert.Equal("EAS", product.JiraProjectKey);
    }

    [Fact]
    public async Task IssueTypeMappings_ShouldSeedEasBugMapping()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var product = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(item => item.Code == "EAS");

        var mapping = await dbContext.IssueTypeMappings
            .AsNoTracking()
            .SingleAsync(item =>
                item.ProductId == product.Id
                && item.IssueTypeCode == "BUG"
                && item.IsActive);

        Assert.Equal("Bug", mapping.JiraIssueTypeName);
    }

    [Fact]
    public async Task StatusMappings_ShouldSeedEasInProgressMapping()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var product = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(item => item.Code == "EAS");

        var bugMapping = await dbContext.IssueTypeMappings
            .AsNoTracking()
            .SingleAsync(item => item.ProductId == product.Id && item.IssueTypeCode == "BUG");

        var statusMapping = await dbContext.StatusMappings
            .AsNoTracking()
            .SingleAsync(item =>
                item.ProductId == product.Id
                && item.IssueTypeMappingId == bugMapping.Id
                && item.StandardStatus == "IN_PROGRESS"
                && item.IsActive);

        Assert.Equal("In Progress", statusMapping.JiraStatusName);
        Assert.Equal("31", statusMapping.JiraTransitionId);
        Assert.Equal("Start Progress", statusMapping.JiraTransitionName);
    }

    [Fact]
    public async Task Products_WhenInactive_ShouldNotBeReturnedByActiveQuery()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        dbContext.Products.Add(new Product
        {
            Code = "DISABLED",
            Name = "Disabled Product",
            JiraProjectKey = "DIS",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == "DISABLED" && item.IsActive);

        Assert.Null(product);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        return connection;
    }

    private static async Task<AppDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }
}
