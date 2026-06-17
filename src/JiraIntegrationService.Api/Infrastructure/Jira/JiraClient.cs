using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Application.Jira.Models;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Options;
using Microsoft.Extensions.Options;

namespace JiraIntegrationService.Api.Infrastructure.Jira;

public sealed class JiraClient : IJiraClient
{
    private const int MaxLoggedBodyLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<JiraClient> _logger;

    public JiraClient(
        HttpClient httpClient,
        IOptions<RetryOptions> retryOptions,
        ILogger<JiraClient> logger)
    {
        _httpClient = httpClient;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    public async Task<CreateJiraIssueResponse> CreateIssueAsync(
        JiraConnectionConfig connection,
        CreateJiraIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var payload = BuildCreateIssuePayload(request);
        var jiraResponse = await SendAsync<CreateIssueJiraResponse>(
            HttpMethod.Post,
            "issue",
            connection,
            payload,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(jiraResponse.Id) || string.IsNullOrWhiteSpace(jiraResponse.Key))
        {
            throw new JiraClientException("Jira create issue response does not contain issue id and key.");
        }

        return new CreateJiraIssueResponse(jiraResponse.Id, jiraResponse.Key);
    }

    public async Task<JiraIssueStatusResponse> GetIssueStatusAsync(
        JiraConnectionConfig connection,
        string? jiraIssueId,
        string? jiraIssueKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var issueIdOrKey = BuildIssueIdOrKey(jiraIssueId, jiraIssueKey);
        var jiraResponse = await SendAsync<GetIssueJiraResponse>(
            HttpMethod.Get,
            $"issue/{Uri.EscapeDataString(issueIdOrKey)}?fields=status",
            connection,
            body: null,
            cancellationToken);

        var statusName = jiraResponse.Fields?.Status?.Name;
        if (string.IsNullOrWhiteSpace(statusName))
        {
            throw new JiraClientException("Jira issue status response does not contain status name.");
        }

        return new JiraIssueStatusResponse(statusName);
    }

    public async Task<IReadOnlyList<JiraTransitionResponse>> GetTransitionsAsync(
        JiraConnectionConfig connection,
        string? jiraIssueId,
        string? jiraIssueKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var issueIdOrKey = BuildIssueIdOrKey(jiraIssueId, jiraIssueKey);
        var jiraResponse = await SendAsync<GetTransitionsJiraResponse>(
            HttpMethod.Get,
            $"issue/{Uri.EscapeDataString(issueIdOrKey)}/transitions",
            connection,
            body: null,
            cancellationToken);

        return jiraResponse.Transitions
            ?.Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new JiraTransitionResponse(item.Id!, item.Name!))
            .ToArray()
            ?? [];
    }

