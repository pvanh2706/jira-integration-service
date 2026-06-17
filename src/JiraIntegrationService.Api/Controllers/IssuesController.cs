using JiraIntegrationService.Api.Application.Issues;
using JiraIntegrationService.Api.Application.Issues.Models;
using JiraIntegrationService.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace JiraIntegrationService.Api.Controllers;

[ApiController]
[Route("api/issues")]
public sealed class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<CreateIssueResult>>> CreateIssue(
        [FromBody] CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.CreateIssueAsync(request, cancellationToken);

        return Ok(ApiResponse<CreateIssueResult>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("create/preview")]
    public async Task<ActionResult<ApiResponse<CreateIssuePreviewResult>>> PreviewCreateIssue(
        [FromBody] CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.PreviewCreateIssueAsync(request, cancellationToken);

        return Ok(ApiResponse<CreateIssuePreviewResult>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpPost("status/update")]
    public async Task<ActionResult<ApiResponse<UpdateIssueStatusResult>>> UpdateIssueStatus(
        [FromBody] UpdateIssueStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.UpdateIssueStatusAsync(request, cancellationToken);

        return Ok(ApiResponse<UpdateIssueStatusResult>.Ok(result, TraceId.From(HttpContext)));
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<GetIssueStatusResult>>> GetIssueStatus(
        [FromQuery] GetIssueStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetIssueStatusAsync(request, cancellationToken);

        return Ok(ApiResponse<GetIssueStatusResult>.Ok(result, TraceId.From(HttpContext)));
    }
}
