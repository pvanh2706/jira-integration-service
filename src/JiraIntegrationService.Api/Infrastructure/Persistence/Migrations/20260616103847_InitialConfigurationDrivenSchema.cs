using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialConfigurationDrivenSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JiraProjectKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    JiraBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    JiraApiBasePath = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JiraVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueTypeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTypeCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JiraIssueTypeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JiraIssueTypeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTypeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueTypeMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JiraCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PasswordOrToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraCredentials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueFieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTypeMappingId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourcePath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    JiraField = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ValueShape = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TransformConfigJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueFieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueFieldMappings_IssueTypeMappings_IssueTypeMappingId",
                        column: x => x.IssueTypeMappingId,
                        principalTable: "IssueTypeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueFieldMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatusMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTypeMappingId = table.Column<int>(type: "INTEGER", nullable: true),
                    StandardStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    JiraStatusName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JiraTransitionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JiraTransitionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusMappings_IssueTypeMappings_IssueTypeMappingId",
                        column: x => x.IssueTypeMappingId,
                        principalTable: "IssueTypeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "JiraApiBasePath", "JiraBaseUrl", "JiraProjectKey", "JiraVersion", "Name", "UpdatedAt" },
                values: new object[] { 1, "CRM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "/rest/api/2", "https://jira.example.com", "CRM", "ServerV2", "CRM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

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
                values: new object[] { 1, "Basic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "change-me", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jira-crm-user" });

            migrationBuilder.InsertData(
                table: "IssueFieldMappings",
                columns: new[] { "Id", "CreatedAt", "DefaultValue", "IsActive", "IsRequired", "IssueTypeMappingId", "JiraField", "ProductId", "SortOrder", "SourcePath", "TransformConfigJson", "UpdatedAt", "ValueShape", "ValueType" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, 1, "summary", 1, 10, "data.summary", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, "description", 1, 20, "data.description", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, "priority", 1, 30, "data.priority", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "name", "string" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, "customfield_10010", 1, 40, "data.customer.code", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, 1, "customfield_10011", 1, 50, "data.ticket.url", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "raw", "string" }
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

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappings_IssueTypeMappingId",
                table: "IssueFieldMappings",
                column: "IssueTypeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappings_ProductId_IssueTypeMappingId_SourcePath",
                table: "IssueFieldMappings",
                columns: new[] { "ProductId", "IssueTypeMappingId", "SourcePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueTypeMappings_ProductId_IssueTypeCode",
                table: "IssueTypeMappings",
                columns: new[] { "ProductId", "IssueTypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JiraCredentials_ProductId_IsActive",
                table: "JiraCredentials",
                columns: new[] { "ProductId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusMappings_IssueTypeMappingId",
                table: "StatusMappings",
                column: "IssueTypeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusMappings_ProductId_IssueTypeMappingId_StandardStatus_JiraStatusName",
                table: "StatusMappings",
                columns: new[] { "ProductId", "IssueTypeMappingId", "StandardStatus", "JiraStatusName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueFieldMappings");

            migrationBuilder.DropTable(
                name: "JiraCredentials");

            migrationBuilder.DropTable(
                name: "StatusMappings");

            migrationBuilder.DropTable(
                name: "IssueTypeMappings");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
