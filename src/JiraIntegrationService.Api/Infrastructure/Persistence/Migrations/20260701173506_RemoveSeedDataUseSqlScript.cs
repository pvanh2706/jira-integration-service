using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedDataUseSqlScript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IssueFieldMappingTemplates",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappingTemplates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "JiraCredentials",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StatusMappings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StatusMappings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StatusMappings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StatusMappings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "StatusMappings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "IssueTypeMappings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "IssueTypeMappings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "JiraApiBasePath", "JiraBaseUrl", "JiraProjectKey", "JiraVersion", "Name", "UpdatedAt" },
                values: new object[] { 1, "EAS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "/rest/api/2", "https://jira.ezcloudhotel.com", "EAS", "ServerV2", "EAS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "IssueTypeMappings",
                columns: new[] { "Id", "CreatedAt", "IsActive", "IssueTypeCode", "JiraIssueTypeId", "JiraIssueTypeName", "ProductId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "BUG", null, "Bug", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TASK", null, "Task", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "JiraCredentials",
                columns: new[] { "Id", "AuthType", "CreatedAt", "IsActive", "PasswordOrToken", "ProductId", "UpdatedAt", "Username" },
                values: new object[] { 1, "Basic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "123456Aa@", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "anh.phamviet" });

            migrationBuilder.InsertData(
                table: "IssueFieldMappingTemplates",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "IsDefault", "IssueTypeMappingId", "Name", "ProductId", "TemplateCode", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Default field mapping template.", true, true, 1, "Default", 1, "DEFAULT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Default field mapping template.", true, true, 2, "Default", 1, "DEFAULT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "IssueFieldMappings",
                columns: new[] { "Id", "CreatedAt", "DefaultValue", "IsActive", "IsRequired", "IssueTypeMappingId", "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraField", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType", "ProductId", "SortOrder", "SourcePath", "TemplateCode", "TransformConfigJson", "UpdatedAt", "ValueShape", "ValueType" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, 1, null, null, null, "summary", null, null, null, null, null, null, 1, 10, "data.summary", "DEFAULT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, null, null, null, "description", null, null, null, null, null, null, 1, 20, "data.description", "DEFAULT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, null, null, null, "priority", null, null, null, null, null, null, 1, 30, "data.priority", "DEFAULT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "name", "string" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, null, null, null, "customfield_10010", null, null, null, null, null, null, 1, 40, "data.customer.code", "DEFAULT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, null, null, null, "customfield_10011", null, null, null, null, null, null, 1, 50, "data.ticket.url", "DEFAULT", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" }
                });

            migrationBuilder.InsertData(
                table: "StatusMappings",
                columns: new[] { "Id", "IsActive", "IssueTypeMappingId", "JiraStatusName", "JiraTransitionId", "JiraTransitionName", "ProductId", "StandardStatus" },
                values: new object[,]
                {
                    { 1, true, 1, "To Do", null, null, 1, "OPEN" },
                    { 2, true, 1, "In Progress", "31", "Start Progress", 1, "IN_PROGRESS" },
                    { 3, true, 1, "Waiting", "41", "Waiting", 1, "WAITING" },
                    { 4, true, 1, "Done", "51", "Done", 1, "DONE" },
                    { 5, true, 1, "Cancelled", "61", "Cancel", 1, "CANCELLED" }
                });
        }
    }
}
