using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraFieldMetadataCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JiraIssueTypeFieldMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTypeMappingId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Required = table.Column<bool>(type: "INTEGER", nullable: false),
                    SchemaType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SchemaItems = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SchemaSystem = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SchemaCustom = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SchemaCustomId = table.Column<int>(type: "INTEGER", nullable: true),
                    HasDefaultValue = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultValueJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AutoCompleteUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OperationsJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AllowedValuesJson = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    RecommendedValueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecommendedValueShape = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraIssueTypeFieldMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraIssueTypeFieldMetadata_IssueTypeMappings_IssueTypeMappingId",
                        column: x => x.IssueTypeMappingId,
                        principalTable: "IssueTypeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JiraIssueTypeFieldMetadata_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JiraIssueTypeFieldMetadata_IssueTypeMappingId",
                table: "JiraIssueTypeFieldMetadata",
                column: "IssueTypeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_JiraIssueTypeFieldMetadata_ProductId_IssueTypeMappingId_FieldId",
                table: "JiraIssueTypeFieldMetadata",
                columns: new[] { "ProductId", "IssueTypeMappingId", "FieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JiraIssueTypeFieldMetadata");
        }
    }
}
