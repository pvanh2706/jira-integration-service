using JiraIntegrationService.Api.Application.Admin;
using JiraIntegrationService.Api.Application.Admin.Models;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace JiraIntegrationService.Api.Infrastructure.Persistence;

public sealed class AdminConfigurationService : IAdminConfigurationService
{
    private const string DefaultAuthType = "Basic";
    private const string DefaultJiraApiBasePath = "/rest/api/2";
    private const string DefaultJiraVersion = "ServerV2";
    private const string DefaultValueType = "string";
    private const string DefaultValueShape = "raw";

    private static readonly HashSet<string> AllowedValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "number",
        "boolean",
        "date",
        "object",
        "array"
    };

    private static readonly HashSet<string> AllowedValueShapes = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw",
        "name",
        "id",
        "value",
        "arrayOfName",
        "arrayOfId"
    };

    private readonly AppDbContext _dbContext;

    public AdminConfigurationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductAdminResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Code)
            .ToListAsync(cancellationToken);

        return products.Select(ToProductResponse).ToList();
    }

    public async Task<ProductAdminResponse> GetProductAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductByCodeAsync(code, cancellationToken);

        return ToProductResponse(product);
    }

    public async Task<ProductAdminResponse> CreateProductAsync(
        CreateProductAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = NormalizeCode(request.Code, nameof(request.Code));
        if (await _dbContext.Products.AnyAsync(product => product.Code == code, cancellationToken))
        {
            throw new RequestValidationException($"Product '{code}' already exists.");
        }

        var timestamp = DateTime.UtcNow;
        var product = new Product
        {
            Code = code,
            Name = NormalizeRequired(request.Name, nameof(request.Name)),
            JiraProjectKey = NormalizeCode(request.JiraProjectKey, nameof(request.JiraProjectKey)),
            JiraBaseUrl = NormalizeRequired(request.JiraBaseUrl, nameof(request.JiraBaseUrl)).TrimEnd('/'),
            JiraApiBasePath = NormalizeOptional(request.JiraApiBasePath) ?? DefaultJiraApiBasePath,
            JiraVersion = NormalizeOptional(request.JiraVersion) ?? DefaultJiraVersion,
            IsActive = request.IsActive,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToProductResponse(product);
    }

    public async Task<ProductAdminResponse> UpdateProductAsync(
        string code,
        UpdateProductAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await GetProductByCodeAsync(code, cancellationToken);

        product.Name = NormalizeRequired(request.Name, nameof(request.Name));
        product.JiraProjectKey = NormalizeCode(request.JiraProjectKey, nameof(request.JiraProjectKey));
        product.JiraBaseUrl = NormalizeRequired(request.JiraBaseUrl, nameof(request.JiraBaseUrl)).TrimEnd('/');
        product.JiraApiBasePath = NormalizeOptional(request.JiraApiBasePath) ?? DefaultJiraApiBasePath;
        product.JiraVersion = NormalizeOptional(request.JiraVersion) ?? DefaultJiraVersion;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToProductResponse(product);
    }

    public async Task DeleteProductAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductByCodeAsync(code, cancellationToken);

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JiraCredentialAdminResponse> GetCredentialAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var credential = await GetCredentialByProductAsync(product.Id, product.Code, cancellationToken);

        return ToCredentialResponse(credential);
    }

    public async Task<JiraCredentialAdminResponse> UpsertCredentialAsync(
        string productCode,
        UpsertJiraCredentialAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var credential = await _dbContext.JiraCredentials
            .Where(item => item.ProductId == product.Id)
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var timestamp = DateTime.UtcNow;
        if (credential is null)
        {
            credential = new JiraCredential
            {
                ProductId = product.Id,
                CreatedAt = timestamp
            };
            _dbContext.JiraCredentials.Add(credential);
        }

        credential.AuthType = NormalizeOptional(request.AuthType) ?? DefaultAuthType;
        credential.Username = NormalizeRequired(request.Username, nameof(request.Username));
        credential.PasswordOrToken = NormalizeRequired(request.PasswordOrToken, nameof(request.PasswordOrToken));
        credential.IsActive = request.IsActive;
        credential.UpdatedAt = timestamp;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCredentialResponse(credential);
    }

    public async Task<IReadOnlyList<IssueTypeMappingAdminResponse>> GetIssueTypesAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductByCodeAsync(productCode, cancellationToken);

        var mappings = await _dbContext.IssueTypeMappings
            .AsNoTracking()
            .Where(mapping => mapping.ProductId == product.Id)
            .OrderBy(mapping => mapping.IssueTypeCode)
            .ToListAsync(cancellationToken);

        return mappings.Select(ToIssueTypeResponse).ToList();
    }

    public async Task<IssueTypeMappingAdminResponse> CreateIssueTypeAsync(
        string productCode,
        CreateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var issueTypeCode = NormalizeCode(request.IssueTypeCode, nameof(request.IssueTypeCode));
        if (await _dbContext.IssueTypeMappings.AnyAsync(
                mapping => mapping.ProductId == product.Id && mapping.IssueTypeCode == issueTypeCode,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Issue type '{issueTypeCode}' already exists for product '{product.Code}'.");
        }

        ValidateIssueTypeTarget(request.JiraIssueTypeId, request.JiraIssueTypeName);

        var timestamp = DateTime.UtcNow;
        var mapping = new IssueTypeMapping
        {
            ProductId = product.Id,
            IssueTypeCode = issueTypeCode,
            JiraIssueTypeId = NormalizeOptional(request.JiraIssueTypeId),
            JiraIssueTypeName = NormalizeOptional(request.JiraIssueTypeName),
            IsActive = request.IsActive,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        _dbContext.IssueTypeMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToIssueTypeResponse(mapping);
    }

    public async Task<IssueTypeMappingAdminResponse> UpdateIssueTypeAsync(
        string productCode,
        string issueTypeCode,
        UpdateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var mapping = await GetIssueTypeByCodeAsync(product.Id, product.Code, issueTypeCode, cancellationToken);

        ValidateIssueTypeTarget(request.JiraIssueTypeId, request.JiraIssueTypeName);

        mapping.JiraIssueTypeId = NormalizeOptional(request.JiraIssueTypeId);
        mapping.JiraIssueTypeName = NormalizeOptional(request.JiraIssueTypeName);
        mapping.IsActive = request.IsActive;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToIssueTypeResponse(mapping);
    }

    public async Task<IReadOnlyList<IssueFieldMappingAdminResponse>> GetFieldMappingsAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);

        var mappings = await _dbContext.IssueFieldMappings
            .AsNoTracking()
            .Where(mapping => mapping.ProductId == product.Id && mapping.IssueTypeMappingId == issueType.Id)
            .OrderBy(mapping => mapping.SortOrder)
            .ThenBy(mapping => mapping.SourcePath)
            .ToListAsync(cancellationToken);

        return mappings.Select(ToFieldMappingResponse).ToList();
    }

    public async Task<IssueFieldMappingAdminResponse> CreateFieldMappingAsync(
        string productCode,
        string issueTypeCode,
        UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        var sourcePath = NormalizeRequired(request.SourcePath, nameof(request.SourcePath));

        if (await _dbContext.IssueFieldMappings.AnyAsync(
                mapping =>
                    mapping.ProductId == product.Id
                    && mapping.IssueTypeMappingId == issueType.Id
                    && mapping.SourcePath == sourcePath,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Field mapping '{sourcePath}' already exists for product '{product.Code}' and issue type '{issueType.IssueTypeCode}'.");
        }

        var timestamp = DateTime.UtcNow;
        var mapping = new IssueFieldMapping
        {
            ProductId = product.Id,
            IssueTypeMappingId = issueType.Id,
            CreatedAt = timestamp
        };

        ApplyFieldMappingRequest(mapping, request, sourcePath, timestamp);
        _dbContext.IssueFieldMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToFieldMappingResponse(mapping);
    }

    public async Task<IssueFieldMappingAdminResponse> UpdateFieldMappingAsync(
        int id,
        UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapping = await GetFieldMappingByIdAsync(id, cancellationToken);
        var sourcePath = NormalizeRequired(request.SourcePath, nameof(request.SourcePath));

        if (await _dbContext.IssueFieldMappings.AnyAsync(
                item =>
                    item.Id != mapping.Id
                    && item.ProductId == mapping.ProductId
                    && item.IssueTypeMappingId == mapping.IssueTypeMappingId
                    && item.SourcePath == sourcePath,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Field mapping '{sourcePath}' already exists in this product and issue type scope.");
        }

        ApplyFieldMappingRequest(mapping, request, sourcePath, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToFieldMappingResponse(mapping);
    }

    public async Task DeleteFieldMappingAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetFieldMappingByIdAsync(id, cancellationToken);

        _dbContext.IssueFieldMappings.Remove(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StatusMappingAdminResponse>> GetStatusMappingsAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);

        var mappings = await _dbContext.StatusMappings
            .AsNoTracking()
            .Where(mapping => mapping.ProductId == product.Id && mapping.IssueTypeMappingId == issueType.Id)
            .OrderBy(mapping => mapping.StandardStatus)
            .ThenBy(mapping => mapping.JiraStatusName)
            .ToListAsync(cancellationToken);

        return mappings.Select(ToStatusMappingResponse).ToList();
    }

    public async Task<StatusMappingAdminResponse> CreateStatusMappingAsync(
        string productCode,
        string issueTypeCode,
        UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        var standardStatus = NormalizeCode(request.StandardStatus, nameof(request.StandardStatus));
        var jiraStatusName = NormalizeRequired(request.JiraStatusName, nameof(request.JiraStatusName));

        await EnsureStatusMappingIsUniqueAsync(
            product.Id,
            issueType.Id,
            standardStatus,
            jiraStatusName,
            ignoredId: null,
            cancellationToken);

        var mapping = new StatusMapping
        {
            ProductId = product.Id,
            IssueTypeMappingId = issueType.Id
        };

        ApplyStatusMappingRequest(mapping, request, standardStatus, jiraStatusName);
        _dbContext.StatusMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToStatusMappingResponse(mapping);
    }

    public async Task<StatusMappingAdminResponse> UpdateStatusMappingAsync(
        int id,
        UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapping = await GetStatusMappingByIdAsync(id, cancellationToken);
        var standardStatus = NormalizeCode(request.StandardStatus, nameof(request.StandardStatus));
        var jiraStatusName = NormalizeRequired(request.JiraStatusName, nameof(request.JiraStatusName));

        await EnsureStatusMappingIsUniqueAsync(
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            standardStatus,
            jiraStatusName,
            mapping.Id,
            cancellationToken);

        ApplyStatusMappingRequest(mapping, request, standardStatus, jiraStatusName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToStatusMappingResponse(mapping);
    }

    public async Task DeleteStatusMappingAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetStatusMappingByIdAsync(id, cancellationToken);

        _dbContext.StatusMappings.Remove(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValidateCreateIssueConfigAdminResponse> ValidateCreateIssueConfigAsync(
        string productCode,
        ValidateCreateIssueConfigAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var errors = new List<string>();

        if (!product.IsActive)
        {
            errors.Add($"Product '{product.Code}' is inactive.");
        }

        AddMissingConfigError(errors, product.JiraProjectKey, "Products.JiraProjectKey is required.");
        AddMissingConfigError(errors, product.JiraBaseUrl, "Products.JiraBaseUrl is required.");
        AddMissingConfigError(errors, product.JiraApiBasePath, "Products.JiraApiBasePath is required.");
        AddMissingConfigError(errors, product.JiraVersion, "Products.JiraVersion is required.");

        var activeCredential = await _dbContext.JiraCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(
                credential => credential.ProductId == product.Id && credential.IsActive,
                cancellationToken);
        if (activeCredential is null)
        {
            errors.Add("Active Jira credential is required.");
        }
        else
        {
            AddMissingConfigError(errors, activeCredential.AuthType, "JiraCredentials.AuthType is required.");
            AddMissingConfigError(errors, activeCredential.Username, "JiraCredentials.Username is required.");
            AddMissingConfigError(errors, activeCredential.PasswordOrToken, "JiraCredentials.PasswordOrToken is required.");
        }

        var normalizedIssueTypeCode = NormalizeOptional(request.IssueTypeCode)?.ToUpperInvariant();
        var issueTypes = await GetIssueTypesToValidateAsync(
            product.Id,
            normalizedIssueTypeCode,
            errors,
            cancellationToken);

        foreach (var issueType in issueTypes)
        {
            await ValidateIssueTypeForCreateIssueAsync(product.Id, issueType, errors, cancellationToken);
        }

        return new ValidateCreateIssueConfigAdminResponse(
            product.Code,
            normalizedIssueTypeCode,
            errors.Count == 0,
            errors);
    }

    private async Task ValidateIssueTypeForCreateIssueAsync(
        int productId,
        IssueTypeMapping issueType,
        ICollection<string> errors,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(issueType.JiraIssueTypeId)
            && string.IsNullOrWhiteSpace(issueType.JiraIssueTypeName))
        {
            errors.Add($"Issue type '{issueType.IssueTypeCode}' must have JiraIssueTypeId or JiraIssueTypeName.");
        }

        var mappings = await _dbContext.IssueFieldMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.ProductId == productId
                && mapping.IsActive
                && (mapping.IssueTypeMappingId == null || mapping.IssueTypeMappingId == issueType.Id))
            .OrderBy(mapping => mapping.IssueTypeMappingId == null ? 0 : 1)
            .ThenBy(mapping => mapping.SortOrder)
            .ToListAsync(cancellationToken);

        var effectiveMappings = mappings
            .GroupBy(mapping => mapping.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();

        if (!effectiveMappings.Any(mapping => string.Equals(mapping.JiraField, "summary", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Issue type '{issueType.IssueTypeCode}' must have a mapping to Jira field 'summary'.");
        }

        foreach (var requiredMapping in effectiveMappings.Where(mapping => mapping.IsRequired))
        {
            if (string.IsNullOrWhiteSpace(requiredMapping.SourcePath)
                && string.IsNullOrWhiteSpace(requiredMapping.DefaultValue))
            {
                errors.Add(
                    $"Required mapping '{requiredMapping.JiraField}' for issue type '{issueType.IssueTypeCode}' must have SourcePath or DefaultValue.");
            }
        }

        var duplicateJiraFields = effectiveMappings
            .GroupBy(mapping => mapping.JiraField, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(field => field)
            .ToArray();

        foreach (var duplicateJiraField in duplicateJiraFields)
        {
            errors.Add(
                $"Issue type '{issueType.IssueTypeCode}' has duplicate Jira field mapping '{duplicateJiraField}'.");
        }
    }

    private async Task<IReadOnlyList<IssueTypeMapping>> GetIssueTypesToValidateAsync(
        int productId,
        string? issueTypeCode,
        ICollection<string> errors,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(issueTypeCode))
        {
            var issueType = await _dbContext.IssueTypeMappings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    mapping =>
                        mapping.ProductId == productId
                        && mapping.IssueTypeCode == issueTypeCode
                        && mapping.IsActive,
                    cancellationToken);

            if (issueType is null)
            {
                errors.Add($"Active issue type '{issueTypeCode}' is required.");
                return [];
            }

            return [issueType];
        }

        var issueTypes = await _dbContext.IssueTypeMappings
            .AsNoTracking()
            .Where(mapping => mapping.ProductId == productId && mapping.IsActive)
            .OrderBy(mapping => mapping.IssueTypeCode)
            .ToListAsync(cancellationToken);

        if (issueTypes.Count == 0)
        {
            errors.Add("At least one active issue type mapping is required.");
        }

        return issueTypes;
    }

    private static void ApplyFieldMappingRequest(
        IssueFieldMapping mapping,
        UpsertIssueFieldMappingAdminRequest request,
        string sourcePath,
        DateTime timestamp)
    {
        var valueType = NormalizeOptional(request.ValueType) ?? DefaultValueType;
        if (!AllowedValueTypes.Contains(valueType))
        {
            throw new RequestValidationException($"valueType '{valueType}' is not supported.");
        }

        var valueShape = NormalizeOptional(request.ValueShape) ?? DefaultValueShape;
        if (!AllowedValueShapes.Contains(valueShape))
        {
            throw new RequestValidationException($"valueShape '{valueShape}' is not supported.");
        }

        mapping.SourcePath = sourcePath;
        mapping.JiraField = NormalizeRequired(request.JiraField, nameof(request.JiraField));
        mapping.ValueType = valueType;
        mapping.ValueShape = valueShape;
        mapping.IsRequired = request.IsRequired;
        mapping.DefaultValue = NormalizeOptional(request.DefaultValue);
        mapping.SortOrder = request.SortOrder;
        mapping.IsActive = request.IsActive;
        mapping.TransformConfigJson = NormalizeOptional(request.TransformConfigJson);
        mapping.UpdatedAt = timestamp;
    }

    private static void ApplyStatusMappingRequest(
        StatusMapping mapping,
        UpsertStatusMappingAdminRequest request,
        string standardStatus,
        string jiraStatusName)
    {
        mapping.StandardStatus = standardStatus;
        mapping.JiraStatusName = jiraStatusName;
        mapping.JiraTransitionId = NormalizeOptional(request.JiraTransitionId);
        mapping.JiraTransitionName = NormalizeOptional(request.JiraTransitionName);
        mapping.IsActive = request.IsActive;
    }

    private async Task EnsureStatusMappingIsUniqueAsync(
        int productId,
        int? issueTypeMappingId,
        string standardStatus,
        string jiraStatusName,
        int? ignoredId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.StatusMappings.AnyAsync(
            mapping =>
                mapping.Id != ignoredId
                && mapping.ProductId == productId
                && mapping.IssueTypeMappingId == issueTypeMappingId
                && mapping.StandardStatus == standardStatus
                && mapping.JiraStatusName == jiraStatusName,
            cancellationToken);

        if (exists)
        {
            throw new RequestValidationException(
                $"Status mapping '{standardStatus}' -> '{jiraStatusName}' already exists in this product and issue type scope.");
        }
    }

    private async Task<(Product Product, IssueTypeMapping IssueType)> GetProductAndIssueTypeAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var issueType = await GetIssueTypeByCodeAsync(product.Id, product.Code, issueTypeCode, cancellationToken);

        return (product, issueType);
    }

    private async Task<Product> GetProductByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizeCode(code, nameof(code));

        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);

        return product ?? throw new ConfigNotFoundException(
            $"Product '{normalizedCode}' was not found.");
    }

    private async Task<JiraCredential> GetCredentialByProductAsync(
        int productId,
        string productCode,
        CancellationToken cancellationToken)
    {
        var credential = await _dbContext.JiraCredentials
            .AsNoTracking()
            .Where(item => item.ProductId == productId && item.IsActive)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return credential ?? throw new ConfigNotFoundException(
            $"Active Jira credential was not found for product '{productCode}'.");
    }

    private async Task<IssueTypeMapping> GetIssueTypeByCodeAsync(
        int productId,
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var normalizedIssueTypeCode = NormalizeCode(issueTypeCode, nameof(issueTypeCode));

        var issueType = await _dbContext.IssueTypeMappings
            .SingleOrDefaultAsync(
                item => item.ProductId == productId && item.IssueTypeCode == normalizedIssueTypeCode,
                cancellationToken);

        return issueType ?? throw new ConfigNotFoundException(
            $"Issue type '{normalizedIssueTypeCode}' was not found for product '{productCode}'.");
    }

    private async Task<IssueFieldMapping> GetFieldMappingByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var mapping = await _dbContext.IssueFieldMappings
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return mapping ?? throw new ConfigNotFoundException(
            $"Field mapping '{id}' was not found.");
    }

    private async Task<StatusMapping> GetStatusMappingByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var mapping = await _dbContext.StatusMappings
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return mapping ?? throw new ConfigNotFoundException(
            $"Status mapping '{id}' was not found.");
    }

    private static void ValidateIssueTypeTarget(
        string? jiraIssueTypeId,
        string? jiraIssueTypeName)
    {
        if (string.IsNullOrWhiteSpace(jiraIssueTypeId)
            && string.IsNullOrWhiteSpace(jiraIssueTypeName))
        {
            throw new RequestValidationException("jiraIssueTypeId or jiraIssueTypeName is required.");
        }
    }

    private static void AddMissingConfigError(
        ICollection<string> errors,
        string? value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }

    private static string NormalizeCode(string? value, string fieldName)
    {
        return NormalizeRequired(value, fieldName).ToUpperInvariant();
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RequestValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ProductAdminResponse ToProductResponse(Product product)
    {
        return new ProductAdminResponse(
            product.Id,
            product.Code,
            product.Name,
            product.JiraProjectKey,
            product.JiraBaseUrl,
            product.JiraApiBasePath,
            product.JiraVersion,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }

    private static JiraCredentialAdminResponse ToCredentialResponse(JiraCredential credential)
    {
        return new JiraCredentialAdminResponse(
            credential.Id,
            credential.ProductId,
            credential.AuthType,
            credential.Username,
            !string.IsNullOrWhiteSpace(credential.PasswordOrToken),
            credential.IsActive,
            credential.CreatedAt,
            credential.UpdatedAt);
    }

    private static IssueTypeMappingAdminResponse ToIssueTypeResponse(IssueTypeMapping mapping)
    {
        return new IssueTypeMappingAdminResponse(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeCode,
            mapping.JiraIssueTypeId,
            mapping.JiraIssueTypeName,
            mapping.IsActive,
            mapping.CreatedAt,
            mapping.UpdatedAt);
    }

    private static IssueFieldMappingAdminResponse ToFieldMappingResponse(IssueFieldMapping mapping)
    {
        return new IssueFieldMappingAdminResponse(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.SourcePath,
            mapping.JiraField,
            mapping.ValueType,
            mapping.ValueShape,
            mapping.IsRequired,
            mapping.DefaultValue,
            mapping.SortOrder,
            mapping.IsActive,
            mapping.TransformConfigJson,
            mapping.CreatedAt,
            mapping.UpdatedAt);
    }

    private static StatusMappingAdminResponse ToStatusMappingResponse(StatusMapping mapping)
    {
        return new StatusMappingAdminResponse(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.StandardStatus,
            mapping.JiraStatusName,
            mapping.JiraTransitionId,
            mapping.JiraTransitionName,
            mapping.IsActive);
    }
}
