using System.Text;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Admin;
using JiraIntegrationService.Api.Application.Admin.Models;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
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
    private const string DefaultTemplateCode = "DEFAULT";
    private const string DefaultTemplateName = "Default";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    private readonly IJiraClientResolver _jiraClientResolver;

    public AdminConfigurationService(
        AppDbContext dbContext,
        IJiraClientResolver jiraClientResolver)
    {
        _dbContext = dbContext;
        _jiraClientResolver = jiraClientResolver;
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

    public async Task<SyncIssueTypesAdminResponse> SyncIssueTypesFromJiraAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductByCodeAsync(productCode, cancellationToken);
        var credential = await GetCredentialByProductAsync(product.Id, product.Code, cancellationToken);
        var connection = ToJiraConnection(product, credential);
        var jiraClient = _jiraClientResolver.Resolve(product.JiraVersion);
        var jiraIssueTypes = await jiraClient.GetIssueTypesAsync(
            connection,
            product.JiraProjectKey,
            cancellationToken);

        var timestamp = DateTime.UtcNow;
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newMappings = jiraIssueTypes
            .Select(issueType => new IssueTypeMapping
            {
                ProductId = product.Id,
                IssueTypeCode = BuildUniqueIssueTypeCode(issueType, usedCodes),
                JiraIssueTypeId = issueType.Id,
                JiraIssueTypeName = issueType.Name,
                IsActive = true,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            })
            .ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingIssueTypeIds = await _dbContext.IssueTypeMappings
            .Where(mapping => mapping.ProductId == product.Id)
            .Select(mapping => mapping.Id)
            .ToListAsync(cancellationToken);

        if (existingIssueTypeIds.Count > 0)
        {
            var issueFieldMappings = await _dbContext.IssueFieldMappings
                .Where(mapping =>
                    mapping.ProductId == product.Id
                    && mapping.IssueTypeMappingId.HasValue
                    && existingIssueTypeIds.Contains(mapping.IssueTypeMappingId.Value))
                .ToListAsync(cancellationToken);
            var statusMappings = await _dbContext.StatusMappings
                .Where(mapping =>
                    mapping.ProductId == product.Id
                    && mapping.IssueTypeMappingId.HasValue
                    && existingIssueTypeIds.Contains(mapping.IssueTypeMappingId.Value))
                .ToListAsync(cancellationToken);
            var jiraFieldMetadata = await _dbContext.JiraIssueTypeFieldMetadata
                .Where(metadata =>
                    metadata.ProductId == product.Id
                    && existingIssueTypeIds.Contains(metadata.IssueTypeMappingId))
                .ToListAsync(cancellationToken);
            var fieldMappingTemplates = await _dbContext.IssueFieldMappingTemplates
                .Where(template =>
                    template.ProductId == product.Id
                    && existingIssueTypeIds.Contains(template.IssueTypeMappingId))
                .ToListAsync(cancellationToken);
            var issueTypeMappings = await _dbContext.IssueTypeMappings
                .Where(mapping => mapping.ProductId == product.Id)
                .ToListAsync(cancellationToken);

            _dbContext.IssueFieldMappings.RemoveRange(issueFieldMappings);
            _dbContext.StatusMappings.RemoveRange(statusMappings);
            _dbContext.JiraIssueTypeFieldMetadata.RemoveRange(jiraFieldMetadata);
            _dbContext.IssueFieldMappingTemplates.RemoveRange(fieldMappingTemplates);
            _dbContext.IssueTypeMappings.RemoveRange(issueTypeMappings);
        }

        _dbContext.IssueTypeMappings.AddRange(newMappings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var issueTypes = newMappings
            .OrderBy(mapping => mapping.IssueTypeCode)
            .Select(ToIssueTypeResponse)
            .ToList();

        return new SyncIssueTypesAdminResponse(product.Code, issueTypes.Count, issueTypes);
    }

    public async Task<JiraFieldsMetadataAdminResponse> GetJiraFieldsAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);

        var metadata = await _dbContext.JiraIssueTypeFieldMetadata
            .AsNoTracking()
            .Where(field => field.ProductId == product.Id && field.IssueTypeMappingId == issueType.Id)
            .OrderBy(field => field.Required ? 0 : 1)
            .ThenBy(field => field.Name)
            .ThenBy(field => field.FieldId)
            .ToListAsync(cancellationToken);

        return ToJiraFieldsMetadataAdminResponse(product, issueType, metadata);
    }

    public async Task<JiraFieldsMetadataAdminResponse> SyncJiraFieldsFromJiraAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(issueType.JiraIssueTypeId))
        {
            throw new RequestValidationException("Jira issue type id is required to reload Jira fields.");
        }

        var credential = await GetCredentialByProductAsync(product.Id, product.Code, cancellationToken);
        var connection = ToJiraConnection(product, credential);
        var jiraClient = _jiraClientResolver.Resolve(product.JiraVersion);
        var fields = await jiraClient.GetIssueTypeFieldsAsync(
            connection,
            product.JiraProjectKey,
            issueType.JiraIssueTypeId,
            cancellationToken);

        var timestamp = DateTime.UtcNow;
        var metadata = fields
            .Select(field => ToJiraIssueTypeFieldMetadata(product.Id, issueType.Id, field, timestamp))
            .ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingMetadata = await _dbContext.JiraIssueTypeFieldMetadata
            .Where(field => field.ProductId == product.Id && field.IssueTypeMappingId == issueType.Id)
            .ToListAsync(cancellationToken);

        _dbContext.JiraIssueTypeFieldMetadata.RemoveRange(existingMetadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.JiraIssueTypeFieldMetadata.AddRange(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToJiraFieldsMetadataAdminResponse(product, issueType, metadata);
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

    public async Task<IReadOnlyList<FieldMappingTemplateAdminResponse>> GetFieldMappingTemplatesAsync(
        string productCode,
        string issueTypeCode,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        await EnsureDefaultFieldMappingTemplateAsync(product.Id, issueType.Id, cancellationToken);

        return await GetFieldMappingTemplateResponsesAsync(product.Id, issueType.Id, cancellationToken);
    }

    public async Task<FieldMappingTemplateAdminResponse> CreateFieldMappingTemplateAsync(
        string productCode,
        string issueTypeCode,
        CreateFieldMappingTemplateAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        var templateCode = NormalizeTemplateCode(request.TemplateCode, nameof(request.TemplateCode));
        if (string.Equals(templateCode, DefaultTemplateCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException("Template code 'DEFAULT' is reserved.");
        }

        await EnsureDefaultFieldMappingTemplateAsync(product.Id, issueType.Id, cancellationToken);

        if (await _dbContext.IssueFieldMappingTemplates.AnyAsync(
                template =>
                    template.ProductId == product.Id
                    && template.IssueTypeMappingId == issueType.Id
                    && template.TemplateCode == templateCode,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Field mapping template '{templateCode}' already exists for issue type '{issueType.IssueTypeCode}'.");
        }

        var timestamp = DateTime.UtcNow;
        var template = new IssueFieldMappingTemplate
        {
            ProductId = product.Id,
            IssueTypeMappingId = issueType.Id,
            TemplateCode = templateCode,
            Name = NormalizeRequired(request.Name, nameof(request.Name)),
            Description = NormalizeOptional(request.Description),
            IsDefault = false,
            IsActive = request.IsActive,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.IssueFieldMappingTemplates.Add(template);

        if (request.CopyMappings)
        {
            var sourceTemplateCode = NormalizeOptional(request.SourceTemplateCode) is { } source
                ? NormalizeTemplateCode(source, nameof(request.SourceTemplateCode))
                : DefaultTemplateCode;
            var sourceMappings = await _dbContext.IssueFieldMappings
                .AsNoTracking()
                .Where(mapping =>
                    mapping.ProductId == product.Id
                    && mapping.IssueTypeMappingId == issueType.Id
                    && mapping.TemplateCode == sourceTemplateCode)
                .ToListAsync(cancellationToken);

            var copiedMappings = sourceMappings
                .Select(mapping => CopyFieldMapping(mapping, templateCode, timestamp))
                .ToList();
            _dbContext.IssueFieldMappings.AddRange(copiedMappings);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var mappingCount = await CountFieldMappingsAsync(
            product.Id,
            issueType.Id,
            template.TemplateCode,
            cancellationToken);

        return ToFieldMappingTemplateResponse(template, mappingCount);
    }

    public async Task DeleteFieldMappingTemplateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.IssueFieldMappingTemplates
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (template is null)
        {
            throw new ConfigNotFoundException($"Field mapping template id '{id}' was not found.");
        }

        if (template.IsDefault || string.Equals(template.TemplateCode, DefaultTemplateCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException("Default field mapping template cannot be deleted.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var mappings = await _dbContext.IssueFieldMappings
            .Where(mapping =>
                mapping.ProductId == template.ProductId
                && mapping.IssueTypeMappingId == template.IssueTypeMappingId
                && mapping.TemplateCode == template.TemplateCode)
            .ToListAsync(cancellationToken);

        _dbContext.IssueFieldMappings.RemoveRange(mappings);
        _dbContext.IssueFieldMappingTemplates.Remove(template);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueFieldMappingAdminResponse>> GetFieldMappingsAsync(
        string productCode,
        string issueTypeCode,
        string? templateCode = null,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        var normalizedTemplateCode = await ResolveExistingTemplateCodeAsync(
            product.Id,
            issueType.Id,
            templateCode,
            cancellationToken);

        var mappings = await _dbContext.IssueFieldMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.ProductId == product.Id
                && mapping.IssueTypeMappingId == issueType.Id
                && mapping.TemplateCode == normalizedTemplateCode)
            .OrderBy(mapping => mapping.SortOrder)
            .ThenBy(mapping => mapping.SourcePath)
            .ToListAsync(cancellationToken);

        return mappings.Select(ToFieldMappingResponse).ToList();
    }

    public async Task<IssueFieldMappingAdminResponse> CreateFieldMappingAsync(
        string productCode,
        string issueTypeCode,
        string? templateCode,
        UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        var normalizedTemplateCode = await ResolveExistingTemplateCodeAsync(
            product.Id,
            issueType.Id,
            templateCode,
            cancellationToken);
        var sourcePath = NormalizeRequired(request.SourcePath, nameof(request.SourcePath));

        if (await _dbContext.IssueFieldMappings.AnyAsync(
                mapping =>
                    mapping.ProductId == product.Id
                    && mapping.IssueTypeMappingId == issueType.Id
                    && mapping.TemplateCode == normalizedTemplateCode
                    && mapping.SourcePath == sourcePath,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Field mapping '{sourcePath}' already exists for product '{product.Code}', issue type '{issueType.IssueTypeCode}', template '{normalizedTemplateCode}'.");
        }

        var timestamp = DateTime.UtcNow;
        var mapping = new IssueFieldMapping
        {
            ProductId = product.Id,
            IssueTypeMappingId = issueType.Id,
            TemplateCode = normalizedTemplateCode,
            CreatedAt = timestamp
        };

        ApplyFieldMappingRequest(mapping, request, sourcePath, timestamp);
        _dbContext.IssueFieldMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToFieldMappingResponse(mapping);
    }

    public async Task<SetDefaultFieldMappingsAdminResponse> SetEasSubTaskDefaultFieldMappingsAsync(
        string productCode,
        string issueTypeCode,
        string? templateCode = null,
        CancellationToken cancellationToken = default)
    {
        var (product, issueType) = await GetProductAndIssueTypeAsync(productCode, issueTypeCode, cancellationToken);
        if (!string.Equals(product.Code, "EAS", StringComparison.OrdinalIgnoreCase)
            || !IsSubTaskIssueType(issueType.IssueTypeCode))
        {
            throw new RequestValidationException("Default field mappings are only supported for product 'EAS' and issue type 'SUB TASK'.");
        }

        var normalizedTemplateCode = await ResolveExistingTemplateCodeAsync(
            product.Id,
            issueType.Id,
            templateCode,
            cancellationToken);
        var timestamp = DateTime.UtcNow;
        var mappings = BuildEasSubTaskDefaultFieldMappings(
            product.Id,
            issueType.Id,
            timestamp)
            .Select(mapping =>
            {
                mapping.TemplateCode = normalizedTemplateCode;
                return mapping;
            })
            .ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingMappings = await _dbContext.IssueFieldMappings
            .Where(mapping =>
                mapping.ProductId == product.Id
                && mapping.IssueTypeMappingId == issueType.Id
                && mapping.TemplateCode == normalizedTemplateCode)
            .ToListAsync(cancellationToken);

        _dbContext.IssueFieldMappings.RemoveRange(existingMappings);

        issueType.JiraIssueTypeId = "6";
        issueType.JiraIssueTypeName = string.IsNullOrWhiteSpace(issueType.JiraIssueTypeName)
            ? "Sub-task"
            : issueType.JiraIssueTypeName;
        issueType.IsActive = true;
        issueType.UpdatedAt = timestamp;

        _dbContext.IssueFieldMappings.AddRange(mappings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var responses = mappings
            .OrderBy(mapping => mapping.SortOrder)
            .Select(ToFieldMappingResponse)
            .ToList();

        return new SetDefaultFieldMappingsAdminResponse(
            product.Code,
            issueType.IssueTypeCode,
            responses.Count,
            responses);
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
                    && item.TemplateCode == mapping.TemplateCode
                    && item.SourcePath == sourcePath,
                cancellationToken))
        {
            throw new RequestValidationException(
                $"Field mapping '{sourcePath}' already exists in this product, issue type, and template scope.");
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
                && mapping.TemplateCode == DefaultTemplateCode
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
        mapping.JiraFieldName = NormalizeOptional(request.JiraFieldName);
        mapping.JiraFieldDescription = NormalizeOptional(request.JiraFieldDescription);
        mapping.JiraSchemaType = NormalizeOptional(request.JiraSchemaType);
        mapping.JiraSchemaItems = NormalizeOptional(request.JiraSchemaItems);
        mapping.JiraSchemaSystem = NormalizeOptional(request.JiraSchemaSystem);
        mapping.JiraSchemaCustom = NormalizeOptional(request.JiraSchemaCustom);
        mapping.JiraAllowedValuesJson = NormalizeOptional(request.JiraAllowedValuesJson);
        mapping.JiraDefaultValueJson = NormalizeOptional(request.JiraDefaultValueJson);
        mapping.JiraAutoCompleteUrl = NormalizeOptional(request.JiraAutoCompleteUrl);
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

    private async Task<string> ResolveExistingTemplateCodeAsync(
        int productId,
        int issueTypeId,
        string? templateCode,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeTemplateCodeOrDefault(templateCode);
        await EnsureDefaultFieldMappingTemplateAsync(productId, issueTypeId, cancellationToken);

        var exists = await _dbContext.IssueFieldMappingTemplates.AnyAsync(
            template =>
                template.ProductId == productId
                && template.IssueTypeMappingId == issueTypeId
                && template.TemplateCode == normalizedTemplateCode
                && template.IsActive,
            cancellationToken);
        if (!exists)
        {
            throw new ConfigNotFoundException($"Field mapping template '{normalizedTemplateCode}' was not found.");
        }

        return normalizedTemplateCode;
    }

    private async Task EnsureDefaultFieldMappingTemplateAsync(
        int productId,
        int issueTypeId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.IssueFieldMappingTemplates.AnyAsync(
            template =>
                template.ProductId == productId
                && template.IssueTypeMappingId == issueTypeId
                && template.TemplateCode == DefaultTemplateCode,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var timestamp = DateTime.UtcNow;
        _dbContext.IssueFieldMappingTemplates.Add(new IssueFieldMappingTemplate
        {
            ProductId = productId,
            IssueTypeMappingId = issueTypeId,
            TemplateCode = DefaultTemplateCode,
            Name = DefaultTemplateName,
            Description = "Default field mapping template.",
            IsDefault = true,
            IsActive = true,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FieldMappingTemplateAdminResponse>> GetFieldMappingTemplateResponsesAsync(
        int productId,
        int issueTypeId,
        CancellationToken cancellationToken)
    {
        var templates = await _dbContext.IssueFieldMappingTemplates
            .AsNoTracking()
            .Where(template => template.ProductId == productId && template.IssueTypeMappingId == issueTypeId)
            .OrderBy(template => template.IsDefault ? 0 : 1)
            .ThenBy(template => template.Name)
            .ToListAsync(cancellationToken);
        var templateCodes = templates.Select(template => template.TemplateCode).ToArray();
        var counts = await _dbContext.IssueFieldMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.ProductId == productId
                && mapping.IssueTypeMappingId == issueTypeId
                && templateCodes.Contains(mapping.TemplateCode))
            .GroupBy(mapping => mapping.TemplateCode)
            .Select(group => new { TemplateCode = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TemplateCode, item => item.Count, cancellationToken);

        return templates
            .Select(template => ToFieldMappingTemplateResponse(
                template,
                counts.TryGetValue(template.TemplateCode, out var count) ? count : 0))
            .ToList();
    }

    private async Task<int> CountFieldMappingsAsync(
        int productId,
        int issueTypeId,
        string templateCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IssueFieldMappings.CountAsync(
            mapping =>
                mapping.ProductId == productId
                && mapping.IssueTypeMappingId == issueTypeId
                && mapping.TemplateCode == templateCode,
            cancellationToken);
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

    private static JiraConnectionConfig ToJiraConnection(Product product, JiraCredential credential)
    {
        return new JiraConnectionConfig(
            product.JiraBaseUrl,
            product.JiraApiBasePath,
            product.JiraVersion,
            credential.AuthType,
            credential.Username,
            credential.PasswordOrToken);
    }

    private static IReadOnlyList<IssueFieldMapping> BuildEasSubTaskDefaultFieldMappings(
        int productId,
        int issueTypeId,
        DateTime timestamp)
    {
        return
        [
            CreateDefaultFieldMapping(productId, issueTypeId, "data.summary", "summary", "string", "raw", true, "Xử lý bug v5.31.0", 10, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.description", "description", "string", "raw", false, null, 20, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.priority", "priority", "string", "name", false, null, 30, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.reporter", "reporter", "string", "name", false, null, 40, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.assignee", "assignee", "string", "name", false, "anh.phamviet", 50, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.parentKey", "parentKey", "string", "raw", true, "EAS-38560", 60, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.componentIds", "componentIds", "array", "raw", false, """["15690"]""", 70, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.customFields.customfield_12815", "customfield_12815", "object", "raw", false, """{"value":"Development"}""", 80, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.customFields.customfield_14338", "customfield_14338", "object", "raw", false, """{"value":"SX_Development"}""", 90, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.customFields.customfield_13630", "customfield_13630", "date", "raw", false, "2026-11-15", 100, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.customFields.customfield_12413", "customfield_12413", "date", "raw", false, "2026-11-15", 110, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.customFields.customfield_12412", "customfield_12412", "date", "raw", false, "2026-06-15", 120, timestamp),
            CreateDefaultFieldMapping(productId, issueTypeId, "data.worklogs", "worklogs", "array", "raw", false, """[{"started":"2024-11-25T15:05:00.000+0000","timeSpent":"4.7h","comment":"Xử lý bug v4.56.0"}]""", 130, timestamp)
        ];
    }

    private static IssueFieldMapping CreateDefaultFieldMapping(
        int productId,
        int issueTypeId,
        string sourcePath,
        string jiraField,
        string valueType,
        string valueShape,
        bool isRequired,
        string? defaultValue,
        int sortOrder,
        DateTime timestamp)
    {
        return new IssueFieldMapping
        {
            ProductId = productId,
            IssueTypeMappingId = issueTypeId,
            TemplateCode = DefaultTemplateCode,
            SourcePath = sourcePath,
            JiraField = jiraField,
            ValueType = valueType,
            ValueShape = valueShape,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            SortOrder = sortOrder,
            IsActive = true,
            TransformConfigJson = null,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static IssueFieldMapping CopyFieldMapping(
        IssueFieldMapping source,
        string templateCode,
        DateTime timestamp)
    {
        return new IssueFieldMapping
        {
            ProductId = source.ProductId,
            IssueTypeMappingId = source.IssueTypeMappingId,
            TemplateCode = templateCode,
            SourcePath = source.SourcePath,
            JiraField = source.JiraField,
            JiraFieldName = source.JiraFieldName,
            JiraFieldDescription = source.JiraFieldDescription,
            JiraSchemaType = source.JiraSchemaType,
            JiraSchemaItems = source.JiraSchemaItems,
            JiraSchemaSystem = source.JiraSchemaSystem,
            JiraSchemaCustom = source.JiraSchemaCustom,
            JiraAllowedValuesJson = source.JiraAllowedValuesJson,
            JiraDefaultValueJson = source.JiraDefaultValueJson,
            JiraAutoCompleteUrl = source.JiraAutoCompleteUrl,
            ValueType = source.ValueType,
            ValueShape = source.ValueShape,
            IsRequired = source.IsRequired,
            DefaultValue = source.DefaultValue,
            SortOrder = source.SortOrder,
            IsActive = source.IsActive,
            TransformConfigJson = source.TransformConfigJson,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static bool IsSubTaskIssueType(string issueTypeCode)
    {
        return string.Equals(
            NormalizeIdentifier(issueTypeCode),
            "SUBTASK",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string BuildUniqueIssueTypeCode(
        JiraIssueTypeResponse issueType,
        ISet<string> usedCodes)
    {
        var baseCode = BuildIssueTypeCode(issueType.Name, issueType.Id);
        if (usedCodes.Add(baseCode))
        {
            return baseCode;
        }

        var idSuffix = BuildIssueTypeCode(issueType.Id, issueType.Id);
        var candidate = AppendCodeSuffix(baseCode, idSuffix);
        if (usedCodes.Add(candidate))
        {
            return candidate;
        }

        for (var index = 2; ; index++)
        {
            candidate = AppendCodeSuffix(baseCode, index.ToString());
            if (usedCodes.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string BuildIssueTypeCode(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value;
        var builder = new StringBuilder(source.Length);
        var previousWasSeparator = false;

        foreach (var character in source.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        var code = builder.ToString().Trim('_');
        return TruncateCode(string.IsNullOrWhiteSpace(code) ? $"ISSUETYPE_{fallback}" : code);
    }

    private static string TruncateCode(string code)
    {
        return code.Length <= 100 ? code : code[..100].TrimEnd('_');
    }

    private static string AppendCodeSuffix(string code, string suffix)
    {
        var normalizedSuffix = BuildIssueTypeCode(suffix, suffix);
        var suffixWithSeparator = $"_{normalizedSuffix}";
        var maxCodeLength = Math.Max(1, 100 - suffixWithSeparator.Length);
        var prefix = code.Length <= maxCodeLength ? code : code[..maxCodeLength].TrimEnd('_');

        return $"{prefix}{suffixWithSeparator}";
    }

    private static string NormalizeCode(string? value, string fieldName)
    {
        return NormalizeRequired(value, fieldName).ToUpperInvariant();
    }

    private static string NormalizeTemplateCodeOrDefault(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultTemplateCode
            : NormalizeTemplateCode(value, nameof(value));
    }

    private static string NormalizeTemplateCode(string? value, string fieldName)
    {
        var normalized = NormalizeCode(value, fieldName);
        if (normalized.Length > 100)
        {
            throw new RequestValidationException($"{fieldName} must be 100 characters or less.");
        }

        return normalized;
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

    private static JiraFieldsMetadataAdminResponse ToJiraFieldsMetadataAdminResponse(
        Product product,
        IssueTypeMapping issueType,
        IReadOnlyList<JiraIssueTypeFieldMetadata> metadata)
    {
        var fields = metadata
            .OrderBy(field => field.Required ? 0 : 1)
            .ThenBy(field => field.Name)
            .ThenBy(field => field.FieldId)
            .Select(ToJiraFieldMetadataAdminResponse)
            .ToList();
        var updatedAt = fields.Count == 0 ? null : fields.Max(field => field.UpdatedAt);

        return new JiraFieldsMetadataAdminResponse(
            product.Code,
            issueType.IssueTypeCode,
            updatedAt,
            fields.Count,
            fields);
    }

    private static JiraFieldMetadataAdminResponse ToJiraFieldMetadataAdminResponse(
        JiraIssueFieldMetadataResponse field)
    {
        var recommendation = RecommendFieldMapping(field);

        return new JiraFieldMetadataAdminResponse(
            field.FieldId,
            field.Name,
            field.Required,
            field.Schema.Type,
            field.Schema.Items,
            field.Schema.System,
            field.Schema.Custom,
            field.Schema.CustomId,
            field.HasDefaultValue,
            ToJsonString(field.DefaultValue),
            field.AutoCompleteUrl,
            field.Operations,
            field.AllowedValues.Select(ToAllowedValueAdminResponse).ToList(),
            recommendation.ValueType,
            recommendation.ValueShape,
            null);
    }

    private static JiraFieldMetadataAdminResponse ToJiraFieldMetadataAdminResponse(
        JiraIssueTypeFieldMetadata field)
    {
        return new JiraFieldMetadataAdminResponse(
            field.FieldId,
            field.Name,
            field.Required,
            field.SchemaType,
            field.SchemaItems,
            field.SchemaSystem,
            field.SchemaCustom,
            field.SchemaCustomId,
            field.HasDefaultValue,
            field.DefaultValueJson,
            field.AutoCompleteUrl,
            ParseStringList(field.OperationsJson),
            ParseAllowedValues(field.AllowedValuesJson),
            field.RecommendedValueType,
            field.RecommendedValueShape,
            field.UpdatedAt);
    }

    private static JiraIssueTypeFieldMetadata ToJiraIssueTypeFieldMetadata(
        int productId,
        int issueTypeMappingId,
        JiraIssueFieldMetadataResponse field,
        DateTime timestamp)
    {
        var recommendation = RecommendFieldMapping(field);

        return new JiraIssueTypeFieldMetadata
        {
            ProductId = productId,
            IssueTypeMappingId = issueTypeMappingId,
            FieldId = field.FieldId,
            Name = field.Name,
            Required = field.Required,
            SchemaType = field.Schema.Type,
            SchemaItems = field.Schema.Items,
            SchemaSystem = field.Schema.System,
            SchemaCustom = field.Schema.Custom,
            SchemaCustomId = field.Schema.CustomId,
            HasDefaultValue = field.HasDefaultValue,
            DefaultValueJson = ToJsonString(field.DefaultValue),
            AutoCompleteUrl = field.AutoCompleteUrl,
            OperationsJson = SerializeStringList(field.Operations),
            AllowedValuesJson = SerializeAllowedValues(field.AllowedValues),
            RecommendedValueType = recommendation.ValueType,
            RecommendedValueShape = recommendation.ValueShape,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static JiraAllowedValueAdminResponse ToAllowedValueAdminResponse(
        JiraAllowedValueResponse value)
    {
        return new JiraAllowedValueAdminResponse(
            value.Id,
            value.Key,
            value.Name,
            value.Value,
            value.Description,
            value.Disabled,
            value.Raw.GetRawText());
    }

    private static string? SerializeStringList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private static string? SerializeAllowedValues(IReadOnlyList<JiraAllowedValueResponse> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(values.Select(ToAllowedValueAdminResponse).ToList(), JsonOptions);
    }

    private static IReadOnlyList<string> ParseStringList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<JiraAllowedValueAdminResponse> ParseAllowedValues(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<JiraAllowedValueAdminResponse>>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static (string ValueType, string ValueShape) RecommendFieldMapping(
        JiraIssueFieldMetadataResponse field)
    {
        var schemaType = NormalizeMetadataToken(field.Schema.Type);
        var schemaItems = NormalizeMetadataToken(field.Schema.Items);

        if (string.Equals(schemaType, "array", StringComparison.Ordinal))
        {
            return schemaItems switch
            {
                "component" or "version" => ("array", "arrayOfId"),
                _ => ("array", "raw")
            };
        }

        return schemaType switch
        {
            "date" or "datetime" => ("date", "raw"),
            "number" => ("number", "raw"),
            "option" => ("string", "value"),
            "priority" => ("string", "name"),
            "user" => ("string", "name"),
            "boolean" => ("boolean", "raw"),
            _ => ("string", "raw")
        };
    }

    private static string? ToJsonString(JsonElement? value)
    {
        if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.Value.GetRawText();
    }

    private static string? NormalizeMetadataToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
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

    private static FieldMappingTemplateAdminResponse ToFieldMappingTemplateResponse(
        IssueFieldMappingTemplate template,
        int mappingCount)
    {
        return new FieldMappingTemplateAdminResponse(
            template.Id,
            template.ProductId,
            template.IssueTypeMappingId,
            template.TemplateCode,
            template.Name,
            template.Description,
            template.IsDefault,
            template.IsActive,
            mappingCount,
            template.CreatedAt,
            template.UpdatedAt);
    }

    private static IssueFieldMappingAdminResponse ToFieldMappingResponse(IssueFieldMapping mapping)
    {
        return new IssueFieldMappingAdminResponse(
            mapping.Id,
            mapping.ProductId,
            mapping.IssueTypeMappingId,
            mapping.TemplateCode,
            mapping.SourcePath,
            mapping.JiraField,
            mapping.JiraFieldName,
            mapping.JiraFieldDescription,
            mapping.JiraSchemaType,
            mapping.JiraSchemaItems,
            mapping.JiraSchemaSystem,
            mapping.JiraSchemaCustom,
            mapping.JiraAllowedValuesJson,
            mapping.JiraDefaultValueJson,
            mapping.JiraAutoCompleteUrl,
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
