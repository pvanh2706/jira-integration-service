using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Issues.Models;
using JiraIntegrationService.Api.Application.Issues.Mapping;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Application.Issues;

public sealed class IssueService : IIssueService
{
    private const string UnknownStatus = "UNKNOWN";

    private static readonly HashSet<string> AllowedUpdateStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPEN",
        "IN_PROGRESS",
        "WAITING",
        "DONE",
        "CANCELLED"
    };

    private readonly IProductConfigService _productConfigService;
    private readonly IJiraIssuePayloadBuilder _jiraIssuePayloadBuilder;
    private readonly IJiraClientResolver _jiraClientResolver;
    private readonly ILogger<IssueService> _logger;

    public IssueService(
        IProductConfigService productConfigService,
        IJiraIssuePayloadBuilder jiraIssuePayloadBuilder,
        IJiraClientResolver jiraClientResolver,
        ILogger<IssueService> logger)
    {
        _productConfigService = productConfigService;
        _jiraIssuePayloadBuilder = jiraIssuePayloadBuilder;
        _jiraClientResolver = jiraClientResolver;
        _logger = logger;
    }

    public async Task<CreateIssueResult> CreateIssueAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        var createIssueContext = await BuildCreateIssueContextAsync(request, cancellationToken);
        var credential = await _productConfigService.GetJiraCredentialAsync(
            createIssueContext.Product.Code,
            cancellationToken);
        var jiraClient = _jiraClientResolver.Resolve(createIssueContext.Product.JiraVersion);
        var jiraConnection = BuildJiraConnection(createIssueContext.Product, credential);

        _logger.LogInformation(
            "CreateIssue started. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}",
            createIssueContext.Product.Code,
            createIssueContext.IssueType.IssueTypeCode);

        var jiraResponse = await jiraClient.CreateIssueAsync(
            jiraConnection,
            createIssueContext.JiraRequest,
            cancellationToken);

        _logger.LogInformation(
            "CreateIssue completed. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}. JiraIssueId: {JiraIssueId}. JiraIssueKey: {JiraIssueKey}",
            createIssueContext.Product.Code,
            createIssueContext.IssueType.IssueTypeCode,
            jiraResponse.Id,
            jiraResponse.Key);

        return new CreateIssueResult(jiraResponse.Id, jiraResponse.Key);
    }

    public async Task<CreateIssuePreviewResult> PreviewCreateIssueAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        var createIssueContext = await BuildCreateIssueContextAsync(request, cancellationToken);

        _logger.LogInformation(
            "PreviewCreateIssue completed. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}",
            createIssueContext.Product.Code,
            createIssueContext.IssueType.IssueTypeCode);

        return new CreateIssuePreviewResult(createIssueContext.JiraRequest);
    }

    public async Task<UpdateIssueStatusResult> UpdateIssueStatusAsync(
        UpdateIssueStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productCode = NormalizeRequired(request.ProductCode, nameof(request.ProductCode));
        var issueId = NormalizeOptional(request.JiraIssueId);
        var issueKey = NormalizeOptional(request.JiraIssueKey);
        var issueTypeCode = NormalizeOptional(request.IssueTypeCode);
        var standardStatus = NormalizeStandardStatus(request.StandardStatus);

        if (issueId is null && issueKey is null)
        {
            throw new RequestValidationException("jiraIssueId or jiraIssueKey is required.");
        }

        var jiraIssueKeyForJira = issueId is null ? issueKey : null;
        var product = await _productConfigService.GetProductAsync(productCode, cancellationToken);
        var credential = await _productConfigService.GetJiraCredentialAsync(
            product.Code,
            cancellationToken);
        var transitionConfig = await _productConfigService.GetStatusTransitionAsync(
            product.Code,
            issueTypeCode,
            standardStatus,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(transitionConfig.JiraTransitionId)
            && string.IsNullOrWhiteSpace(transitionConfig.JiraTransitionName))
        {
            throw new ConfigNotFoundException(
                $"Jira transition config was not found for product '{product.Code}' and status '{standardStatus}'.");
        }
        var jiraClient = _jiraClientResolver.Resolve(product.JiraVersion);
        var jiraConnection = BuildJiraConnection(product, credential);

        _logger.LogInformation(
            "UpdateStatus started. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}. JiraIssueId: {JiraIssueId}. JiraIssueKey: {JiraIssueKey}. StandardStatus: {StandardStatus}",
            product.Code,
            issueTypeCode,
            issueId,
            issueKey,
            standardStatus);

        var availableTransitions = await jiraClient.GetTransitionsAsync(
            jiraConnection,
            issueId,
            jiraIssueKeyForJira,
            cancellationToken);
        var selectedTransition = SelectTransition(transitionConfig, availableTransitions);

        if (selectedTransition is null)
        {
            throw new JiraClientException(
                $"Jira transition is not available for status '{standardStatus}'.");
        }

        await jiraClient.TransitionIssueAsync(
            jiraConnection,
            new TransitionJiraIssueRequest(
                JiraIssueId: issueId,
                JiraIssueKey: jiraIssueKeyForJira,
                TransitionId: selectedTransition.Id),
            cancellationToken);

        var jiraStatusAfterTransition = await jiraClient.GetIssueStatusAsync(
            jiraConnection,
            issueId,
            jiraIssueKeyForJira,
            cancellationToken);
        if (!string.Equals(
                jiraStatusAfterTransition.StatusName,
                transitionConfig.JiraStatusName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new JiraClientException(
                $"Jira status after transition was '{jiraStatusAfterTransition.StatusName}', expected '{transitionConfig.JiraStatusName}'.");
        }

        _logger.LogInformation(
            "UpdateStatus completed. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}. JiraIssueId: {JiraIssueId}. JiraIssueKey: {JiraIssueKey}. StandardStatus: {StandardStatus}. JiraTransitionId: {JiraTransitionId}. JiraStatusName: {JiraStatusName}",
            product.Code,
            issueTypeCode,
            issueId,
            issueKey,
            standardStatus,
            selectedTransition.Id,
            jiraStatusAfterTransition.StatusName);

        return new UpdateIssueStatusResult(
            JiraIssueId: issueId,
            JiraIssueKey: issueKey,
            StandardStatus: standardStatus);
    }

    public async Task<GetIssueStatusResult> GetIssueStatusAsync(
        GetIssueStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productCode = NormalizeRequired(request.ProductCode, nameof(request.ProductCode));
        var issueId = NormalizeOptional(request.JiraIssueId);
        var issueKey = NormalizeOptional(request.JiraIssueKey);
        var issueTypeCode = NormalizeOptional(request.IssueTypeCode);

        if (issueId is null && issueKey is null)
        {
            throw new RequestValidationException("jiraIssueId or jiraIssueKey is required.");
        }

        var jiraIssueKeyForJira = issueId is null ? issueKey : null;
        var product = await _productConfigService.GetProductAsync(productCode, cancellationToken);
        var credential = await _productConfigService.GetJiraCredentialAsync(
            product.Code,
            cancellationToken);
        var jiraClient = _jiraClientResolver.Resolve(product.JiraVersion);
        var jiraConnection = BuildJiraConnection(product, credential);

        _logger.LogInformation(
            "GetStatus started. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}. JiraIssueId: {JiraIssueId}. JiraIssueKey: {JiraIssueKey}",
            product.Code,
            issueTypeCode,
            issueId,
            issueKey);

        var jiraStatus = await jiraClient.GetIssueStatusAsync(
            jiraConnection,
            issueId,
            jiraIssueKeyForJira,
            cancellationToken);
        var standardStatus = await _productConfigService.MapJiraStatusToStandardStatusAsync(
            product.Code,
            issueTypeCode,
            jiraStatus.StatusName,
            cancellationToken);

        _logger.LogInformation(
            "GetStatus completed. ProductCode: {ProductCode}. IssueTypeCode: {IssueTypeCode}. JiraIssueId: {JiraIssueId}. JiraIssueKey: {JiraIssueKey}. JiraStatusName: {JiraStatusName}. StandardStatus: {StandardStatus}",
            product.Code,
            issueTypeCode,
            issueId,
            issueKey,
            jiraStatus.StatusName,
            standardStatus);

        return new GetIssueStatusResult(standardStatus);
    }

    private static JiraConnectionConfig BuildJiraConnection(
        ProductConfig product,
        JiraCredentialConfig credential)
    {
        return new JiraConnectionConfig(
            product.JiraBaseUrl,
            product.JiraApiBasePath,
            product.JiraVersion,
            credential.AuthType,
            credential.Username,
            credential.Password);
    }

    private async Task<CreateIssueContext> BuildCreateIssueContextAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productCode = NormalizeRequired(request.ProductCode, nameof(request.ProductCode));
        var issueTypeCode = NormalizeRequired(request.IssueTypeCode, nameof(request.IssueTypeCode));
        var data = request.Data ?? throw new RequestValidationException("data is required.");
        if (data.ValueKind != JsonValueKind.Object)
        {
            throw new RequestValidationException("data must be a JSON object.");
        }

        var product = await _productConfigService.GetProductAsync(productCode, cancellationToken);
        var issueType = await _productConfigService.GetIssueTypeAsync(
            product.Code,
            issueTypeCode,
            cancellationToken);
        var fieldMappings = await _productConfigService.GetFieldMappingsAsync(
            product.Code,
            issueType.IssueTypeCode,
            cancellationToken);

        var jiraRequest = _jiraIssuePayloadBuilder.BuildCreateIssueRequest(
            product,
            issueType,
            fieldMappings,
            data);

        return new CreateIssueContext(product, issueType, jiraRequest);
    }

    /// <summary>
    /// Hàm chuẩn hóa dữ liệu đầu vào
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    /// <exception cref="RequestValidationException"></exception>
    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RequestValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeStandardStatus(string? value)
    {
        var normalizedStatus = NormalizeRequired(value, nameof(UpdateIssueStatusRequest.StandardStatus))
            .ToUpperInvariant();

        if (normalizedStatus == UnknownStatus)
        {
            throw new RequestValidationException("standardStatus cannot be UNKNOWN.");
        }

        if (!AllowedUpdateStatuses.Contains(normalizedStatus))
        {
            throw new RequestValidationException($"standardStatus '{normalizedStatus}' is not supported.");
        }

        return normalizedStatus;
    }

    private static JiraTransitionResponse? SelectTransition(
        StatusTransitionConfig transitionConfig,
        IReadOnlyList<JiraTransitionResponse> availableTransitions)
    {
        if (!string.IsNullOrWhiteSpace(transitionConfig.JiraTransitionId))
        {
            var transitionById = availableTransitions.SingleOrDefault(transition =>
                string.Equals(
                    transition.Id,
                    transitionConfig.JiraTransitionId.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (transitionById is not null)
            {
                return transitionById;
            }
        }

        if (!string.IsNullOrWhiteSpace(transitionConfig.JiraTransitionName))
        {
            return availableTransitions.SingleOrDefault(transition =>
                string.Equals(
                    transition.Name,
                    transitionConfig.JiraTransitionName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record CreateIssueContext(
        ProductConfig Product,
        IssueTypeConfig IssueType,
        CreateJiraIssueRequest JiraRequest);
}
