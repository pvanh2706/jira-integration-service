using JiraIntegrationService.Api.Application.Admin;
using JiraIntegrationService.Api.Application.Admin.Models;
using JiraIntegrationService.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace JiraIntegrationService.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminConfigurationController : ControllerBase
{
    private readonly IAdminConfigurationService _adminConfigurationService;

    public AdminConfigurationController(IAdminConfigurationService adminConfigurationService)
    {
        _adminConfigurationService = adminConfigurationService;
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductAdminResponse>>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetProductsAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ProductAdminResponse>>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<ProductAdminResponse>>> CreateProduct(
        [FromBody] CreateProductAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.CreateProductAsync(request, cancellationToken);

        return Ok(ApiResponse<ProductAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}")]
    public async Task<ActionResult<ApiResponse<ProductAdminResponse>>> GetProduct(
        string code,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetProductAsync(code, cancellationToken);

        return Ok(ApiResponse<ProductAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPut("products/{code}")]
    public async Task<ActionResult<ApiResponse<ProductAdminResponse>>> UpdateProduct(
        string code,
        [FromBody] UpdateProductAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.UpdateProductAsync(code, request, cancellationToken);

        return Ok(ApiResponse<ProductAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpDelete("products/{code}")]
    public async Task<ActionResult<ApiResponse<DeleteAdminResponse>>> DeleteProduct(
        string code,
        CancellationToken cancellationToken)
    {
        await _adminConfigurationService.DeleteProductAsync(code, cancellationToken);

        return Ok(ApiResponse<DeleteAdminResponse>.Ok(new DeleteAdminResponse(true), TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/credential")]
    public async Task<ActionResult<ApiResponse<JiraCredentialAdminResponse>>> GetCredential(
        string code,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetCredentialAsync(code, cancellationToken);

        return Ok(ApiResponse<JiraCredentialAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPut("products/{code}/credential")]
    public async Task<ActionResult<ApiResponse<JiraCredentialAdminResponse>>> UpsertCredential(
        string code,
        [FromBody] UpsertJiraCredentialAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.UpsertCredentialAsync(code, request, cancellationToken);

        return Ok(ApiResponse<JiraCredentialAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/issue-types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueTypeMappingAdminResponse>>>> GetIssueTypes(
        string code,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetIssueTypesAsync(code, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<IssueTypeMappingAdminResponse>>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types")]
    public async Task<ActionResult<ApiResponse<IssueTypeMappingAdminResponse>>> CreateIssueType(
        string code,
        [FromBody] CreateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.CreateIssueTypeAsync(code, request, cancellationToken);

        return Ok(ApiResponse<IssueTypeMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/sync-from-jira")]
    public async Task<ActionResult<ApiResponse<SyncIssueTypesAdminResponse>>> SyncIssueTypesFromJira(
        string code,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.SyncIssueTypesFromJiraAsync(code, cancellationToken);

        return Ok(ApiResponse<SyncIssueTypesAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/issue-types/{issueTypeCode}/jira-fields")]
    public async Task<ActionResult<ApiResponse<JiraFieldsMetadataAdminResponse>>> GetJiraFields(
        string code,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetJiraFieldsAsync(
            code,
            issueTypeCode,
            cancellationToken);

        return Ok(ApiResponse<JiraFieldsMetadataAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/{issueTypeCode}/jira-fields/sync-from-jira")]
    public async Task<ActionResult<ApiResponse<JiraFieldsMetadataAdminResponse>>> SyncJiraFieldsFromJira(
        string code,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.SyncJiraFieldsFromJiraAsync(
            code,
            issueTypeCode,
            cancellationToken);

        return Ok(ApiResponse<JiraFieldsMetadataAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPut("products/{code}/issue-types/{issueTypeCode}")]
    public async Task<ActionResult<ApiResponse<IssueTypeMappingAdminResponse>>> UpdateIssueType(
        string code,
        string issueTypeCode,
        [FromBody] UpdateIssueTypeMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.UpdateIssueTypeAsync(
            code,
            issueTypeCode,
            request,
            cancellationToken);

        return Ok(ApiResponse<IssueTypeMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/issue-types/{issueTypeCode}/field-mapping-templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FieldMappingTemplateAdminResponse>>>> GetFieldMappingTemplates(
        string code,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetFieldMappingTemplatesAsync(
            code,
            issueTypeCode,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<FieldMappingTemplateAdminResponse>>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/{issueTypeCode}/field-mapping-templates")]
    public async Task<ActionResult<ApiResponse<FieldMappingTemplateAdminResponse>>> CreateFieldMappingTemplate(
        string code,
        string issueTypeCode,
        [FromBody] CreateFieldMappingTemplateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.CreateFieldMappingTemplateAsync(
            code,
            issueTypeCode,
            request,
            cancellationToken);

        return Ok(ApiResponse<FieldMappingTemplateAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpDelete("field-mapping-templates/{id:int}")]
    public async Task<ActionResult<ApiResponse<DeleteAdminResponse>>> DeleteFieldMappingTemplate(
        int id,
        CancellationToken cancellationToken)
    {
        await _adminConfigurationService.DeleteFieldMappingTemplateAsync(id, cancellationToken);

        return Ok(ApiResponse<DeleteAdminResponse>.Ok(new DeleteAdminResponse(true), TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/issue-types/{issueTypeCode}/field-mappings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueFieldMappingAdminResponse>>>> GetFieldMappings(
        string code,
        string issueTypeCode,
        [FromQuery] string? templateCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetFieldMappingsAsync(
            code,
            issueTypeCode,
            templateCode,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<IssueFieldMappingAdminResponse>>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/{issueTypeCode}/field-mappings")]
    public async Task<ActionResult<ApiResponse<IssueFieldMappingAdminResponse>>> CreateFieldMapping(
        string code,
        string issueTypeCode,
        [FromQuery] string? templateCode,
        [FromBody] UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.CreateFieldMappingAsync(
            code,
            issueTypeCode,
            templateCode,
            request,
            cancellationToken);

        return Ok(ApiResponse<IssueFieldMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/{issueTypeCode}/field-mappings/eas-sub-task-defaults")]
    public async Task<ActionResult<ApiResponse<SetDefaultFieldMappingsAdminResponse>>> SetEasSubTaskDefaultFieldMappings(
        string code,
        string issueTypeCode,
        [FromQuery] string? templateCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.SetEasSubTaskDefaultFieldMappingsAsync(
            code,
            issueTypeCode,
            templateCode,
            cancellationToken);

        return Ok(ApiResponse<SetDefaultFieldMappingsAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPut("field-mappings/{id:int}")]
    public async Task<ActionResult<ApiResponse<IssueFieldMappingAdminResponse>>> UpdateFieldMapping(
        int id,
        [FromBody] UpsertIssueFieldMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.UpdateFieldMappingAsync(id, request, cancellationToken);

        return Ok(ApiResponse<IssueFieldMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpDelete("field-mappings/{id:int}")]
    public async Task<ActionResult<ApiResponse<DeleteAdminResponse>>> DeleteFieldMapping(
        int id,
        CancellationToken cancellationToken)
    {
        await _adminConfigurationService.DeleteFieldMappingAsync(id, cancellationToken);

        return Ok(ApiResponse<DeleteAdminResponse>.Ok(new DeleteAdminResponse(true), TraceId.From(HttpContext)));
    }

    [HttpGet("products/{code}/issue-types/{issueTypeCode}/status-mappings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StatusMappingAdminResponse>>>> GetStatusMappings(
        string code,
        string issueTypeCode,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.GetStatusMappingsAsync(code, issueTypeCode, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<StatusMappingAdminResponse>>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/issue-types/{issueTypeCode}/status-mappings")]
    public async Task<ActionResult<ApiResponse<StatusMappingAdminResponse>>> CreateStatusMapping(
        string code,
        string issueTypeCode,
        [FromBody] UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.CreateStatusMappingAsync(
            code,
            issueTypeCode,
            request,
            cancellationToken);

        return Ok(ApiResponse<StatusMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPut("status-mappings/{id:int}")]
    public async Task<ActionResult<ApiResponse<StatusMappingAdminResponse>>> UpdateStatusMapping(
        int id,
        [FromBody] UpsertStatusMappingAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.UpdateStatusMappingAsync(id, request, cancellationToken);

        return Ok(ApiResponse<StatusMappingAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpDelete("status-mappings/{id:int}")]
    public async Task<ActionResult<ApiResponse<DeleteAdminResponse>>> DeleteStatusMapping(
        int id,
        CancellationToken cancellationToken)
    {
        await _adminConfigurationService.DeleteStatusMappingAsync(id, cancellationToken);

        return Ok(ApiResponse<DeleteAdminResponse>.Ok(new DeleteAdminResponse(true), TraceId.From(HttpContext)));
    }

    [HttpPost("products/{code}/validate-create-issue-config")]
    public async Task<ActionResult<ApiResponse<ValidateCreateIssueConfigAdminResponse>>> ValidateCreateIssueConfig(
        string code,
        [FromBody] ValidateCreateIssueConfigAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminConfigurationService.ValidateCreateIssueConfigAsync(
            code,
            request,
            cancellationToken);

        return Ok(ApiResponse<ValidateCreateIssueConfigAdminResponse>.Ok(result, TraceId.From(HttpContext)));
    }
}
