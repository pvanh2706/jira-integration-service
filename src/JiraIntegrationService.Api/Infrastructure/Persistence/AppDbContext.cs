using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Api.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<JiraCredential> JiraCredentials => Set<JiraCredential>();

    public DbSet<IssueTypeMapping> IssueTypeMappings => Set<IssueTypeMapping>();

    public DbSet<IssueFieldMapping> IssueFieldMappings => Set<IssueFieldMapping>();

    public DbSet<IssueFieldMappingTemplate> IssueFieldMappingTemplates => Set<IssueFieldMappingTemplate>();

    public DbSet<JiraIssueTypeFieldMetadata> JiraIssueTypeFieldMetadata => Set<JiraIssueTypeFieldMetadata>();

    public DbSet<StatusMapping> StatusMappings => Set<StatusMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureProducts(modelBuilder);
        ConfigureJiraCredentials(modelBuilder);
        ConfigureIssueTypeMappings(modelBuilder);
        ConfigureIssueFieldMappingTemplates(modelBuilder);
        ConfigureIssueFieldMappings(modelBuilder);
        ConfigureJiraIssueTypeFieldMetadata(modelBuilder);
        ConfigureStatusMappings(modelBuilder);
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
            mapping.TemplateCode,
            mapping.SourcePath
        }).IsUnique();

        entity.Property(mapping => mapping.TemplateCode).HasMaxLength(100).IsRequired();
        entity.Property(mapping => mapping.SourcePath).HasMaxLength(300).IsRequired();
        entity.Property(mapping => mapping.JiraField).HasMaxLength(200).IsRequired();
        entity.Property(mapping => mapping.JiraFieldName).HasMaxLength(300);
        entity.Property(mapping => mapping.JiraFieldDescription).HasMaxLength(2000);
        entity.Property(mapping => mapping.JiraSchemaType).HasMaxLength(100);
        entity.Property(mapping => mapping.JiraSchemaItems).HasMaxLength(100);
        entity.Property(mapping => mapping.JiraSchemaSystem).HasMaxLength(100);
        entity.Property(mapping => mapping.JiraSchemaCustom).HasMaxLength(300);
        entity.Property(mapping => mapping.JiraAllowedValuesJson).HasMaxLength(20000);
        entity.Property(mapping => mapping.JiraDefaultValueJson).HasMaxLength(4000);
        entity.Property(mapping => mapping.JiraAutoCompleteUrl).HasMaxLength(1000);
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

    private static void ConfigureIssueFieldMappingTemplates(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssueFieldMappingTemplate>();

        entity.ToTable("IssueFieldMappingTemplates");

        entity.HasKey(template => template.Id);
        entity.HasIndex(template => new
        {
            template.ProductId,
            template.IssueTypeMappingId,
            template.TemplateCode
        }).IsUnique();

        entity.Property(template => template.TemplateCode).HasMaxLength(100).IsRequired();
        entity.Property(template => template.Name).HasMaxLength(200).IsRequired();
        entity.Property(template => template.Description).HasMaxLength(1000);
        entity.Property(template => template.IsDefault).IsRequired();
        entity.Property(template => template.IsActive).IsRequired();
        entity.Property(template => template.CreatedAt).IsRequired();
        entity.Property(template => template.UpdatedAt).IsRequired();

        entity
            .HasOne(template => template.Product)
            .WithMany(product => product.IssueFieldMappingTemplates)
            .HasForeignKey(template => template.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(template => template.IssueTypeMapping)
            .WithMany(issueType => issueType.IssueFieldMappingTemplates)
            .HasForeignKey(template => template.IssueTypeMappingId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureJiraIssueTypeFieldMetadata(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<JiraIssueTypeFieldMetadata>();

        entity.ToTable("JiraIssueTypeFieldMetadata");

        entity.HasKey(metadata => metadata.Id);
        entity.HasIndex(metadata => new
        {
            metadata.ProductId,
            metadata.IssueTypeMappingId,
            metadata.FieldId
        }).IsUnique();

        entity.Property(metadata => metadata.FieldId).HasMaxLength(200).IsRequired();
        entity.Property(metadata => metadata.Name).HasMaxLength(300).IsRequired();
        entity.Property(metadata => metadata.Required).IsRequired();
        entity.Property(metadata => metadata.SchemaType).HasMaxLength(100);
        entity.Property(metadata => metadata.SchemaItems).HasMaxLength(100);
        entity.Property(metadata => metadata.SchemaSystem).HasMaxLength(100);
        entity.Property(metadata => metadata.SchemaCustom).HasMaxLength(300);
        entity.Property(metadata => metadata.HasDefaultValue).IsRequired();
        entity.Property(metadata => metadata.DefaultValueJson).HasMaxLength(4000);
        entity.Property(metadata => metadata.AutoCompleteUrl).HasMaxLength(1000);
        entity.Property(metadata => metadata.OperationsJson).HasMaxLength(4000);
        entity.Property(metadata => metadata.AllowedValuesJson).HasMaxLength(20000);
        entity.Property(metadata => metadata.RecommendedValueType).HasMaxLength(50).IsRequired();
        entity.Property(metadata => metadata.RecommendedValueShape).HasMaxLength(50).IsRequired();
        entity.Property(metadata => metadata.CreatedAt).IsRequired();
        entity.Property(metadata => metadata.UpdatedAt).IsRequired();

        entity
            .HasOne(metadata => metadata.Product)
            .WithMany(product => product.JiraIssueTypeFieldMetadata)
            .HasForeignKey(metadata => metadata.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(metadata => metadata.IssueTypeMapping)
            .WithMany(issueType => issueType.JiraFieldMetadata)
            .HasForeignKey(metadata => metadata.IssueTypeMappingId)
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
}
