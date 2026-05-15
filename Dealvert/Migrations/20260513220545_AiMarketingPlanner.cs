using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alivert.Migrations
{
    /// <inheritdoc />
    public partial class AiMarketingPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ProductUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompanyOrIdea = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    ValueProposition = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    CampaignGoal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Platforms = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EmailAudience = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingEmailSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingPlanId = table.Column<int>(type: "int", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PreviewText = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AudienceSegment = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    EstimatedReach = table.Column<int>(type: "int", nullable: false),
                    EstimatedInteractions = table.Column<int>(type: "int", nullable: false),
                    EstimatedConversions = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingEmailSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingEmailSuggestions_MarketingPlans_MarketingPlanId",
                        column: x => x.MarketingPlanId,
                        principalTable: "MarketingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingLeadSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingPlanId = table.Column<int>(type: "int", nullable: false),
                    CompanyProfile = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EmailSearchHint = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingLeadSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingLeadSuggestions_MarketingPlans_MarketingPlanId",
                        column: x => x.MarketingPlanId,
                        principalTable: "MarketingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingPostSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketingPlanId = table.Column<int>(type: "int", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    CreativeBrief = table.Column<string>(type: "nvarchar(900)", maxLength: 900, nullable: false),
                    Hashtags = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CallToAction = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    EstimatedReach = table.Column<int>(type: "int", nullable: false),
                    EstimatedInteractions = table.Column<int>(type: "int", nullable: false),
                    EstimatedConversions = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingPostSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingPostSuggestions_MarketingPlans_MarketingPlanId",
                        column: x => x.MarketingPlanId,
                        principalTable: "MarketingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmailSuggestions_MarketingPlanId_ScheduledForUtc",
                table: "MarketingEmailSuggestions",
                columns: new[] { "MarketingPlanId", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmailSuggestions_Status_ScheduledForUtc",
                table: "MarketingEmailSuggestions",
                columns: new[] { "Status", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingLeadSuggestions_MarketingPlanId_Industry",
                table: "MarketingLeadSuggestions",
                columns: new[] { "MarketingPlanId", "Industry" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPlans_UserId_CreatedAtUtc",
                table: "MarketingPlans",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPlans_UserId_Status",
                table: "MarketingPlans",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostSuggestions_MarketingPlanId_ScheduledForUtc",
                table: "MarketingPostSuggestions",
                columns: new[] { "MarketingPlanId", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingPostSuggestions_Status_ScheduledForUtc",
                table: "MarketingPostSuggestions",
                columns: new[] { "Status", "ScheduledForUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingEmailSuggestions");

            migrationBuilder.DropTable(
                name: "MarketingLeadSuggestions");

            migrationBuilder.DropTable(
                name: "MarketingPostSuggestions");

            migrationBuilder.DropTable(
                name: "MarketingPlans");
        }
    }
}
