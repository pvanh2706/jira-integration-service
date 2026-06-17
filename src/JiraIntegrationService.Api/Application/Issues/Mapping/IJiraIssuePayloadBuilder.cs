using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Application.Jira.Models;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public interface IJiraIssuePayloadBuilder
{
    CreateJiraIssueRequest BuildCreateIssueRequest(
        ProductConfig product,
        IssueTypeConfig issueType,
        IReadOnlyList<FieldMappingConfig> fieldMappings,
        JsonElement data);
}
