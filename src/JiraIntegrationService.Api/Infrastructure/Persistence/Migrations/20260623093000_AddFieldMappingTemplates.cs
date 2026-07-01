using System;
using JiraIntegrationService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiraIntegrationService.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260623093000_AddFieldMappingTemplates")]
    public partial class AddFieldMappingTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssueFieldMappings_ProductId_IssueTypeMappingId_SourcePath",
                table: "IssueFieldMappings");

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "IssueFieldMappings",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.CreateTable(
                name: "IssueFieldMappingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTypeMappingId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueFieldMappingTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueFieldMappingTemplates_IssueTypeMappings_IssueTypeMappingId",
                        column: x => x.IssueTypeMappingId,
                        principalTable: "IssueTypeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueFieldMappingTemplates_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappings_ProductId_IssueTypeMappingId_TemplateCode_SourcePath",
                table: "IssueFieldMappings",
                columns: new[] { "ProductId", "IssueTypeMappingId", "TemplateCode", "SourcePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappingTemplates_IssueTypeMappingId",
                table: "IssueFieldMappingTemplates",
                column: "IssueTypeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappingTemplates_ProductId_IssueTypeMappingId_TemplateCode",
                table: "IssueFieldMappingTemplates",
                columns: new[] { "ProductId", "IssueTypeMappingId", "TemplateCode" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO IssueFieldMappingTemplates
                    (ProductId, IssueTypeMappingId, TemplateCode, Name, Description, IsDefault, IsActive, CreatedAt, UpdatedAt)
                SELECT
                    ProductId,
                    Id,
                    'DEFAULT',
                    'Default',
                    'Default field mapping template.',
                    1,
                    1,
                    '2026-01-01 00:00:00',
                    '2026-01-01 00:00:00'
                FROM IssueTypeMappings
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM IssueFieldMappingTemplates t
                    WHERE t.ProductId = IssueTypeMappings.ProductId
                      AND t.IssueTypeMappingId = IssueTypeMappings.Id
                      AND t.TemplateCode = 'DEFAULT'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueFieldMappingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_IssueFieldMappings_ProductId_IssueTypeMappingId_TemplateCode_SourcePath",
                table: "IssueFieldMappings");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "IssueFieldMappings");

            migrationBuilder.CreateIndex(
                name: "IX_IssueFieldMappings_ProductId_IssueTypeMappingId_SourcePath",
                table: "IssueFieldMappings",
                columns: new[] { "ProductId", "IssueTypeMappingId", "SourcePath" },
                unique: true);
        }
    }
}