    public Task TransitionIssueAsync(
        JiraConnectionConfig connection,
        TransitionJiraIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var issueIdOrKey = BuildIssueIdOrKey(request.JiraIssueId, request.JiraIssueKey);
        if (string.IsNullOrWhiteSpace(request.TransitionId))
        {
            throw new JiraClientException("Jira transition id is required.");
        }

        var payload = new
        {
            transition = new
            {
                id = request.TransitionId.Trim()
            }
        };

        return SendAsync<EmptyJiraResponse>(
            HttpMethod.Post,
            $"issue/{Uri.EscapeDataString(issueIdOrKey)}/transitions",
            connection,
            payload,
            cancellationToken);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        string relativePath,
        JiraConnectionConfig connection,
        object? body,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(connection, relativePath);
        var requestUri = BuildRequestUri(connection, endpoint);
        var requestBody = body is null ? null : JsonSerializer.Serialize(body, JsonOptions);
        var sanitizedRequestBody = SanitizeBody(requestBody);
        var maxAttempts = Math.Max(1, _retryOptions.MaxAttempts);
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, _retryOptions.DelayMilliseconds));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = CreateRequest(method, requestUri, connection, requestBody);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var responseBody = await ReadResponseBodyAsync(response, cancellationToken);
                stopwatch.Stop();

                LogJiraResponse(
                    method,
                    endpoint,
                    attempt,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    sanitizedRequestBody,
                    responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return DeserializeResponse<TResponse>(responseBody);
                }

                if (ShouldRetryStatus(response.StatusCode) && attempt < maxAttempts)
                {
                    await DelayBeforeRetryAsync(delay, cancellationToken);
                    continue;
                }

                throw BuildJiraError(response.StatusCode, responseBody);
            }
            catch (Exception exception) when (IsTemporaryException(exception, cancellationToken) && attempt < maxAttempts)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    exception,
                    "Temporary Jira request failure. ErrorCode: {ErrorCode}. Method: {Method}. Endpoint: {Endpoint}. Attempt: {Attempt}. DurationMs: {DurationMs}",
                    ErrorCodes.JiraError,
                    method.Method,
                    endpoint,
                    attempt,
                    stopwatch.ElapsedMilliseconds);

                await DelayBeforeRetryAsync(delay, cancellationToken);
            }
            catch (Exception exception) when (IsTemporaryException(exception, cancellationToken))
            {
                _logger.LogWarning(
                    exception,
                    "Jira request failed after retry attempts. ErrorCode: {ErrorCode}. Method: {Method}. Endpoint: {Endpoint}. Attempts: {Attempts}",
                    ErrorCodes.JiraError,
                    method.Method,
                    endpoint,
                    maxAttempts);

                throw new JiraClientException("Jira is temporarily unavailable.");
            }
        }

        throw new JiraClientException("Jira request failed.");
    }

    private static object BuildCreateIssuePayload(CreateJiraIssueRequest request)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["project"] = new { key = request.ProjectKey },
            ["issuetype"] = BuildIssueTypeField(request),
            ["summary"] = request.Summary
        };

        AddOptionalString(fields, "description", request.Description);
        AddOptionalNamedValue(fields, "priority", request.PriorityName);
        AddOptionalNamedValue(fields, "reporter", request.ReporterName);
        AddOptionalNamedValue(fields, "assignee", request.AssigneeName);

        if (!string.IsNullOrWhiteSpace(request.ParentKey))
        {
            fields["parent"] = new
            {
                key = request.ParentKey.Trim()
            };
        }

        if (request.ComponentIds is { Count: > 0 })
        {
            var components = request.ComponentIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => new
                {
                    id = id.Trim()
                })
                .ToArray();

            if (components.Length > 0)
            {
                fields["components"] = components;
            }
        }

        if (request.CustomFields is not null)
        {
            foreach (var field in request.CustomFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Key))
                {
                    fields[field.Key] = field.Value;
                }
            }
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["fields"] = fields
        };

        if (request.Worklogs is { Count: > 0 })
        {
            payload["update"] = new
            {
                worklog = request.Worklogs.Select(worklog => new
                {
                    add = new
                    {
                        started = NormalizeRequiredJiraValue(worklog.Started, "worklog.started"),
                        timeSpent = NormalizeRequiredJiraValue(worklog.TimeSpent, "worklog.timeSpent"),
                        comment = string.IsNullOrWhiteSpace(worklog.Comment)
                            ? null
                            : worklog.Comment.Trim()
                    }
                }).ToArray()
            };
        }

        return payload;
    }

    private static object BuildIssueTypeField(CreateJiraIssueRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IssueTypeId))
        {
            return new
            {
                id = request.IssueTypeId.Trim()
            };
        }

        if (!string.IsNullOrWhiteSpace(request.IssueTypeName))
        {
            return new
            {
                name = request.IssueTypeName.Trim()
            };
        }

        throw new JiraClientException("Jira issue type id or name is required.");
    }

    private static string NormalizeRequiredJiraValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JiraClientException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static void AddOptionalString(
        IDictionary<string, object?> fields,
        string fieldName,
        string? fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue))
        {
            fields[fieldName] = fieldValue.Trim();
        }
    }

    private static void AddOptionalNamedValue(
        IDictionary<string, object?> fields,
        string fieldName,
        string? fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue))
        {
            fields[fieldName] = new
            {
                name = fieldValue.Trim()
            };
        }
    }

    private static string BuildIssueIdOrKey(string? jiraIssueId, string? jiraIssueKey)
    {
        if (!string.IsNullOrWhiteSpace(jiraIssueId))
        {
            return jiraIssueId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(jiraIssueKey))
        {
            return jiraIssueKey.Trim();
        }

        throw new JiraClientException("Jira issue id or key is required.");
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        Uri requestUri,
        JiraConnectionConfig connection,
        string? requestBody)
    {
        if (!string.Equals(connection.AuthType, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            throw new JiraClientException($"Jira auth type '{connection.AuthType}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(connection.Username) || string.IsNullOrWhiteSpace(connection.PasswordOrToken))
        {
            throw new JiraClientException("Jira credential is not configured.");
        }

        var request = new HttpRequestMessage(method, requestUri);
        var authBytes = Encoding.UTF8.GetBytes($"{connection.Username}:{connection.PasswordOrToken}");
        var authValue = Convert.ToBase64String(authBytes);

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (requestBody is not null)
        {
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string BuildEndpoint(JiraConnectionConfig connection, string relativePath)
    {
        var apiBasePath = string.IsNullOrWhiteSpace(connection.ApiBasePath)
            ? string.Empty
            : connection.ApiBasePath.Trim('/');
        var path = relativePath.TrimStart('/');

        return string.IsNullOrEmpty(apiBasePath)
            ? path
            : $"{apiBasePath}/{path}";
    }

    private static Uri BuildRequestUri(JiraConnectionConfig connection, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(connection.BaseUrl))
        {
            throw new JiraClientException("Jira base URL is not configured.");
        }

        var baseUrl = connection.BaseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), endpoint);
    }

    private static async Task<string> ReadResponseBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return string.Empty;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static TResponse DeserializeResponse<TResponse>(string responseBody)
    {
        if (typeof(TResponse) == typeof(EmptyJiraResponse))
        {
            return (TResponse)(object)new EmptyJiraResponse();
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new JiraClientException("Jira returned an empty response.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions);
            return result ?? throw new JiraClientException("Jira returned an empty response.");
        }
        catch (JsonException)
        {
            throw new JiraClientException("Jira returned an invalid JSON response.");
        }
    }

    private static bool ShouldRetryStatus(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static bool IsTemporaryException(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is HttpRequestException)
        {
            return true;
        }

        return exception is TaskCanceledException && !cancellationToken.IsCancellationRequested;
    }

    private static Task DelayBeforeRetryAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return delay <= TimeSpan.Zero
            ? Task.CompletedTask
            : Task.Delay(delay, cancellationToken);
    }

    private static JiraClientException BuildJiraError(HttpStatusCode statusCode, string responseBody)
    {
        var message = ExtractJiraErrorMessage(statusCode, responseBody);
        return new JiraClientException(message);
    }

    private static string ExtractJiraErrorMessage(HttpStatusCode statusCode, string responseBody)
    {
        var fallback = $"Jira returned HTTP {(int)statusCode}.";
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var messages = new List<string>();

            if (document.RootElement.TryGetProperty("errorMessages", out var errorMessages)
                && errorMessages.ValueKind == JsonValueKind.Array)
            {
                messages.AddRange(errorMessages
                    .EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))!);
            }

            if (document.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                messages.AddRange(errors
                    .EnumerateObject()
                    .Select(item => item.Value.ValueKind == JsonValueKind.String
                        ? item.Value.GetString()
                        : item.Value.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))!);
            }

            if (messages.Count > 0)
            {
                return Truncate($"Jira error: {string.Join("; ", messages)}", MaxLoggedBodyLength);
            }
        }
        catch (JsonException)
        {
            return Truncate($"{fallback} {responseBody}", MaxLoggedBodyLength);
        }

        return Truncate($"{fallback} {responseBody}", MaxLoggedBodyLength);
    }

    private void LogJiraResponse(
        HttpMethod method,
        string endpoint,
        int attempt,
        HttpStatusCode statusCode,
        long durationMilliseconds,
        string? requestBody,
        string responseBody)
    {
        var sanitizedResponseBody = SanitizeBody(responseBody);
        var logLevel = (int)statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(
            logLevel,
            "Jira request completed. ErrorCode: {ErrorCode}. Method: {Method}. Endpoint: {Endpoint}. StatusCode: {StatusCode}. DurationMs: {DurationMs}. Attempt: {Attempt}. RequestBody: {RequestBody}. ResponseBody: {ResponseBody}",
            (int)statusCode >= 400 ? ErrorCodes.JiraError : null,
            method.Method,
            endpoint,
            (int)statusCode,
            durationMilliseconds,
            attempt,
            requestBody,
            sanitizedResponseBody);
    }

    private static string? SanitizeBody(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return rawBody;
        }

        try
        {
            var node = JsonNode.Parse(rawBody);
            SanitizeNode(node);
            return Truncate(node?.ToJsonString(JsonOptions) ?? rawBody, MaxLoggedBodyLength);
        }
        catch (JsonException)
        {
            return Truncate(rawBody, MaxLoggedBodyLength);
        }
    }

    private static void SanitizeNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToArray())
                {
                    if (IsSensitiveKey(property.Key))
                    {
                        jsonObject[property.Key] = "***";
                    }
                    else
                    {
                        SanitizeNode(property.Value);
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    SanitizeNode(item);
                }

                break;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        return key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("authorization", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("secret", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private sealed record CreateIssueJiraResponse(string? Id, string? Key);

    private sealed record GetIssueJiraResponse(GetIssueFieldsJiraResponse? Fields);

    private sealed record GetIssueFieldsJiraResponse(GetIssueStatusJiraResponse? Status);

    private sealed record GetIssueStatusJiraResponse(string? Name);

    private sealed record GetTransitionsJiraResponse(IReadOnlyList<GetTransitionJiraResponse>? Transitions);

    private sealed record GetTransitionJiraResponse(string? Id, string? Name);

    private sealed record EmptyJiraResponse;
}
