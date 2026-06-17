using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JiraIntegrationService.Tests.Controllers;

public sealed class AdminConfigurationControllerTests
{
    [Fact]
    public async Task AdminConfiguration_WhenProductIsConfigured_ShouldValidateCreateIssueConfig()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var createProductResponse = await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "ops",
                name = "Operations",
                jiraProjectKey = "ops",
                jiraBaseUrl = "https://jira-ops.example.com",
                jiraApiBasePath = "/rest/api/2",
                jiraVersion = "ServerV2",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, createProductResponse.StatusCode);
        using var productJson = JsonDocument.Parse(await createProductResponse.Content.ReadAsStringAsync());
        Assert.Equal("OPS", productJson.RootElement.GetProperty("data").GetProperty("code").GetString());
        Assert.Equal("OPS", productJson.RootElement.GetProperty("data").GetProperty("jiraProjectKey").GetString());

        var credentialResponse = await client.PutAsJsonAsync(
            "/api/admin/products/ops/credential",
            new
            {
                authType = "Basic",
                username = "ops-jira-user",
                passwordOrToken = "secret-token",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, credentialResponse.StatusCode);
        using var credentialJson = JsonDocument.Parse(await credentialResponse.Content.ReadAsStringAsync());
        Assert.True(credentialJson.RootElement.GetProperty("data").GetProperty("hasPasswordOrToken").GetBoolean());
        Assert.False(credentialJson.RootElement.GetProperty("data").TryGetProperty("passwordOrToken", out _));

        var issueTypeResponse = await client.PostAsJsonAsync(
            "/api/admin/products/ops/issue-types",
            new
            {
                issueTypeCode = "incident",
                jiraIssueTypeName = "Incident",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, issueTypeResponse.StatusCode);

        var fieldMappingResponse = await client.PostAsJsonAsync(
            "/api/admin/products/ops/issue-types/incident/field-mappings",
            new
            {
                sourcePath = "data.title",
                jiraField = "summary",
                valueType = "string",
                valueShape = "raw",
                isRequired = true,
                sortOrder = 10,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, fieldMappingResponse.StatusCode);
        using var fieldJson = JsonDocument.Parse(await fieldMappingResponse.Content.ReadAsStringAsync());
        Assert.Equal("data.title", fieldJson.RootElement.GetProperty("data").GetProperty("sourcePath").GetString());
        Assert.Equal("summary", fieldJson.RootElement.GetProperty("data").GetProperty("jiraField").GetString());

        var statusMappingResponse = await client.PostAsJsonAsync(
            "/api/admin/products/ops/issue-types/incident/status-mappings",
            new
            {
                standardStatus = "in_progress",
                jiraStatusName = "In Progress",
                jiraTransitionId = "31",
                jiraTransitionName = "Start Progress",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, statusMappingResponse.StatusCode);
        using var statusJson = JsonDocument.Parse(await statusMappingResponse.Content.ReadAsStringAsync());
        Assert.Equal("IN_PROGRESS", statusJson.RootElement.GetProperty("data").GetProperty("standardStatus").GetString());

        var validateResponse = await client.PostAsJsonAsync(
            "/api/admin/products/ops/validate-create-issue-config",
            new
            {
                issueTypeCode = "incident"
            });

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        using var validateJson = JsonDocument.Parse(await validateResponse.Content.ReadAsStringAsync());
        var validateData = validateJson.RootElement.GetProperty("data");
        Assert.True(validateData.GetProperty("isValid").GetBoolean());
        Assert.Empty(validateData.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task CreateProduct_WhenProductAlreadyExists_ShouldReturnValidationError()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "crm",
                name = "CRM",
                jiraProjectKey = "CRM",
                jiraBaseUrl = "https://jira.example.com"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(ErrorCodes.ValidationError, json.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task ValidateCreateIssueConfig_WhenCredentialIsMissing_ShouldReturnInvalidResult()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "helpdesk",
                name = "Helpdesk",
                jiraProjectKey = "HLP",
                jiraBaseUrl = "https://jira-helpdesk.example.com"
            });

        await client.PostAsJsonAsync(
            "/api/admin/products/helpdesk/issue-types",
            new
            {
                issueTypeCode = "ticket",
                jiraIssueTypeName = "Task"
            });

        await client.PostAsJsonAsync(
            "/api/admin/products/helpdesk/issue-types/ticket/field-mappings",
            new
            {
                sourcePath = "data.summary",
                jiraField = "summary"
            });

        var response = await client.PostAsJsonAsync(
            "/api/admin/products/helpdesk/validate-create-issue-config",
            new
            {
                issueTypeCode = "ticket"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.False(data.GetProperty("isValid").GetBoolean());
        Assert.Contains(
            data.GetProperty("errors").EnumerateArray(),
            item => item.GetString() == "Active Jira credential is required.");
    }

    [Fact]
    public async Task CreateIssue_WhenProductWasConfiguredByAdminApi_ShouldCreateIssueWithoutCodeChange()
    {
        var jiraClient = new FakeJiraClient();
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IJiraClient>();
            services.AddSingleton<IJiraClient>(jiraClient);
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "portal",
                name = "Portal",
                jiraProjectKey = "PRT",
                jiraBaseUrl = "https://jira-portal.example.com"
            });
        await client.PutAsJsonAsync(
            "/api/admin/products/portal/credential",
            new
            {
                username = "portal-user",
                passwordOrToken = "portal-token"
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/portal/issue-types",
            new
            {
                issueTypeCode = "bug",
                jiraIssueTypeName = "Bug"
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/portal/issue-types/bug/field-mappings",
            new
            {
                sourcePath = "data.title",
                jiraField = "summary",
                isRequired = true,
                sortOrder = 10
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/portal/issue-types/bug/field-mappings",
            new
            {
                sourcePath = "data.customer.code",
                jiraField = "customfield_10010",
                sortOrder = 20
            });

        var response = await client.PostAsJsonAsync(
            "/api/issues/create",
            new
            {
                productCode = "portal",
                issueTypeCode = "bug",
                data = new
                {
                    title = "Portal login failed",
                    customer = new
                    {
                        code = "C001"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("PRT-1", json.RootElement.GetProperty("data").GetProperty("jiraIssueKey").GetString());

        Assert.NotNull(jiraClient.CapturedCreateRequest);
        Assert.Equal("PRT", jiraClient.CapturedCreateRequest!.ProjectKey);
        Assert.Equal("Bug", jiraClient.CapturedCreateRequest.IssueTypeName);
        Assert.Equal("Portal login failed", jiraClient.CapturedCreateRequest.Summary);
        Assert.Equal("C001", jiraClient.CapturedCreateRequest.CustomFields!["customfield_10010"]);
        Assert.Equal("https://jira-portal.example.com", jiraClient.CapturedConnection!.BaseUrl);
    }

    [Fact]
    public async Task PreviewCreateIssue_WhenProductWasConfiguredByAdminApi_ShouldReturnPayloadWithoutCallingJira()
    {
        var jiraClient = new FakeJiraClient();
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IJiraClient>();
            services.AddSingleton<IJiraClient>(jiraClient);
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "preview",
                name = "Preview",
                jiraProjectKey = "PRV",
                jiraBaseUrl = "https://jira-preview.example.com"
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/preview/issue-types",
            new
            {
                issueTypeCode = "bug",
                jiraIssueTypeName = "Bug"
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/preview/issue-types/bug/field-mappings",
            new
            {
                sourcePath = "data.title",
                jiraField = "summary",
                isRequired = true,
                sortOrder = 10
            });
        await client.PostAsJsonAsync(
            "/api/admin/products/preview/issue-types/bug/field-mappings",
            new
            {
                sourcePath = "data.customer.code",
                jiraField = "customfield_10010",
                sortOrder = 20
            });

        var response = await client.PostAsJsonAsync(
            "/api/issues/create/preview",
            new
            {
                productCode = "preview",
                issueTypeCode = "bug",
                data = new
                {
                    title = "Preview payload",
                    customer = new
                    {
                        code = "C001"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var jiraRequest = json.RootElement.GetProperty("data").GetProperty("jiraRequest");

        Assert.Equal("PRV", jiraRequest.GetProperty("projectKey").GetString());
        Assert.Equal("Bug", jiraRequest.GetProperty("issueTypeName").GetString());
        Assert.Equal("Preview payload", jiraRequest.GetProperty("summary").GetString());
        Assert.Equal(
            "C001",
            jiraRequest.GetProperty("customFields").GetProperty("customfield_10010").GetString());
        Assert.Null(jiraClient.CapturedCreateRequest);
        Assert.Null(jiraClient.CapturedConnection);
    }

    private sealed class FakeJiraClient : IJiraClient
    {
        public JiraConnectionConfig? CapturedConnection { get; private set; }

        public CreateJiraIssueRequest? CapturedCreateRequest { get; private set; }

        public Task<CreateJiraIssueResponse> CreateIssueAsync(
            JiraConnectionConfig connection,
            CreateJiraIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedCreateRequest = request;

            return Task.FromResult(new CreateJiraIssueResponse("10001", "PRT-1"));
        }

        public Task<JiraIssueStatusResponse> GetIssueStatusAsync(
            JiraConnectionConfig connection,
            string? jiraIssueId,
            string? jiraIssueKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new JiraIssueStatusResponse("In Progress"));
        }

        public Task<IReadOnlyList<JiraTransitionResponse>> GetTransitionsAsync(
            JiraConnectionConfig connection,
            string? jiraIssueId,
            string? jiraIssueKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<JiraTransitionResponse>>([]);
        }

        public Task TransitionIssueAsync(
            JiraConnectionConfig connection,
            TransitionJiraIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
