using JiraIntegrationService.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace JiraIntegrationService.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthResponse>> Get()
    {
        var response = new HealthResponse("OK");

        return Ok(ApiResponse<HealthResponse>.Ok(response, TraceId.From(HttpContext)));
    }
}
