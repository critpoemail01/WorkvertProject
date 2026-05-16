using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class CampaignLandingCrmLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessDna",
                table: "MarketingPlans",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CrmLeadFilter",
                table: "MarketingPlans",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrmLeadSourceCount",
                table: "MarketingPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CrmIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiKeyHint = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmLeads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingLandingPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingPlanId = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Subheadline = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    PrimaryCallToAction = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FormTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FormIntro = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ThankYouMessage = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingLandingPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingLandingPages_MarketingPlans_MarketingPlanId",
                        column: x => x.MarketingPlanId,
                        principalTable: "MarketingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingLandingLeads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingLandingPageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingLandingLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingLandingLeads_MarketingLandingPages_MarketingLandingPageId",
                        column: x => x.MarketingLandingPageId,
                        principalTable: "MarketingLandingPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmIntegrations_UserId_Provider",
                table: "CrmIntegrations",
                columns: new[] { "UserId", "Provider" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_UserId_Email",
                table: "CrmLeads",
                columns: new[] { "UserId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_UserId_Industry",
                table: "CrmLeads",
                columns: new[] { "UserId", "Industry" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_UserId_Stage",
                table: "CrmLeads",
                columns: new[] { "UserId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLandingLeads_Email",
                table: "MarketingLandingLeads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLandingLeads_MarketingLandingPageId_CreatedAtUtc",
                table: "MarketingLandingLeads",
                columns: new[] { "MarketingLandingPageId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLandingPages_MarketingPlanId",
                table: "MarketingLandingPages",
                column: "MarketingPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLandingPages_Slug",
                table: "MarketingLandingPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLandingPages_Status_PublishedAtUtc",
                table: "MarketingLandingPages",
                columns: new[] { "Status", "PublishedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmIntegrations");

            migrationBuilder.DropTable(
                name: "CrmLeads");

            migrationBuilder.DropTable(
                name: "MarketingLandingLeads");

            migrationBuilder.DropTable(
                name: "MarketingLandingPages");

            migrationBuilder.DropColumn(
                name: "BusinessDna",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "CrmLeadFilter",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "CrmLeadSourceCount",
                table: "MarketingPlans");
        }
    }
}
