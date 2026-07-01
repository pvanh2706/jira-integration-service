using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Issues;
using JiraIntegrationService.Api.Application.Issues.Mapping;
using JiraIntegrationService.Api.Application.Issues.Models;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace JiraIntegrationService.Tests.Application.Issues;

public sealed class IssueServiceTests
{
    [Fact]
    public async Task CreateIssueAsync_WhenRequestIsValid_ShouldMapPayloadAndReturnJiraIssueIdAndKey()
    {
        var configService = new FakeProductConfigService
        {
            FieldMappings =
            [
                new FieldMappingConfig(
                    Id: 1,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.summary",
                    JiraField: "summary",
                    IsRequired: true,
                    DefaultValue: null,
                    SortOrder: 1),
                new FieldMappingConfig(
                    Id: 2,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.description",
                    JiraField: "description",
                    IsRequired: false,
                    DefaultValue: null,
                    SortOrder: 2),
                new FieldMappingConfig(
                    Id: 3,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.priority",
                    JiraField: "priority",
                    IsRequired: false,
                    DefaultValue: null,
                    ValueShape: "name",
                    SortOrder: 3),
                new FieldMappingConfig(
                    Id: 4,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.customer.id",
                    JiraField: "customfield_10010",
                    IsRequired: false,
                    DefaultValue: null,
                    SortOrder: 4),
                new FieldMappingConfig(
                    Id: 5,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.sourceRecordId",
                    JiraField: "customfield_10011",
                    IsRequired: false,
                    DefaultValue: "DEFAULT-SOURCE",
                    SortOrder: 5)
            ]
        };
        var jiraClient = new FakeJiraClient();
        var service = CreateService(configService, jiraClient);

        var result = await service.CreateIssueAsync(new CreateIssueRequest
        {
            ProductCode = " crm ",
            IssueTypeCode = " bug ",
            TemplateCode = "support_fast",
            Data = Data(new
            {
                summary = " Customer cannot submit order ",
                description = " Error occurs after submit. ",
                priority = "High",
                customer = new
                {
                    id = " CUST-001 "
                }
            })
        });

        Assert.Equal("10001", result.JiraIssueId);
        Assert.Equal("CRM-123", result.JiraIssueKey);
        Assert.Equal("support_fast", configService.CapturedTemplateCode);

        var capturedRequest = Assert.IsType<CreateJiraIssueRequest>(jiraClient.CapturedCreateRequest);
        Assert.Equal("CRM", capturedRequest.ProjectKey);
        Assert.Equal("Bug", capturedRequest.IssueTypeName);
        Assert.Equal("Customer cannot submit order", capturedRequest.Summary);
        Assert.Equal("Error occurs after submit.", capturedRequest.Description);
        Assert.Equal("High", capturedRequest.PriorityName);
        Assert.Equal("10001", capturedRequest.IssueTypeId);
        Assert.Equal("https://jira.example.com", jiraClient.CapturedConnection!.BaseUrl);
        Assert.Equal("Basic", jiraClient.CapturedConnection.AuthType);
        Assert.NotNull(capturedRequest.CustomFields);
        Assert.Equal("CUST-001", capturedRequest.CustomFields!["customfield_10010"]);
        Assert.Equal("DEFAULT-SOURCE", capturedRequest.CustomFields!["customfield_10011"]);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenDataIsMissing_ShouldThrowValidationError()
    {
        var service = CreateService(new FakeProductConfigService(), new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateIssueAsync(new CreateIssueRequest
            {
                ProductCode = "CRM",
                IssueTypeCode = "BUG"
            }));

        Assert.Equal(ErrorCodes.ValidationError, exception.ErrorCode);
        Assert.Contains("data", exception.Message);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenRequiredMappedFieldIsMissing_ShouldThrowValidationError()
    {
        var configService = new FakeProductConfigService
        {
            FieldMappings =
            [
                new FieldMappingConfig(
                    Id: 1,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.summary",
                    JiraField: "summary",
                    IsRequired: true,
                    DefaultValue: null),
                new FieldMappingConfig(
                    Id: 2,
                    ProductId: 1,
                    IssueTypeMappingId: 1,
                    SourceField: "data.customer.id",
                    JiraField: "customfield_10010",
                    IsRequired: true,
                    DefaultValue: null)
            ]
        };
        var service = CreateService(configService, new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateIssueAsync(new CreateIssueRequest
            {
                ProductCode = "CRM",
                IssueTypeCode = "BUG",
                Data = Data(new
                {
                    summary = "Customer cannot submit order"
                })
            }));

        Assert.Equal(ErrorCodes.ValidationError, exception.ErrorCode);
        Assert.Contains("data.customer.id", exception.Message);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenProductConfigIsMissing_ShouldPropagateConfigNotFound()
    {
        var configService = new FakeProductConfigService
        {
            ThrowProductConfigNotFound = true
        };
        var service = CreateService(configService, new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<ConfigNotFoundException>(
            () => service.CreateIssueAsync(new CreateIssueRequest
            {
                ProductCode = "UNKNOWN",
                IssueTypeCode = "BUG",
                Data = Data(new
                {
                    summary = "Customer cannot submit order"
                })
            }));

        Assert.Equal(ErrorCodes.ConfigNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenRequestUsesIssueKey_ShouldTransitionIssueByKey()
    {
        var jiraClient = new FakeJiraClient
        {
            AvailableTransitions =
            [
                new JiraTransitionResponse("31", "Start Progress")
            ]
        };
        var service = CreateService(new FakeProductConfigService(), jiraClient);

        var result = await service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueKey = "CRM-123",
            IssueTypeCode = "BUG",
            StandardStatus = "in_progress"
        });

        Assert.Null(result.JiraIssueId);
        Assert.Equal("CRM-123", result.JiraIssueKey);
        Assert.Equal("IN_PROGRESS", result.StandardStatus);
        Assert.Null(jiraClient.CapturedGetTransitionsIssueId);
        Assert.Equal("CRM-123", jiraClient.CapturedGetTransitionsIssueKey);
        Assert.Null(jiraClient.CapturedTransitionRequest!.JiraIssueId);
        Assert.Equal("CRM-123", jiraClient.CapturedTransitionRequest.JiraIssueKey);
        Assert.Equal("31", jiraClient.CapturedTransitionRequest.TransitionId);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenRequestUsesIssueId_ShouldTransitionIssueById()
    {
        var jiraClient = new FakeJiraClient
        {
            AvailableTransitions =
            [
                new JiraTransitionResponse("31", "Start Progress")
            ]
        };
        var service = CreateService(new FakeProductConfigService(), jiraClient);

        var result = await service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueId = "10001",
            IssueTypeCode = "BUG",
            StandardStatus = "IN_PROGRESS"
        });

        Assert.Equal("10001", result.JiraIssueId);
        Assert.Null(result.JiraIssueKey);
        Assert.Equal("10001", jiraClient.CapturedTransitionRequest!.JiraIssueId);
        Assert.Null(jiraClient.CapturedTransitionRequest.JiraIssueKey);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenBothIssueIdAndIssueKeyAreProvided_ShouldUseIssueIdForJiraCalls()
    {
        var jiraClient = new FakeJiraClient
        {
            AvailableTransitions =
            [
                new JiraTransitionResponse("31", "Start Progress")
            ]
        };
        var service = CreateService(new FakeProductConfigService(), jiraClient);

        var result = await service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueId = "10001",
            JiraIssueKey = "CRM-123",
            IssueTypeCode = "BUG",
            StandardStatus = "IN_PROGRESS"
        });

        Assert.Equal("10001", result.JiraIssueId);
        Assert.Equal("CRM-123", result.JiraIssueKey);
        Assert.Equal("10001", jiraClient.CapturedGetTransitionsIssueId);
        Assert.Null(jiraClient.CapturedGetTransitionsIssueKey);
        Assert.Equal("10001", jiraClient.CapturedTransitionRequest!.JiraIssueId);
        Assert.Null(jiraClient.CapturedTransitionRequest.JiraIssueKey);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenIssueIdAndIssueKeyAreMissing_ShouldThrowValidationError()
    {
        var service = CreateService(new FakeProductConfigService(), new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
            {
                ProductCode = "CRM",
                StandardStatus = "IN_PROGRESS"
            }));

        Assert.Equal(ErrorCodes.ValidationError, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenStandardStatusIsUnknown_ShouldThrowValidationError()
    {
        var service = CreateService(new FakeProductConfigService(), new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
            {
                ProductCode = "CRM",
                JiraIssueKey = "CRM-123",
                StandardStatus = "UNKNOWN"
            }));

        Assert.Equal(ErrorCodes.ValidationError, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenTransitionMappingIsMissing_ShouldThrowConfigNotFound()
    {
        var configService = new FakeProductConfigService
        {
            StatusTransition = new StatusTransitionConfig(
                Id: 1,
                ProductId: 1,
                IssueTypeMappingId: 1,
                StandardStatus: "OPEN",
                JiraStatusName: "To Do",
                JiraTransitionId: null,
                JiraTransitionName: null)
        };
        var service = CreateService(configService, new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<ConfigNotFoundException>(
            () => service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
            {
                ProductCode = "CRM",
                JiraIssueKey = "CRM-123",
                IssueTypeCode = "BUG",
                StandardStatus = "OPEN"
            }));

        Assert.Equal(ErrorCodes.ConfigNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenTransitionIdIsUnavailable_ShouldFallbackToTransitionName()
    {
        var configService = new FakeProductConfigService
        {
            StatusTransition = new StatusTransitionConfig(
                Id: 1,
                ProductId: 1,
                IssueTypeMappingId: 1,
                StandardStatus: "IN_PROGRESS",
                JiraStatusName: "In Progress",
                JiraTransitionId: "31",
                JiraTransitionName: "Start Progress")
        };
        var jiraClient = new FakeJiraClient
        {
            AvailableTransitions =
            [
                new JiraTransitionResponse("32", "Start Progress")
            ]
        };
        var service = CreateService(configService, jiraClient);

        await service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueKey = "CRM-123",
            IssueTypeCode = "BUG",
            StandardStatus = "IN_PROGRESS"
        });

        Assert.Equal("32", jiraClient.CapturedTransitionRequest!.TransitionId);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_WhenStatusAfterTransitionDoesNotMatchConfig_ShouldThrowJiraError()
    {
        var jiraClient = new FakeJiraClient
        {
            AvailableTransitions =
            [
                new JiraTransitionResponse("31", "Start Progress")
            ],
            JiraStatusName = "Done"
        };
        var service = CreateService(new FakeProductConfigService(), jiraClient);

        var exception = await Assert.ThrowsAsync<JiraClientException>(
            () => service.UpdateIssueStatusAsync(new UpdateIssueStatusRequest
            {
                ProductCode = "CRM",
                JiraIssueKey = "CRM-123",
                IssueTypeCode = "BUG",
                StandardStatus = "IN_PROGRESS"
            }));

        Assert.Equal(ErrorCodes.JiraError, exception.ErrorCode);
        Assert.Contains("expected 'In Progress'", exception.Message);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenRequestUsesIssueKey_ShouldReturnMappedStandardStatus()
    {
        var configService = new FakeProductConfigService
        {
            MappedStandardStatus = "IN_PROGRESS"
        };
        var jiraClient = new FakeJiraClient
        {
            JiraStatusName = "In Progress"
        };
        var service = CreateService(configService, jiraClient);

        var result = await service.GetIssueStatusAsync(new GetIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueKey = "CRM-123"
        });

        Assert.Equal("IN_PROGRESS", result.StandardStatus);
        Assert.Null(jiraClient.CapturedGetStatusIssueId);
        Assert.Equal("CRM-123", jiraClient.CapturedGetStatusIssueKey);
        Assert.Equal("In Progress", configService.CapturedJiraStatusName);
        Assert.Null(configService.CapturedMapIssueTypeCode);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenRequestUsesIssueId_ShouldReturnMappedStandardStatus()
    {
        var jiraClient = new FakeJiraClient
        {
            JiraStatusName = "Done"
        };
        var service = CreateService(new FakeProductConfigService { MappedStandardStatus = "DONE" }, jiraClient);

        var result = await service.GetIssueStatusAsync(new GetIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueId = "10001"
        });

        Assert.Equal("DONE", result.StandardStatus);
        Assert.Equal("10001", jiraClient.CapturedGetStatusIssueId);
        Assert.Null(jiraClient.CapturedGetStatusIssueKey);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenIssueTypeCodeIsProvided_ShouldUseItForStatusMapping()
    {
        var configService = new FakeProductConfigService
        {
            MappedStandardStatus = "WAITING"
        };
        var service = CreateService(
            configService,
            new FakeJiraClient { JiraStatusName = "Waiting" });

        var result = await service.GetIssueStatusAsync(new GetIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueKey = "CRM-123",
            IssueTypeCode = " bug "
        });

        Assert.Equal("WAITING", result.StandardStatus);
        Assert.Equal("bug", configService.CapturedMapIssueTypeCode);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenBothIssueIdAndIssueKeyAreProvided_ShouldUseIssueIdForJiraCall()
    {
        var jiraClient = new FakeJiraClient
        {
            JiraStatusName = "In Progress"
        };
        var service = CreateService(new FakeProductConfigService(), jiraClient);

        await service.GetIssueStatusAsync(new GetIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueId = "10001",
            JiraIssueKey = "CRM-123"
        });

        Assert.Equal("10001", jiraClient.CapturedGetStatusIssueId);
        Assert.Null(jiraClient.CapturedGetStatusIssueKey);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenIssueIdAndIssueKeyAreMissing_ShouldThrowValidationError()
    {
        var service = CreateService(new FakeProductConfigService(), new FakeJiraClient());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetIssueStatusAsync(new GetIssueStatusRequest
            {
                ProductCode = "CRM"
            }));

        Assert.Equal(ErrorCodes.ValidationError, exception.ErrorCode);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenJiraStatusDoesNotMap_ShouldReturnUnknown()
    {
        var service = CreateService(
            new FakeProductConfigService { MappedStandardStatus = "UNKNOWN" },
            new FakeJiraClient { JiraStatusName = "Not A Mapped Status" });

        var result = await service.GetIssueStatusAsync(new GetIssueStatusRequest
        {
            ProductCode = "CRM",
            JiraIssueKey = "CRM-123"
        });

        Assert.Equal("UNKNOWN", result.StandardStatus);
    }

    private static IssueService CreateService(
        IProductConfigService configService,
        IJiraClient jiraClient)
    {
        return new IssueService(
            configService,
            new JiraIssuePayloadBuilder(
                new SourcePathResolver(),
                new JiraFieldValueBuilder()),
            new FakeJiraClientResolver(jiraClient),
            NullLogger<IssueService>.Instance);
    }

    private static JsonElement Data<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    private sealed class FakeProductConfigService : IProductConfigService
    {
        public bool ThrowProductConfigNotFound { get; init; }

        public IReadOnlyList<FieldMappingConfig> FieldMappings { get; init; } = [];

        public string MappedStandardStatus { get; init; } = "IN_PROGRESS";

        public string? CapturedMapIssueTypeCode { get; private set; }

        public string? CapturedTemplateCode { get; private set; }

        public string? CapturedJiraStatusName { get; private set; }

        public StatusTransitionConfig StatusTransition { get; init; } = new(
            Id: 1,
            ProductId: 1,
            IssueTypeMappingId: 1,
            StandardStatus: "IN_PROGRESS",
            JiraStatusName: "In Progress",
            JiraTransitionId: "31",
            JiraTransitionName: "Start Progress");

        public Task<ProductConfig> GetProductAsync(
            string productCode,
            CancellationToken cancellationToken = default)
        {
            if (ThrowProductConfigNotFound)
            {
                throw new ConfigNotFoundException("Product config was not found.");
            }

            return Task.FromResult(new ProductConfig(
                Id: 1,
                Code: "CRM",
                Name: "CRM",
                JiraProjectKey: "CRM",
                JiraBaseUrl: "https://jira.example.com",
                JiraApiBasePath: "/rest/api/2",
                JiraVersion: "ServerV2"));
        }

        public Task<JiraCredentialConfig> GetJiraCredentialAsync(
            string productCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new JiraCredentialConfig(
                Id: 1,
                ProductId: 1,
                Username: "jira-user",
                Password: "jira-pass"));
        }

        public Task<IssueTypeConfig> GetIssueTypeAsync(
            string productCode,
            string issueTypeCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IssueTypeConfig(
                Id: 1,
                ProductId: 1,
                IssueTypeCode: "BUG",
                JiraIssueTypeName: "Bug",
                JiraIssueTypeId: "10001"));
        }

        public Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsAsync(
            string productCode,
            string? issueTypeCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FieldMappings);
        }

        public Task<IReadOnlyList<FieldMappingConfig>> GetFieldMappingsByTemplateAsync(
            string productCode,
            string issueTypeCode,
            string? templateCode,
            CancellationToken cancellationToken = default)
        {
            CapturedTemplateCode = templateCode;
            return Task.FromResult(FieldMappings);
        }

        public Task<StatusTransitionConfig> GetStatusTransitionAsync(
            string productCode,
            string? issueTypeCode,
            string standardStatus,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StatusTransition);
        }

        public Task<string> MapJiraStatusToStandardStatusAsync(
            string productCode,
            string? issueTypeCode,
            string jiraStatusName,
            CancellationToken cancellationToken = default)
        {
            CapturedMapIssueTypeCode = issueTypeCode;
            CapturedJiraStatusName = jiraStatusName;

            return Task.FromResult(MappedStandardStatus);
        }
    }

    private sealed class FakeJiraClient : IJiraClient
    {
        public JiraConnectionConfig? CapturedConnection { get; private set; }

        public CreateJiraIssueRequest? CapturedCreateRequest { get; private set; }

        public IReadOnlyList<JiraTransitionResponse> AvailableTransitions { get; init; } = [];

        public string JiraStatusName { get; init; } = "In Progress";

        public string? CapturedGetStatusIssueId { get; private set; }

        public string? CapturedGetStatusIssueKey { get; private set; }

        public string? CapturedGetTransitionsIssueId { get; private set; }

        public string? CapturedGetTransitionsIssueKey { get; private set; }

        public TransitionJiraIssueRequest? CapturedTransitionRequest { get; private set; }

        public Task<CreateJiraIssueResponse> CreateIssueAsync(
            JiraConnectionConfig connection,
            CreateJiraIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedCreateRequest = request;

            return Task.FromResult(new CreateJiraIssueResponse(
                Id: "10001",
                Key: "CRM-123"));
        }

        public Task<JiraIssueStatusResponse> GetIssueStatusAsync(
            JiraConnectionConfig connection,
            string? jiraIssueId,
            string? jiraIssueKey,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedGetStatusIssueId = jiraIssueId;
            CapturedGetStatusIssueKey = jiraIssueKey;

            return Task.FromResult(new JiraIssueStatusResponse(JiraStatusName));
        }

        public Task<IReadOnlyList<JiraTransitionResponse>> GetTransitionsAsync(
            JiraConnectionConfig connection,
            string? jiraIssueId,
            string? jiraIssueKey,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedGetTransitionsIssueId = jiraIssueId;
            CapturedGetTransitionsIssueKey = jiraIssueKey;

            return Task.FromResult(AvailableTransitions);
        }

        public Task<IReadOnlyList<JiraIssueTypeResponse>> GetIssueTypesAsync(
            JiraConnectionConfig connection,
            string projectKey,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;

            return Task.FromResult<IReadOnlyList<JiraIssueTypeResponse>>([]);
        }

        public Task<IReadOnlyList<JiraIssueFieldMetadataResponse>> GetIssueTypeFieldsAsync(
            JiraConnectionConfig connection,
            string projectKey,
            string issueTypeId,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;

            return Task.FromResult<IReadOnlyList<JiraIssueFieldMetadataResponse>>([]);
        }

        public Task TransitionIssueAsync(
            JiraConnectionConfig connection,
            TransitionJiraIssueRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedConnection = connection;
            CapturedTransitionRequest = request;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeJiraClientResolver : IJiraClientResolver
    {
        private readonly IJiraClient _jiraClient;

        public FakeJiraClientResolver(IJiraClient jiraClient)
        {
            _jiraClient = jiraClient;
        }

        public IJiraClient Resolve(string jiraVersion)
        {
            return _jiraClient;
        }
    }
}
