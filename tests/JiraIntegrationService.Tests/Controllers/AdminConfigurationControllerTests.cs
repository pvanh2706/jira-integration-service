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
    public async Task FieldMapping_WhenMetadataIsProvided_ShouldPersistMetadata()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/products/eas/issue-types/bug/field-mappings",
            new
            {
                sourcePath = "data.technicalIssueType",
                jiraField = "customfield_12815",
                jiraFieldName = "Technical Issue Type",
                jiraFieldDescription = "Loai cong viec ky thuat dung cho KPI.",
                jiraSchemaType = "option",
                jiraSchemaCustom = "com.atlassian.jira.plugin.system.customfieldtypes:select",
                jiraAllowedValuesJson = """[{"id":"11776","value":"Development"}]""",
                jiraDefaultValueJson = """{"id":"11776","value":"Development"}""",
                valueType = "string",
                valueShape = "value",
                isRequired = false,
                defaultValue = "Development",
                sortOrder = 90,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var created = createJson.RootElement.GetProperty("data");
        var mappingId = created.GetProperty("id").GetInt32();
        Assert.Equal("Technical Issue Type", created.GetProperty("jiraFieldName").GetString());
        Assert.Equal(
            "Loai cong viec ky thuat dung cho KPI.",
            created.GetProperty("jiraFieldDescription").GetString());
        Assert.Equal("option", created.GetProperty("jiraSchemaType").GetString());
        Assert.Equal("value", created.GetProperty("valueShape").GetString());

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/field-mappings/{mappingId}",
            new
            {
                sourcePath = "data.technicalIssueType",
                jiraField = "customfield_12815",
                jiraFieldName = "Technical Issue Type",
                jiraFieldDescription = "Mo ta da cap nhat.",
                jiraSchemaType = "option",
                jiraSchemaCustom = "com.atlassian.jira.plugin.system.customfieldtypes:select",
                jiraAllowedValuesJson = """[{"id":"11776","value":"Development"}]""",
                jiraDefaultValueJson = """{"id":"11776","value":"Development"}""",
                valueType = "string",
                valueShape = "value",
                isRequired = false,
                defaultValue = "Development",
                sortOrder = 90,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var mappingsResponse = await client.GetAsync("/api/admin/products/eas/issue-types/bug/field-mappings");

        Assert.Equal(HttpStatusCode.OK, mappingsResponse.StatusCode);
        using var mappingsJson = JsonDocument.Parse(await mappingsResponse.Content.ReadAsStringAsync());
        var mappings = mappingsJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        var mapping = Assert.Single(mappings, item => item.GetProperty("id").GetInt32() == mappingId);
        Assert.Equal("Technical Issue Type", mapping.GetProperty("jiraFieldName").GetString());
        Assert.Equal("Mo ta da cap nhat.", mapping.GetProperty("jiraFieldDescription").GetString());
        Assert.Equal(
            """[{"id":"11776","value":"Development"}]""",
            mapping.GetProperty("jiraAllowedValuesJson").GetString());
        Assert.Equal(
            """{"id":"11776","value":"Development"}""",
            mapping.GetProperty("jiraDefaultValueJson").GetString());
    }

    [Fact]
    public async Task FieldMappingTemplates_WhenIssueTypeExists_ShouldReturnDefaultTemplateAndScopeMappings()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var productResponse = await client.PostAsJsonAsync(
            "/api/admin/products",
            new
            {
                code = "template-test",
                name = "Template Test",
                jiraProjectKey = "TPL",
                jiraBaseUrl = "https://jira-template.example.com"
            });
        Assert.Equal(HttpStatusCode.OK, productResponse.StatusCode);

        var issueTypeResponse = await client.PostAsJsonAsync(
            "/api/admin/products/template-test/issue-types",
            new
            {
                issueTypeCode = "SUB TASK",
                jiraIssueTypeName = "Sub-task",
                isActive = true
            });
        Assert.Equal(HttpStatusCode.OK, issueTypeResponse.StatusCode);

        var templatesResponse = await client.GetAsync(
            "/api/admin/products/template-test/issue-types/SUB%20TASK/field-mapping-templates");

        Assert.Equal(HttpStatusCode.OK, templatesResponse.StatusCode);
        using (var templatesJson = JsonDocument.Parse(await templatesResponse.Content.ReadAsStringAsync()))
        {
            var defaultTemplate = Assert.Single(templatesJson.RootElement.GetProperty("data").EnumerateArray());
            Assert.Equal("DEFAULT", defaultTemplate.GetProperty("templateCode").GetString());
            Assert.True(defaultTemplate.GetProperty("isDefault").GetBoolean());
            Assert.Equal(0, defaultTemplate.GetProperty("mappingCount").GetInt32());
        }

        var createTemplateResponse = await client.PostAsJsonAsync(
            "/api/admin/products/template-test/issue-types/SUB%20TASK/field-mapping-templates",
            new
            {
                templateCode = "fast_create",
                name = "Fast create",
                copyMappings = false,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, createTemplateResponse.StatusCode);
        using var createTemplateJson = JsonDocument.Parse(await createTemplateResponse.Content.ReadAsStringAsync());
        Assert.Equal("FAST_CREATE", createTemplateJson.RootElement.GetProperty("data").GetProperty("templateCode").GetString());

        var createMappingResponse = await client.PostAsJsonAsync(
            "/api/admin/products/template-test/issue-types/SUB%20TASK/field-mappings?templateCode=FAST_CREATE",
            new
            {
                sourcePath = "data.summary",
                jiraField = "summary",
                valueType = "string",
                valueShape = "raw",
                isRequired = true,
                sortOrder = 10,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, createMappingResponse.StatusCode);

        var defaultMappingsResponse = await client.GetAsync(
            "/api/admin/products/template-test/issue-types/SUB%20TASK/field-mappings");
        var fastMappingsResponse = await client.GetAsync(
            "/api/admin/products/template-test/issue-types/SUB%20TASK/field-mappings?templateCode=FAST_CREATE");

        Assert.Equal(HttpStatusCode.OK, defaultMappingsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fastMappingsResponse.StatusCode);

        using var defaultMappingsJson = JsonDocument.Parse(await defaultMappingsResponse.Content.ReadAsStringAsync());
        using var fastMappingsJson = JsonDocument.Parse(await fastMappingsResponse.Content.ReadAsStringAsync());
        Assert.Empty(defaultMappingsJson.RootElement.GetProperty("data").EnumerateArray());

        var fastMapping = Assert.Single(fastMappingsJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal("FAST_CREATE", fastMapping.GetProperty("templateCode").GetString());
        Assert.Equal("data.summary", fastMapping.GetProperty("sourcePath").GetString());
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
                code = "eas",
                name = "EAS",
                jiraProjectKey = "EAS",
                jiraBaseUrl = "https://jira.ezcloudhotel.com"
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
    public async Task SyncIssueTypesFromJira_WhenProductIsConfigured_ShouldReplaceIssueTypes()
    {
        var jiraClient = new FakeJiraClient
        {
            IssueTypes =
            [
                new JiraIssueTypeResponse("1", "Bug", "Problem", false),
                new JiraIssueTypeResponse("10010", "Service Request", "Request", false)
            ]
        };
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IJiraClient>();
            services.AddSingleton<IJiraClient>(jiraClient);
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var response = await client.PostAsync(
            "/api/admin/products/eas/issue-types/sync-from-jira",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var syncJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var syncData = syncJson.RootElement.GetProperty("data");
        Assert.Equal("EAS", syncData.GetProperty("productCode").GetString());
        Assert.Equal(2, syncData.GetProperty("total").GetInt32());
        Assert.Equal("EAS", jiraClient.CapturedIssueTypesProjectKey);
        Assert.Equal("https://jira.ezcloudhotel.com", jiraClient.CapturedConnection!.BaseUrl);

        var issueTypesResponse = await client.GetAsync("/api/admin/products/eas/issue-types");

        Assert.Equal(HttpStatusCode.OK, issueTypesResponse.StatusCode);
        using var issueTypesJson = JsonDocument.Parse(await issueTypesResponse.Content.ReadAsStringAsync());
        var issueTypes = issueTypesJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.Equal(2, issueTypes.Length);
        Assert.Contains(issueTypes, item =>
            item.GetProperty("issueTypeCode").GetString() == "BUG"
            && item.GetProperty("jiraIssueTypeId").GetString() == "1"
            && item.GetProperty("jiraIssueTypeName").GetString() == "Bug");
        Assert.Contains(issueTypes, item =>
            item.GetProperty("issueTypeCode").GetString() == "SERVICE_REQUEST"
            && item.GetProperty("jiraIssueTypeId").GetString() == "10010");
        Assert.DoesNotContain(issueTypes, item => item.GetProperty("issueTypeCode").GetString() == "TASK");
    }

    [Fact]
    public async Task GetJiraFields_WhenIssueTypeHasJiraId_ShouldReturnMetadataWithRecommendations()
    {
        var jiraClient = new FakeJiraClient
        {
            IssueFields =
            [
                new JiraIssueFieldMetadataResponse(
                    FieldId: "customfield_12815",
                    Name: "Technical Issue Type",
                    Required: false,
                    Schema: new JiraIssueFieldSchemaResponse(
                        Type: "option",
                        Items: null,
                        System: null,
                        Custom: "com.atlassian.jira.plugin.system.customfieldtypes:select",
                        CustomId: 12815),
                    HasDefaultValue: false,
                    DefaultValue: null,
                    Operations: ["set"],
                    AllowedValues:
                    [
                        new JiraAllowedValueResponse(
                            Id: "11776",
                            Key: null,
                            Name: null,
                            Value: "Development",
                            Description: null,
                            Disabled: false,
                            Raw: JsonSerializer.SerializeToElement(new
                            {
                                id = "11776",
                                value = "Development",
                                disabled = false
                            }))
                    ],
                    AutoCompleteUrl: null),
                new JiraIssueFieldMetadataResponse(
                    FieldId: "components",
                    Name: "Component/s",
                    Required: true,
                    Schema: new JiraIssueFieldSchemaResponse(
                        Type: "array",
                        Items: "component",
                        System: "components",
                        Custom: null,
                        CustomId: null),
                    HasDefaultValue: false,
                    DefaultValue: null,
                    Operations: ["add", "set"],
                    AllowedValues:
                    [
                        new JiraAllowedValueResponse(
                            Id: "15690",
                            Key: null,
                            Name: "ezFolio",
                            Value: null,
                            Description: "Folio component",
                            Disabled: false,
                            Raw: JsonSerializer.SerializeToElement(new
                            {
                                id = "15690",
                                name = "ezFolio",
                                description = "Folio component"
                            }))
                    ],
                    AutoCompleteUrl: null),
                new JiraIssueFieldMetadataResponse(
                    FieldId: "priority",
                    Name: "Priority",
                    Required: false,
                    Schema: new JiraIssueFieldSchemaResponse(
                        Type: "priority",
                        Items: null,
                        System: "priority",
                        Custom: null,
                        CustomId: null),
                    HasDefaultValue: true,
                    DefaultValue: JsonSerializer.SerializeToElement(new
                    {
                        id = "10102",
                        name = "Medium"
                    }),
                    Operations: ["set"],
                    AllowedValues: [],
                    AutoCompleteUrl: null),
                new JiraIssueFieldMetadataResponse(
                    FieldId: "customfield_12412",
                    Name: "Start date",
                    Required: false,
                    Schema: new JiraIssueFieldSchemaResponse(
                        Type: "date",
                        Items: null,
                        System: null,
                        Custom: "com.atlassian.jira.plugin.system.customfieldtypes:datepicker",
                        CustomId: 12412),
                    HasDefaultValue: false,
                    DefaultValue: null,
                    Operations: ["set"],
                    AllowedValues: [],
                    AutoCompleteUrl: null),
                new JiraIssueFieldMetadataResponse(
                    FieldId: "customfield_14025",
                    Name: "Point",
                    Required: false,
                    Schema: new JiraIssueFieldSchemaResponse(
                        Type: "number",
                        Items: null,
                        System: null,
                        Custom: "com.atlassian.jira.plugin.system.customfieldtypes:float",
                        CustomId: 14025),
                    HasDefaultValue: false,
                    DefaultValue: null,
                    Operations: ["set"],
                    AllowedValues: [],
                    AutoCompleteUrl: null)
            ]
        };
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IJiraClient>();
            services.AddSingleton<IJiraClient>(jiraClient);
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var issueTypeResponse = await client.PostAsJsonAsync(
            "/api/admin/products/eas/issue-types",
            new
            {
                issueTypeCode = "subtask",
                jiraIssueTypeId = "6",
                jiraIssueTypeName = "Sub-task",
                isActive = true
            });
        Assert.Equal(HttpStatusCode.OK, issueTypeResponse.StatusCode);

        var cachedBeforeResponse = await client.GetAsync("/api/admin/products/eas/issue-types/subtask/jira-fields");

        Assert.Equal(HttpStatusCode.OK, cachedBeforeResponse.StatusCode);
        using (var cachedBeforeJson = JsonDocument.Parse(await cachedBeforeResponse.Content.ReadAsStringAsync()))
        {
            var cachedBeforeData = cachedBeforeJson.RootElement.GetProperty("data");
            Assert.Equal(0, cachedBeforeData.GetProperty("total").GetInt32());
            Assert.Empty(cachedBeforeData.GetProperty("fields").EnumerateArray());
            Assert.False(cachedBeforeData.TryGetProperty("updatedAt", out _));
        }
        Assert.Equal(0, jiraClient.IssueFieldsCallCount);

        var response = await client.PostAsync(
            "/api/admin/products/eas/issue-types/subtask/jira-fields/sync-from-jira",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        var fields = data.GetProperty("fields").EnumerateArray().ToArray();

        Assert.Equal("EAS", data.GetProperty("productCode").GetString());
        Assert.Equal("SUBTASK", data.GetProperty("issueTypeCode").GetString());
        Assert.Equal(5, data.GetProperty("total").GetInt32());
        Assert.Equal(5, fields.Length);
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("updatedAt").GetString()));
        Assert.Equal("EAS", jiraClient.CapturedFieldsProjectKey);
        Assert.Equal("6", jiraClient.CapturedFieldsIssueTypeId);
        Assert.Equal(1, jiraClient.IssueFieldsCallCount);

        var components = Assert.Single(fields, item => item.GetProperty("fieldId").GetString() == "components");
        Assert.Equal("components", components.GetProperty("fieldId").GetString());
        Assert.True(components.GetProperty("required").GetBoolean());
        Assert.Equal("array", components.GetProperty("recommendedValueType").GetString());
        Assert.Equal("arrayOfId", components.GetProperty("recommendedValueShape").GetString());
        Assert.Equal("15690", components.GetProperty("allowedValues")[0].GetProperty("id").GetString());

        var option = Assert.Single(fields, item => item.GetProperty("fieldId").GetString() == "customfield_12815");
        Assert.Equal("customfield_12815", option.GetProperty("fieldId").GetString());
        Assert.Equal("string", option.GetProperty("recommendedValueType").GetString());
        Assert.Equal("value", option.GetProperty("recommendedValueShape").GetString());
        Assert.Equal("Development", option.GetProperty("allowedValues")[0].GetProperty("value").GetString());

        var priority = Assert.Single(fields, item => item.GetProperty("fieldId").GetString() == "priority");
        Assert.Equal("string", priority.GetProperty("recommendedValueType").GetString());
        Assert.Equal("name", priority.GetProperty("recommendedValueShape").GetString());
        Assert.Contains("Medium", priority.GetProperty("defaultValueJson").GetString());

        var startDate = Assert.Single(fields, item => item.GetProperty("fieldId").GetString() == "customfield_12412");
        Assert.Equal("date", startDate.GetProperty("recommendedValueType").GetString());
        Assert.Equal("raw", startDate.GetProperty("recommendedValueShape").GetString());

        var point = Assert.Single(fields, item => item.GetProperty("fieldId").GetString() == "customfield_14025");
        Assert.Equal("number", point.GetProperty("recommendedValueType").GetString());
        Assert.Equal("raw", point.GetProperty("recommendedValueShape").GetString());

        var cachedAfterResponse = await client.GetAsync("/api/admin/products/eas/issue-types/subtask/jira-fields");

        Assert.Equal(HttpStatusCode.OK, cachedAfterResponse.StatusCode);
        using var cachedAfterJson = JsonDocument.Parse(await cachedAfterResponse.Content.ReadAsStringAsync());
        var cachedAfterData = cachedAfterJson.RootElement.GetProperty("data");
        Assert.Equal(5, cachedAfterData.GetProperty("total").GetInt32());
        Assert.Equal(5, cachedAfterData.GetProperty("fields").GetArrayLength());
        Assert.Equal(1, jiraClient.IssueFieldsCallCount);
    }

    [Fact]
    public async Task SyncJiraFields_WhenIssueTypeHasNoJiraId_ShouldReturnValidationError()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var issueTypeResponse = await client.PostAsJsonAsync(
            "/api/admin/products/eas/issue-types",
            new
            {
                issueTypeCode = "manual",
                jiraIssueTypeName = "Task",
                isActive = true
            });
        Assert.Equal(HttpStatusCode.OK, issueTypeResponse.StatusCode);

        var response = await client.PostAsync(
            "/api/admin/products/eas/issue-types/manual/jira-fields/sync-from-jira",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(
            "Jira issue type id is required to reload Jira fields.",
            json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SetEasSubTaskDefaultFieldMappings_WhenScopeMatches_ShouldOverwriteMappings()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalAuthMiddleware.HeaderName, "test-token");

        var issueTypeResponse = await client.PostAsJsonAsync(
            "/api/admin/products/eas/issue-types",
            new
            {
                issueTypeCode = "SUB TASK",
                jiraIssueTypeName = "Sub-task",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.OK, issueTypeResponse.StatusCode);

        var oldMappingResponse = await client.PostAsJsonAsync(
            "/api/admin/products/eas/issue-types/SUB%20TASK/field-mappings",
            new
            {
                sourcePath = "data.old",
                jiraField = "customfield_old"
            });

        Assert.Equal(HttpStatusCode.OK, oldMappingResponse.StatusCode);

        var response = await client.PostAsync(
            "/api/admin/products/eas/issue-types/SUB%20TASK/field-mappings/eas-sub-task-defaults",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("EAS", data.GetProperty("productCode").GetString());
        Assert.Equal("SUB TASK", data.GetProperty("issueTypeCode").GetString());
        Assert.Equal(13, data.GetProperty("total").GetInt32());

        var mappingsResponse = await client.GetAsync(
            "/api/admin/products/eas/issue-types/SUB%20TASK/field-mappings");

        Assert.Equal(HttpStatusCode.OK, mappingsResponse.StatusCode);
        using var mappingsJson = JsonDocument.Parse(await mappingsResponse.Content.ReadAsStringAsync());
        var mappings = mappingsJson.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(13, mappings.Length);
        Assert.DoesNotContain(mappings, item => item.GetProperty("sourcePath").GetString() == "data.old");
        Assert.Contains(mappings, item =>
            item.GetProperty("sourcePath").GetString() == "data.summary"
            && item.GetProperty("jiraField").GetString() == "summary"
            && item.GetProperty("isRequired").GetBoolean());
        Assert.Contains(mappings, item =>
            item.GetProperty("sourcePath").GetString() == "data.componentIds"
            && item.GetProperty("defaultValue").GetString() == """["15690"]""");
        Assert.Contains(mappings, item =>
            item.GetProperty("sourcePath").GetString() == "data.customFields.customfield_12815"
            && item.GetProperty("jiraField").GetString() == "customfield_12815");
        Assert.Contains(mappings, item =>
            item.GetProperty("sourcePath").GetString() == "data.worklogs"
            && item.GetProperty("valueType").GetString() == "array");
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

        public string? CapturedIssueTypesProjectKey { get; private set; }

        public string? CapturedFieldsProjectKey { get; private set; }

        public string? CapturedFieldsIssueTypeId { get; private set; }

        public int IssueFieldsCallCount { get; private set; }

        public IReadOnlyList<JiraIssueTypeResponse> IssueTypes { get; init; } = [];

        public IReadOnlyList<JiraIssueFieldMetadataResponse> IssueFields { get; init; } = [];

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

        public Task<IReadOnlyList<JiraIssueTypeResponse>> GetIssueTypesAsync(
            JiraConnectionConfig connection,
            string projectKey,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedIssueTypesProjectKey = projectKey;

            return Task.FromResult(IssueTypes);
        }

        public Task<IReadOnlyList<JiraIssueFieldMetadataResponse>> GetIssueTypeFieldsAsync(
            JiraConnectionConfig connection,
            string projectKey,
            string issueTypeId,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedFieldsProjectKey = projectKey;
            CapturedFieldsIssueTypeId = issueTypeId;
            IssueFieldsCallCount++;

            return Task.FromResult(IssueFields);
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
