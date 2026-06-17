using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Persistence;
using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Tests.Application.Configuration;

public sealed class ProductConfigServiceTests
{
    [Fact]
    public async Task GetProductAsync_WhenProductExists_ShouldReturnProductConfig()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var product = await service.GetProductAsync("crm");

        Assert.Equal("CRM", product.Code);
        Assert.Equal("CRM", product.JiraProjectKey);
    }

    [Fact]
    public async Task GetJiraCredentialAsync_WhenCredentialExists_ShouldReturnActiveCredential()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var credential = await service.GetJiraCredentialAsync("CRM");

        Assert.Equal("jira-crm-user", credential.Username);
        Assert.Equal("change-me", credential.Password);
    }

    [Fact]
    public async Task GetIssueTypeAsync_WhenMappingExists_ShouldReturnJiraIssueTypeName()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var issueType = await service.GetIssueTypeAsync("CRM", "bug");

        Assert.Equal("BUG", issueType.IssueTypeCode);
        Assert.Equal("Bug", issueType.JiraIssueTypeName);
    }

    [Fact]
    public async Task GetIssueTypeAsync_WhenMappingIsMissing_ShouldThrowConfigNotFound()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var exception = await Assert.ThrowsAsync<ConfigNotFoundException>(
            () => service.GetIssueTypeAsync("CRM", "UNKNOWN_TYPE"));

        Assert.Equal(ErrorCodes.ConfigNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task GetFieldMappingsAsync_WhenIssueTypeHasMappings_ShouldReturnCustomFields()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var fieldMappings = await service.GetFieldMappingsAsync("CRM", "BUG");

        Assert.Contains(fieldMappings, item =>
            item.SourceField == "data.customer.code"
            && item.JiraField == "customfield_10010");
        Assert.Contains(fieldMappings, item =>
            item.SourceField == "data.ticket.url"
            && item.JiraField == "customfield_10011");
        Assert.Contains(fieldMappings, item =>
            item.SourceField == "data.summary"
            && item.JiraField == "summary"
            && item.IsRequired);
    }

    [Fact]
    public async Task GetStatusTransitionAsync_WhenIssueTypeMappingExists_ShouldReturnTransition()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var transition = await service.GetStatusTransitionAsync("CRM", "BUG", "in_progress");

        Assert.Equal("IN_PROGRESS", transition.StandardStatus);
        Assert.Equal("In Progress", transition.JiraStatusName);
        Assert.Equal("31", transition.JiraTransitionId);
        Assert.Equal("Start Progress", transition.JiraTransitionName);
    }

    [Fact]
    public async Task GetStatusTransitionAsync_WhenIssueTypeMappingIsMissing_ShouldFallbackToProductLevelMapping()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedProductLevelDoneStatusAsync(dbContext);
        var service = new ProductConfigService(dbContext);

        var transition = await service.GetStatusTransitionAsync("CRM", "TASK", "DONE");

        Assert.Null(transition.IssueTypeMappingId);
        Assert.Equal("DONE", transition.StandardStatus);
        Assert.Equal("99", transition.JiraTransitionId);
        Assert.Equal("Product Done", transition.JiraTransitionName);
    }

    [Fact]
    public async Task MapJiraStatusToStandardStatusAsync_WhenStatusMaps_ShouldReturnStandardStatus()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var standardStatus = await service.MapJiraStatusToStandardStatusAsync("CRM", "BUG", "in progress");

        Assert.Equal("IN_PROGRESS", standardStatus);
    }

    [Fact]
    public async Task MapJiraStatusToStandardStatusAsync_WhenIssueTypeMappingIsMissing_ShouldFallbackToProductLevelMapping()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedProductLevelDoneStatusAsync(dbContext);
        var service = new ProductConfigService(dbContext);

        var standardStatus = await service.MapJiraStatusToStandardStatusAsync("CRM", "TASK", "Product Closed");

        Assert.Equal("DONE", standardStatus);
    }

    [Fact]
    public async Task MapJiraStatusToStandardStatusAsync_WhenStatusDoesNotMap_ShouldReturnUnknown()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new ProductConfigService(dbContext);

        var standardStatus = await service.MapJiraStatusToStandardStatusAsync("CRM", "BUG", "Not A Jira Status");

        Assert.Equal(ProductConfigService.UnknownStatus, standardStatus);
    }

    private static async Task SeedProductLevelDoneStatusAsync(AppDbContext dbContext)
    {
        var product = await dbContext.Products.SingleAsync(item => item.Code == "CRM");

        dbContext.StatusMappings.Add(new StatusMapping
        {
            ProductId = product.Id,
            IssueTypeMappingId = null,
            StandardStatus = "DONE",
            JiraStatusName = "Product Closed",
            JiraTransitionId = "99",
            JiraTransitionName = "Product Done",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
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
