using System.Net;
using System.Text;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Jira;
using JiraIntegrationService.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JiraIntegrationService.Tests.Infrastructure.Jira;

public sealed class JiraClientTests
{
    private static readonly JiraConnectionConfig Connection = new(
        BaseUrl: "https://jira.example.com",
        ApiBasePath: "/rest/api/2",
        Version: "ServerV2",
        AuthType: "Basic",
        Username: "jira-user",
        PasswordOrToken: "jira-pass");

    [Fact]
    public async Task CreateIssueAsync_WhenJiraReturnsIssue_ShouldParseIdAndKey()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.Created, """
            {
              "id": "10001",
              "key": "CRM-12"
            }
            """);
        var client = CreateClient(handler);

        var response = await client.CreateIssueAsync(
            Connection,
            new CreateJiraIssueRequest(
                ProjectKey: "CRM",
                IssueTypeName: "Bug",
                Summary: "Customer cannot submit form",
                Description: "Submit button returns 500.",
                PriorityName: "High",
                ReporterName: "reporter-user",
                AssigneeName: "assignee-user",
                CustomFields: new Dictionary<string, object?>
                {
                    ["customfield_10010"] = "CUST-1"
                },
                IssueTypeId: "6",
                ParentKey: "CRM-1",
                ComponentIds: ["15690"],
                Worklogs:
                [
                    new JiraWorklogEntry(
                        Started: "2024-11-25T15:05:00.000+0000",
                        TimeSpent: "4.7h",
                        Comment: "Initial work")
                ]));

        Assert.Equal("10001", response.Id);
        Assert.Equal("CRM-12", response.Key);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/rest/api/2/issue", request.Uri.AbsolutePath);
        Assert.Equal("Basic", request.AuthorizationScheme);

        using var body = JsonDocument.Parse(request.Body!);
        var fields = body.RootElement.GetProperty("fields");
        Assert.Equal("CRM", fields.GetProperty("project").GetProperty("key").GetString());
        Assert.Equal("6", fields.GetProperty("issuetype").GetProperty("id").GetString());
        Assert.Equal("Customer cannot submit form", fields.GetProperty("summary").GetString());
        Assert.Equal("High", fields.GetProperty("priority").GetProperty("name").GetString());
        Assert.Equal("reporter-user", fields.GetProperty("reporter").GetProperty("name").GetString());
        Assert.Equal("assignee-user", fields.GetProperty("assignee").GetProperty("name").GetString());
        Assert.Equal("CRM-1", fields.GetProperty("parent").GetProperty("key").GetString());
        Assert.Equal("15690", fields.GetProperty("components")[0].GetProperty("id").GetString());
        Assert.Equal("CUST-1", fields.GetProperty("customfield_10010").GetString());

        var worklogAdd = body.RootElement
            .GetProperty("update")
            .GetProperty("worklog")[0]
            .GetProperty("add");
        Assert.Equal("2024-11-25T15:05:00.000+0000", worklogAdd.GetProperty("started").GetString());
        Assert.Equal("4.7h", worklogAdd.GetProperty("timeSpent").GetString());
        Assert.Equal("Initial work", worklogAdd.GetProperty("comment").GetString());
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenJiraReturnsStatus_ShouldParseStatusName()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """
            {
              "fields": {
                "status": {
                  "name": "In Progress"
                }
              }
            }
            """);
        var client = CreateClient(handler);

        var response = await client.GetIssueStatusAsync(Connection, jiraIssueId: null, jiraIssueKey: "CRM-12");

        Assert.Equal("In Progress", response.StatusName);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/rest/api/2/issue/CRM-12", request.Uri.AbsolutePath);
        Assert.Equal("?fields=status", request.Uri.Query);
    }

    [Fact]
    public async Task GetTransitionsAsync_WhenJiraReturnsTransitions_ShouldParseValidTransitions()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """
            {
              "transitions": [
                {
                  "id": "31",
                  "name": "Start Progress"
                },
                {
                  "id": "",
                  "name": "Ignored"
                }
              ]
            }
            """);
        var client = CreateClient(handler);

        var transitions = await client.GetTransitionsAsync(Connection, jiraIssueId: null, jiraIssueKey: "CRM-12");

        var transition = Assert.Single(transitions);
        Assert.Equal("31", transition.Id);
        Assert.Equal("Start Progress", transition.Name);
    }

    [Fact]
    public async Task TransitionIssueAsync_WhenBothIdAndKeyProvided_ShouldPreferIssueIdAndCallTransitionsEndpoint()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.NoContent);
        var client = CreateClient(handler);

        await client.TransitionIssueAsync(
            Connection,
            new TransitionJiraIssueRequest(
                JiraIssueId: "10001",
                JiraIssueKey: "CRM-12",
                TransitionId: "31"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/rest/api/2/issue/10001/transitions", request.Uri.AbsolutePath);

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("31", body.RootElement.GetProperty("transition").GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenTemporaryErrorThenSuccess_ShouldRetry()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, """
            {
              "errorMessages": ["Temporary unavailable"]
            }
            """);
        handler.QueueResponse(HttpStatusCode.OK, """
            {
              "fields": {
                "status": {
                  "name": "Done"
                }
              }
            }
            """);
        var client = CreateClient(handler);

        var response = await client.GetIssueStatusAsync(Connection, jiraIssueId: null, jiraIssueKey: "CRM-12");

        Assert.Equal("Done", response.StatusName);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetIssueStatusAsync_WhenJiraReturnsBadRequest_ShouldThrowJiraErrorWithoutRetry()
    {
        var handler = new FakeHttpMessageHandler();
        var logger = new CapturingLogger<JiraClient>();
        handler.QueueResponse(HttpStatusCode.BadRequest, """
            {
              "errorMessages": ["Invalid issue key"],
              "errors": {
                "issue": "Issue does not exist"
              }
            }
            """);
        var client = CreateClient(handler, logger);

        var exception = await Assert.ThrowsAsync<JiraClientException>(
            () => client.GetIssueStatusAsync(Connection, jiraIssueId: null, jiraIssueKey: "CRM-404"));

        Assert.Equal(ErrorCodes.JiraError, exception.ErrorCode);
        Assert.Contains("Invalid issue key", exception.Message);
        Assert.Single(handler.Requests);
        Assert.Contains(logger.Entries, entry =>
            entry.Properties.TryGetValue("ErrorCode", out var errorCode)
            && errorCode?.ToString() == ErrorCodes.JiraError);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenLoggingRequestAndResponse_ShouldSanitizeSensitiveValues()
    {
        var handler = new FakeHttpMessageHandler();
        var logger = new CapturingLogger<JiraClient>();
        handler.QueueResponse(HttpStatusCode.Created, """
            {
              "id": "10001",
              "key": "CRM-12",
              "password": "response-secret"
            }
            """);
        var client = CreateClient(handler, logger);

        await client.CreateIssueAsync(
            Connection,
            new CreateJiraIssueRequest(
                ProjectKey: "CRM",
                IssueTypeName: "Bug",
                Summary: "Customer cannot submit form",
                CustomFields: new Dictionary<string, object?>
                {
                    ["customfield_password"] = "custom-secret",
                    ["customfield_token"] = "token-secret",
                    ["customfield_10010"] = "visible-value"
                }));

        var logText = string.Join(
            " ",
            logger.Entries.Select(entry =>
                $"{entry.Message} {string.Join(" ", entry.Properties.Select(property => $"{property.Key}:{property.Value}"))}"));

        Assert.DoesNotContain("jira-pass", logText);
        Assert.DoesNotContain("custom-secret", logText);
        Assert.DoesNotContain("token-secret", logText);
        Assert.DoesNotContain("response-secret", logText);
        Assert.DoesNotContain("Basic", logText);
        Assert.Contains("***", logText);
        Assert.Contains("visible-value", logText);
    }

    private static JiraClient CreateClient(
        FakeHttpMessageHandler handler,
        ILogger<JiraClient>? logger = null)
    {
        var httpClient = new HttpClient(handler);

        return new JiraClient(
            httpClient,
            Options.Create(new RetryOptions
            {
                MaxAttempts = 3,
                DelayMilliseconds = 1
            }),
            logger ?? NullLogger<JiraClient>.Instance);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

        public List<CapturedRequest> Requests { get; } = [];

        public void QueueResponse(HttpStatusCode statusCode, string? content = null)
        {
            _responses.Enqueue(_ =>
            {
                var response = new HttpResponseMessage(statusCode);
                if (content is not null)
                {
                    response.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }

                return response;
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No fake HTTP response was queued.");
            }

            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri ?? throw new InvalidOperationException("Request URI was not set."),
                request.Headers.Authorization?.Scheme,
                body));

            return _responses.Dequeue().Invoke(request);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        string? AuthorizationScheme,
        string? Body);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<CapturedLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IEnumerable<KeyValuePair<string, object?>>;

            Entries.Add(new CapturedLogEntry(
                logLevel,
                formatter(state, exception),
                properties?.ToDictionary(
                    property => property.Key,
                    property => property.Value)
                ?? new Dictionary<string, object?>()));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record CapturedLogEntry(
        LogLevel LogLevel,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
