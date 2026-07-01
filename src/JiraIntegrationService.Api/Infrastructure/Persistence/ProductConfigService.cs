using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Api.Infrastructure.Persistence;

public sealed class ProductConfigService : IProductConfigService
{
    public const string UnknownStatus = "UNKNOWN";
    private const string DefaultTemplateCode = "DEFAULT";

    private readonly AppDbContext _dbContext;

    public ProductConfigService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductConfig> GetProductAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);

        return ToProductConfig(product);
    }

    public async Task<JiraCredentialConfig> GetJiraCredentialAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);

        var credential = await _dbContext.JiraCredentials
            .AsNoTracking()
            .Where(item => item.ProductId == product.Id && item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (credential is null)
        {
            throw new ConfigNotFoundException(
                $"Active Jira credential config was not found for product '{product.Code}'.");
        }

        return new JiraCredentialConfig(
            credential.Id,
            credential.ProductId,
            credential.Username,
            credential.PasswordOrToken,
            credential.AuthType);
    }

    public async Task<IssueTypeConfig> GetIssueTypeAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);
        var issueType = await GetActiveIssueTypeAsync(product, issueTypeCode, cancellationToken);

        return ToIssueTypeConfig(issueType);
    }

    public async Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsAsync(
        string productCode,
        string? issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issueTypeCode))
        {
            return await GetFieldMappingsByTemplateInternalAsync(
                productCode,
                issueTypeCode,
                templateCode: null,
                cancellationToken);
        }

        return await GetFieldMappingsByTemplateInternalAsync(
            productCode,
            issueTypeCode,
            DefaultTemplateCode,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsByTemplateAsync(
        string productCode,
        string issueTypeCode,
        string? templateCode,
        CancellationToken cancellationToken = default)
    {
        return await GetFieldMappingsByTemplateInternalAsync(
            productCode,
            issueTypeCode,
            NormalizeTemplateCodeOrDefault(templateCode),
            cancellationToken);
    }

    private async Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsByTemplateInternalAsync(
        string productCode,
        string? issueTypeCode,
        string? templateCode,
        CancellationToken cancellationToken)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);
        var issueType = await GetOptionalIssueTypeAsync(product, issueTypeCode, cancellationToken);

        var issueTypeId = issueType?.Id;
        var fieldMappings = await _dbContext.IssueFieldMappings
            .AsNoTracking()
            .Where(item =>
                item.ProductId == product.Id
                && item.IsActive
                && (item.IssueTypeMappingId == null || item.TemplateCode == templateCode)
                && (item.IssueTypeMappingId == null || item.IssueTypeMappingId == issueTypeId))
            .OrderBy(item => item.IssueTypeMappingId == null ? 0 : 1)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.SourcePath)
            .ToListAsync(cancellationToken);

        return fieldMappings
            .GroupBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.SourcePath)
            .Select(ToFieldMappingConfig)
            .ToList();
    }

    public async Task<StatusTransitionConfig> GetStatusTransitionAsync(
        string productCode,
        string? issueTypeCode,
        string standardStatus,
        CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);
        var issueType = await GetOptionalIssueTypeAsync(product, issueTypeCode, cancellationToken);
        var normalizedStatus = NormalizeRequired(standardStatus, nameof(standardStatus));

        var mapping = await FindStatusMappingAsync(
            product.Id,
            issueType?.Id,
            item => item.StandardStatus == normalizedStatus,
            cancellationToken);

        if (mapping is null)
        {
            throw new ConfigNotFoundException(
                $"Status transition config was not found for product '{product.Code}' and status '{normalizedStatus}'.");
        }

        return ToStatusTransitionConfig(mapping);
    }

    public async Task<string> MapJiraStatusToStandardStatusAsync(
        string productCode,
        string? issueTypeCode,
        string jiraStatusName,
        CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(productCode, cancellationToken);
        var issueType = await GetOptionalIssueTypeAsync(product, issueTypeCode, cancellationToken);
        var normalizedJiraStatusName = NormalizeTextRequired(jiraStatusName, nameof(jiraStatusName));

        var mapping = await FindStatusMappingAsync(
            product.Id,
            issueType?.Id,
            item => string.Equals(
                item.JiraStatusName,
                normalizedJiraStatusName,
                StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return mapping?.StandardStatus ?? UnknownStatus;
    }

    private async Task<Product> GetActiveProductAsync(
        string productCode,
        CancellationToken cancellationToken)
    {
        var normalizedProductCode = NormalizeRequired(productCode, nameof(productCode));

        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Code == normalizedProductCode && item.IsActive,
                cancellationToken);

        if (product is null)
        {
            throw new ConfigNotFoundException(
                $"Active product config was not found for product '{normalizedProductCode}'.");
        }

        return product;
    }

    private async Task<IssueTypeMapping> GetActiveIssueTypeAsync(
        Product product,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var normalizedIssueTypeCode = NormalizeRequired(issueTypeCode, nameof(issueTypeCode));

        var issueType = await _dbContext.IssueTypeMappings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.ProductId == product.Id
                    && item.IssueTypeCode == normalizedIssueTypeCode
                    && item.IsActive,
                cancellationToken);

        if (issueType is null)
        {
            throw new ConfigNotFoundException(
                $"Issue type config was not found for product '{product.Code}' and issue type '{normalizedIssueTypeCode}'.");
        }

        return issueType;
    }

    private async Task<IssueTypeMapping?> GetOptionalIssueTypeAsync(
        Product product,
        string? issueTypeCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(issueTypeCode))
        {
            return null;
        }

        return await GetActiveIssueTypeAsync(product, issueTypeCode, cancellationToken);
    }

    private async Task<StatusMapping?> FindStatusMappingAsync(
        int productId,
        int? issueTypeMappingId,
        Func<StatusMapping, bool> predicate,
        CancellationToken cancellationToken)
    {
        if (issueTypeMappingId.HasValue)
        {
            var issueTypeMapping = await _dbContext.StatusMappings
                .AsNoTracking()
                .Where(item =>
                    item.ProductId == productId
                    && item.IssueTypeMappingId == issueTypeMappingId
                    && item.IsActive)
                .ToListAsync(cancellationToken);

            var mapping = issueTypeMapping.SingleOrDefault(predicate);
            if (mapping is not null)
            {
                return mapping;
            }
        }

        var productLevelMappings = await _dbContext.StatusMappings
            .AsNoTracking()
            .Where(item =>
                item.ProductId == productId
                && item.IssueTypeMappingId == null
                && item.IsActive)
            .ToListAsync(cancellationToken);

        return productLevelMappings.SingleOrDefault(predicate);
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ConfigNotFoundException($"Required config lookup value '{parameterName}' was empty.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeTemplateCodeOrDefault(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultTemplateCode
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeTextRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ConfigNotFoundException($"Required config lookup value '{parameterName}' was empty.");
        }

        return value.Trim();
    }

    private static ProductConfig ToProductConfig(Product product)
    {
        return new ProductConfig(
            product.Id,
            product.Code,
            product.Name,
            product.JiraProjectKey,
            product.JiraBaseUrl,
            product.JiraApiBasePath,
            product.JiraVersion);
    }

    private static IssueTypeConfig ToIssueTypeConfig(IssueTypeMapping issueType)
    {
        return new IssueTypeConfig(
            issueType.Id,
            issueType.ProductId,
            issueType.IssueTypeCode,
            issueType.JiraIssueTypeName ?? string.Empty,
            issueType.JiraIssueTypeId);
    }

    private static FieldMappingConfig ToFieldMappingConfig(IssueFieldMapping mapping)
    {
        return new FieldMappingConfig(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.SourcePath,
            mapping.JiraField,
            mapping.IsRequired,
            mapping.DefaultValue,
            mapping.ValueType,
            mapping.ValueShape,
            mapping.SortOrder,
            mapping.TransformConfigJson,
            mapping.JiraFieldName,
            mapping.JiraFieldDescription,
            mapping.JiraSchemaType,
            mapping.JiraSchemaItems,
            mapping.JiraAllowedValuesJson,
            mapping.JiraDefaultValueJson);
    }

    private static StatusTransitionConfig ToStatusTransitionConfig(StatusMapping mapping)
    {
        return new StatusTransitionConfig(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.StandardStatus,
            mapping.JiraStatusName,
            mapping.JiraTransitionId,
            mapping.JiraTransitionName);
    }
}
