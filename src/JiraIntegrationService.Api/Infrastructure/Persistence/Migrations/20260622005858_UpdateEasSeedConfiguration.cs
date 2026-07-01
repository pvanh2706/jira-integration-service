using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEasSeedConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "JiraCredentials",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordOrToken", "Username" },
                values: new object[] { "123456Aa@", "anh.phamviet" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "JiraBaseUrl", "JiraProjectKey", "Name" },
                values: new object[] { "EAS", "https://jira.ezcloudhotel.com", "EAS", "EAS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "JiraCredentials",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordOrToken", "Username" },
                values: new object[] { "change-me", "jira-crm-user" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "JiraBaseUrl", "JiraProjectKey", "Name" },
                values: new object[] { "CRM", "https://jira.example.com", "CRM", "CRM" });
        }
    }
}
