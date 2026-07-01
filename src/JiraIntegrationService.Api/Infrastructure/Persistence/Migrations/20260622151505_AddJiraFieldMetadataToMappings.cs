using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraFieldMetadataToMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JiraAllowedValuesJson",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraAutoCompleteUrl",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraDefaultValueJson",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraFieldDescription",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraFieldName",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraSchemaCustom",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraSchemaItems",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraSchemaSystem",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraSchemaType",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType" },
                values: new object[] { null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType" },
                values: new object[] { null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType" },
                values: new object[] { null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType" },
                values: new object[] { null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "IssueFieldMappings",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "JiraAllowedValuesJson", "JiraAutoCompleteUrl", "JiraDefaultValueJson", "JiraFieldDescription", "JiraFieldName", "JiraSchemaCustom", "JiraSchemaItems", "JiraSchemaSystem", "JiraSchemaType" },
                values: new object[] { null, null, null, null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JiraAllowedValuesJson",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraAutoCompleteUrl",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraDefaultValueJson",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraFieldDescription",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraFieldName",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraSchemaCustom",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraSchemaItems",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraSchemaSystem",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "JiraSchemaType",
                table: "IssueFieldMappings");
        }
    }
}
