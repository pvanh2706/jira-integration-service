using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Api.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<JiraCredential> JiraCredentials => Set<JiraCredential>();

    public DbSet<IssueTypeMapping> IssueTypeMappings => Set<IssueTypeMapping>();

    public DbSet<IssueFieldMapping> IssueFieldMappings => Set<IssueFieldMapping>();

    public DbSet<StatusMapping> StatusMappings => Set<StatusMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureProducts(modelBuilder);
        ConfigureJiraCredentials(modelBuilder);
        ConfigureIssueTypeMappings(modelBuilder);
        ConfigureIssueFieldMappings(modelBuilder);
        ConfigureStatusMappings(modelBuilder);
        SeedInitialConfiguration(modelBuilder);
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Product>();

        entity.HasKey(product => product.Id);
        entity.HasIndex(product => product.Code).IsUnique();

        entity.Property(product => product.Code).HasMaxLength(50).IsRequired();
        entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
        entity.Property(product => product.JiraProjectKey).HasMaxLength(50).IsRequired();
        entity.Property(product => product.JiraBaseUrl).HasMaxLength(500).IsRequired();
        entity.Property(product => product.JiraApiBasePath).HasMaxLength(100).IsRequired();
        entity.Property(product => product.JiraVersion).HasMaxLength(50).IsRequired();
        entity.Property(product => product.IsActive).IsRequired();
        entity.Property(product => product.CreatedAt).IsRequired();
        entity.Property(product => product.UpdatedAt).IsRequired();
    }

    private static void ConfigureJiraCredentials(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<JiraCredential>();

        entity.HasKey(credential => credential.Id);
        entity.HasIndex(credential => new { credential.ProductId, credential.IsActive });

        entity.Property(credential => credential.AuthType).HasMaxLength(50).IsRequired();
        entity.Property(credential => credential.Username).HasMaxLength(200).IsRequired();
        entity.Property(credential => credential.PasswordOrToken).HasMaxLength(500).IsRequired();
        entity.Property(credential => credential.IsActive).IsRequired();
        entity.Property(credential => credential.CreatedAt).IsRequired();
        entity.Property(credential => credential.UpdatedAt).IsRequired();

        entity
            .HasOne(credential => credential.Product)
            .WithMany(product => product.JiraCredentials)
            .HasForeignKey(credential => credential.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIssueTypeMappings(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssueTypeMapping>();

        entity.HasKey(mapping => mapping.Id);
        entity.HasIndex(mapping => new { mapping.ProductId, mapping.IssueTypeCode }).IsUnique();

        entity.Property(mapping => mapping.IssueTypeCode).HasMaxLength(100).IsRequired();
        entity.Property(mapping => mapping.JiraIssueTypeId).HasMaxLength(100);
        entity.Property(mapping => mapping.JiraIssueTypeName).HasMaxLength(200);
        entity.Property(mapping => mapping.IsActive).IsRequired();
        entity.Property(mapping => mapping.CreatedAt).IsRequired();
        entity.Property(mapping => mapping.UpdatedAt).IsRequired();

        entity
            .HasOne(mapping => mapping.Product)
            .WithMany(product => product.IssueTypeMappings)
            .HasForeignKey(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIssueFieldMappings(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssueFieldMapping>();

        entity.ToTable("IssueFieldMappings");

        entity.HasKey(mapping => mapping.Id);
        entity.HasIndex(mapping => new
        {
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.SourcePath
        }).IsUnique();

        entity.Property(mapping => mapping.SourcePath).HasMaxLength(300).IsRequired();
        entity.Property(mapping => mapping.JiraField).HasMaxLength(200).IsRequired();
        entity.Property(mapping => mapping.ValueType).HasMaxLength(50).IsRequired();
        entity.Property(mapping => mapping.ValueShape).HasMaxLength(50).IsRequired();
        entity.Property(mapping => mapping.DefaultValue).HasMaxLength(1000);
        entity.Property(mapping => mapping.IsRequired).IsRequired();
        entity.Property(mapping => mapping.SortOrder).IsRequired();
        entity.Property(mapping => mapping.IsActive).IsRequired();
        entity.Property(mapping => mapping.TransformConfigJson).HasMaxLength(4000);
        entity.Property(mapping => mapping.CreatedAt).IsRequired();
        entity.Property(mapping => mapping.UpdatedAt).IsRequired();

        entity
            .HasOne(mapping => mapping.Product)
            .WithMany(product => product.IssueFieldMappings)
            .HasForeignKey(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(mapping => mapping.IssueTypeMapping)
            .WithMany(issueType => issueType.IssueFieldMappings)
            .HasForeignKey(mapping => mapping.IssueTypeMappingId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureStatusMappings(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StatusMapping>();

        entity.HasKey(mapping => mapping.Id);
        entity.HasIndex(mapping => new
        {
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.StandardStatus,
            mapping.JiraStatusName
        }).IsUnique();

        entity.Property(mapping => mapping.StandardStatus).HasMaxLength(50).IsRequired();
        entity.Property(mapping => mapping.JiraStatusName).HasMaxLength(200).IsRequired();
        entity.Property(mapping => mapping.JiraTransitionId).HasMaxLength(100);
        entity.Property(mapping => mapping.JiraTransitionName).HasMaxLength(200);
        entity.Property(mapping => mapping.IsActive).IsRequired();

        entity
            .HasOne(mapping => mapping.Product)
            .WithMany(product => product.StatusMappings)
            .HasForeignKey(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(mapping => mapping.IssueTypeMapping)
            .WithMany(issueType => issueType.StatusMappings)
            .HasForeignKey(mapping => mapping.IssueTypeMappingId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void SeedInitialConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Code = "EAS",
                Name = "EAS",
                JiraProjectKey = "EAS",
                JiraBaseUrl = "https://jira.ezcloudhotel.com",
                JiraApiBasePath = "/rest/api/2",
                JiraVersion = "ServerV2",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<JiraCredential>().HasData(
            new JiraCredential
            {
                Id = 1,
                ProductId = 1,
                AuthType = "Basic",
                Username = "anh.phamviet",
                PasswordOrToken = "123456Aa@",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<IssueTypeMapping>().HasData(
            new IssueTypeMapping
            {
                Id = 1,
                ProductId = 1,
                IssueTypeCode = "BUG",
                JiraIssueTypeId = null,
                JiraIssueTypeName = "Bug",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new IssueTypeMapping
            {
                Id = 2,
                ProductId = 1,
                IssueTypeCode = "TASK",
                JiraIssueTypeId = null,
                JiraIssueTypeName = "Task",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<IssueFieldMapping>().HasData(
            new IssueFieldMapping
            {
                Id = 1,
                ProductId = 1,
                IssueTypeMappingId = 1,
                SourcePath = "data.summary",
                JiraField = "summary",
                ValueType = "string",
                ValueShape = "raw",
                IsRequired = true,
                DefaultValue = null,
                SortOrder = 10,
                IsActive = true,
                TransformConfigJson = null,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new IssueFieldMapping
            {
                Id = 2,
                ProductId = 1,
                IssueTypeMappingId = 1,
                SourcePath = "data.description",
                JiraField = "description",
                ValueType = "string",
                ValueShape = "raw",
                IsRequired = false,
                DefaultValue = null,
                SortOrder = 20,
                IsActive = true,
                TransformConfigJson = null,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new IssueFieldMapping
            {
                Id = 3,
                ProductId = 1,
                IssueTypeMappingId = 1,
                SourcePath = "data.priority",
                JiraField = "priority",
                ValueType = "string",
                ValueShape = "name",
                IsRequired = false,
                DefaultValue = null,
                SortOrder = 30,
                IsActive = true,
                TransformConfigJson = null,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new IssueFieldMapping
            {
                Id = 4,
                ProductId = 1,
                IssueTypeMappingId = 1,
                SourcePath = "data.customer.code",
                JiraField = "customfield_10010",
                ValueType = "string",
                ValueShape = "raw",
                IsRequired = false,
                DefaultValue = null,
                SortOrder = 40,
                IsActive = true,
                TransformConfigJson = null,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new IssueFieldMapping
            {
                Id = 5,
                ProductId = 1,
                IssueTypeMappingId = 1,
                SourcePath = "data.ticket.url",
                JiraField = "customfield_10011",
                ValueType = "string",
                ValueShape = "raw",
                IsRequired = false,
                DefaultValue = null,
                SortOrder = 50,
                IsActive = true,
                TransformConfigJson = null,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<StatusMapping>().HasData(
            new StatusMapping
            {
                Id = 1,
                ProductId = 1,
                IssueTypeMappingId = 1,
                StandardStatus = "OPEN",
                JiraStatusName = "To Do",
                JiraTransitionId = null,
                JiraTransitionName = null,
                IsActive = true
            },
            new StatusMapping
            {
                Id = 2,
                ProductId = 1,
                IssueTypeMappingId = 1,
                StandardStatus = "IN_PROGRESS",
                JiraStatusName = "In Progress",
                JiraTransitionId = "31",
                JiraTransitionName = "Start Progress",
                IsActive = true
            },
            new StatusMapping
            {
                Id = 3,
                ProductId = 1,
                IssueTypeMappingId = 1,
                StandardStatus = "WAITING",
                JiraStatusName = "Waiting",
                JiraTransitionId = "41",
                JiraTransitionName = "Waiting",
                IsActive = true
            },
            new StatusMapping
            {
                Id = 4,
                ProductId = 1,
                IssueTypeMappingId = 1,
                StandardStatus = "DONE",
                JiraStatusName = "Done",
                JiraTransitionId = "51",
                JiraTransitionName = "Done",
                IsActive = true
            },
            new StatusMapping
            {
                Id = 5,
                ProductId = 1,
                IssueTypeMappingId = 1,
                StandardStatus = "CANCELLED",
                JiraStatusName = "Cancelled",
                JiraTransitionId = "61",
                JiraTransitionName = "Cancel",
                IsActive = true
            });
    }
}
