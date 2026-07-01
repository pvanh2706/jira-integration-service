using JiraIntegrationService.Api.Infrastructure.Persistence;
using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;

namespace JiraIntegrationService.Tests;

/// <summary>
/// Seeds the default EAS configuration for tests.
/// The application no longer seeds data through EF Core <c>HasData</c>; production
/// loads defaults from <c>scripts/insert-product-config.template.sql</c>. This helper
/// mirrors that same data so tests keep a known baseline configuration.
/// </summary>
internal static class DefaultConfigurationSeeder
{
    private static readonly DateTime Timestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedEasDefaultsAsync(AppDbContext dbContext)
    {
        var product = new Product
        {
            Code = "EAS",
            Name = "EAS",
            JiraProjectKey = "EAS",
            JiraBaseUrl = "https://jira.ezcloudhotel.com",
            JiraApiBasePath = "/rest/api/2",
            JiraVersion = "ServerV2",
            IsActive = true,
            CreatedAt = Timestamp,
            UpdatedAt = Timestamp
        };
        dbContext.Products.Add(product);

        dbContext.JiraCredentials.Add(new JiraCredential
        {
            Product = product,
            AuthType = "Basic",
            Username = "anh.phamviet",
            PasswordOrToken = "123456Aa@",
            IsActive = true,
            CreatedAt = Timestamp,
            UpdatedAt = Timestamp
        });

        var bug = new IssueTypeMapping
        {
            Product = product,
            IssueTypeCode = "BUG",
            JiraIssueTypeName = "Bug",
            IsActive = true,
            CreatedAt = Timestamp,
            UpdatedAt = Timestamp
        };
        var task = new IssueTypeMapping
        {
            Product = product,
            IssueTypeCode = "TASK",
            JiraIssueTypeName = "Task",
            IsActive = true,
            CreatedAt = Timestamp,
            UpdatedAt = Timestamp
        };
        dbContext.IssueTypeMappings.AddRange(bug, task);

        dbContext.IssueFieldMappingTemplates.AddRange(
            DefaultTemplate(product, bug),
            DefaultTemplate(product, task));

        dbContext.IssueFieldMappings.AddRange(
            FieldMapping(product, bug, "data.summary", "summary", "raw", isRequired: true, sortOrder: 10),
            FieldMapping(product, bug, "data.description", "description", "raw", isRequired: false, sortOrder: 20),
            FieldMapping(product, bug, "data.priority", "priority", "name", isRequired: false, sortOrder: 30),
            FieldMapping(product, bug, "data.customer.code", "customfield_10010", "raw", isRequired: false, sortOrder: 40),
            FieldMapping(product, bug, "data.ticket.url", "customfield_10011", "raw", isRequired: false, sortOrder: 50));

        dbContext.StatusMappings.AddRange(
            StatusMapping(product, bug, "OPEN", "To Do", transitionId: null, transitionName: null),
            StatusMapping(product, bug, "IN_PROGRESS", "In Progress", "31", "Start Progress"),
            StatusMapping(product, bug, "WAITING", "Waiting", "41", "Waiting"),
            StatusMapping(product, bug, "DONE", "Done", "51", "Done"),
            StatusMapping(product, bug, "CANCELLED", "Cancelled", "61", "Cancel"));

        await dbContext.SaveChangesAsync();
    }

    private static IssueFieldMappingTemplate DefaultTemplate(Product product, IssueTypeMapping issueType) => new()
    {
        Product = product,
        IssueTypeMapping = issueType,
        TemplateCode = "DEFAULT",
        Name = "Default",
        Description = "Default field mapping template.",
        IsDefault = true,
        IsActive = true,
        CreatedAt = Timestamp,
        UpdatedAt = Timestamp
    };

    private static IssueFieldMapping FieldMapping(
        Product product,
        IssueTypeMapping issueType,
        string sourcePath,
        string jiraField,
        string valueShape,
        bool isRequired,
        int sortOrder) => new()
    {
        Product = product,
        IssueTypeMapping = issueType,
        TemplateCode = "DEFAULT",
        SourcePath = sourcePath,
        JiraField = jiraField,
        ValueType = "string",
        ValueShape = valueShape,
        IsRequired = isRequired,
        SortOrder = sortOrder,
        IsActive = true,
        CreatedAt = Timestamp,
        UpdatedAt = Timestamp
    };

    private static StatusMapping StatusMapping(
        Product product,
        IssueTypeMapping issueType,
        string standardStatus,
        string jiraStatusName,
        string? transitionId,
        string? transitionName) => new()
    {
        Product = product,
        IssueTypeMapping = issueType,
        StandardStatus = standardStatus,
        JiraStatusName = jiraStatusName,
        JiraTransitionId = transitionId,
        JiraTransitionName = transitionName,
        IsActive = true
    };
}
