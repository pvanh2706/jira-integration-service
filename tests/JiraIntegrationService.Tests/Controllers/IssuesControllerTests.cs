using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Issues;
using JiraIntegrationService.Api.Application.Issues.Models;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JiraIntegrationService.Tests.Controllers;

public sealed class IssuesControllerTests
{
    [Fact]
    public async Task CreateIssue_WhenRequestIsValid_ShouldReturnStandardSuccessResponse()
    {
        var issueService = new FakeIssueService
        {
            Result = new CreateIssueResult("10001", "CRM-123")
        };
        await using var factory = CreateFactory(issueService);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsJsonAsync(
            "/api/issues/create",
            new
            {
                productCode = "CRM",
                issueTypeCode = "BUG",
                data = new
                {
                    summary = "Customer cannot submit order"
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("10001", root.GetProperty("data").GetProperty("jiraIssueId").GetString());
        Assert.Equal("CRM-123", root.GetProperty("data").GetProperty("jiraIssueKey").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.NotNull(issueService.CapturedRequest);
    }

    [Fact]
    public async Task CreateIssue_WhenSummaryIsMissing_ShouldReturnValidationError()
    {
        var issueService = new FakeIssueService();
        await using var factory = CreateFactory(issueService);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsJsonAsync(
            "/api/issues/create",
            new
            {
                productCode = "CRM",
                issueTypeCode = "BUG"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(ErrorCodes.ValidationError, root.GetProperty("errorCode").GetString());
        Assert.Equal("Invalid request.", root.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.Null(issueService.CapturedRequest);
    }

    [Fact]
    public async Task PreviewCreateIssue_WhenRequestIsValid_ShouldReturnJiraPayload()
    {
        var issueService = new FakeIssueService
        {
            PreviewResult = new CreateIssuePreviewResult(
                new CreateJiraIssueRequest(
                    ProjectKey: "CRM",
                    IssueTypeName: "Bug",
                    Summary: "Customer cannot submit order",
                    CustomFields: new Dictionary<string, object?>
                    {
                        ["customfield_10010"] = "C001"
                    }))
        };
        await using var factory = CreateFactory(issueService);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsJsonAsync(
            "/api/issues/create/preview",
            new
            {
                productCode = "CRM",
                issueTypeCode = "BUG",
                data = new
                {
                    summary = "Customer cannot submit order",
                    customer = new
                    {
                        code = "C001"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        var jiraRequest = root.GetProperty("data").GetProperty("jiraRequest");

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("CRM", jiraRequest.GetProperty("projectKey").GetString());
        Assert.Equal("Bug", jiraRequest.GetProperty("issueTypeName").GetString());
        Assert.Equal("Customer cannot submit order", jiraRequest.GetProperty("summary").GetString());
        Assert.Equal(
            "C001",
            jiraRequest.GetProperty("customFields").GetProperty("customfield_10010").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.NotNull(issueService.CapturedPreviewRequest);
    }

    [Fact]
    public async Task UpdateIssueStatus_WhenRequestIsValid_ShouldReturnStandardSuccessResponse()
    {
        var issueService = new FakeIssueService
        {
            UpdateStatusResult = new UpdateIssueStatusResult("10001", "CRM-123", "IN_PROGRESS")
        };
        await using var factory = CreateFactory(issueService);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsJsonAsync(
            "/api/issues/status/update",
            new
            {
                productCode = "CRM",
                jiraIssueId = "10001",
                jiraIssueKey = "CRM-123",
                issueTypeCode = "BUG",
                standardStatus = "IN_PROGRESS"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("10001", root.GetProperty("data").GetProperty("jiraIssueId").GetString());
        Assert.Equal("CRM-123", root.GetProperty("data").GetProperty("jiraIssueKey").GetString());
        Assert.Equal("IN_PROGRESS", root.GetProperty("data").GetProperty("standardStatus").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.NotNull(issueService.CapturedUpdateStatusRequest);
    }

    [Fact]
    public async Task GetIssueStatus_WhenRequestIsValid_ShouldReturnStandardSuccessResponse()
    {
        var issueService = new FakeIssueService
        {
            GetStatusResult = new GetIssueStatusResult("DONE")
        };
        await using var factory = CreateFactory(issueService);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.GetAsync(
            "/api/issues/status?productCode=CRM&jiraIssueKey=CRM-123&issueTypeCode=BUG");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("DONE", root.GetProperty("data").GetProperty("standardStatus").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.NotNull(issueService.CapturedGetStatusRequest);
        Assert.Equal("CRM", issueService.CapturedGetStatusRequest!.ProductCode);
        Assert.Equal("CRM-123", issueService.CapturedGetStatusRequest.JiraIssueKey);
        Assert.Equal("BUG", issueService.CapturedGetStatusRequest.IssueTypeCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeIssueService issueService)
    {
        return new TestApplicationFactory(services =>
        {
            services.RemoveAll<IIssueService>();
            services.AddSingleton<IIssueService>(issueService);
        });
    }

    private sealed class FakeIssueService : IIssueService
    {
        public CreateIssueResult Result { get; init; } = new("10001", "CRM-123");

        public CreateIssuePreviewResult PreviewResult { get; init; } = new(
            new CreateJiraIssueRequest(
                ProjectKey: "CRM",
                IssueTypeName: "Bug",
                Summary: "Preview summary"));

        public UpdateIssueStatusResult UpdateStatusResult { get; init; } = new(
            JiraIssueId: "10001",
            JiraIssueKey: "CRM-123",
            StandardStatus: "IN_PROGRESS");

        public GetIssueStatusResult GetStatusResult { get; init; } = new("IN_PROGRESS");

        public CreateIssueRequest? CapturedRequest { get; private set; }

        public CreateIssueRequest? CapturedPreviewRequest { get; private set; }

        public UpdateIssueStatusRequest? CapturedUpdateStatusRequest { get; private set; }

        public GetIssueStatusRequest? CapturedGetStatusRequest { get; private set; }

        public Task<CreateIssueResult> CreateIssueAsync(
            CreateIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedRequest = request;

            return Task.FromResult(Result);
        }

        public Task<CreateIssuePreviewResult> PreviewCreateIssueAsync(
            CreateIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedPreviewRequest = request;

            return Task.FromResult(PreviewResult);
        }

        public Task<UpdateIssueStatusResult> UpdateIssueStatusAsync(
            UpdateIssueStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedUpdateStatusRequest = request;

            return Task.FromResult(UpdateStatusResult);
        }

        public Task<GetIssueStatusResult> GetIssueStatusAsync(
            GetIssueStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedGetStatusRequest = request;

            return Task.FromResult(GetStatusResult);
        }
    }
}
