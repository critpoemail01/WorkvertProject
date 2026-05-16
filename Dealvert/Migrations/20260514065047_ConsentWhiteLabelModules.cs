using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class ConsentWhiteLabelModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgencyBrandColor",
                table: "UserNotificationSettings",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyName",
                table: "UserNotificationSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyReportFooter",
                table: "UserNotificationSettings",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyWorkspaceName",
                table: "UserNotificationSettings",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentText",
                table: "MarketingLandingLeads",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentedAtUtc",
                table: "MarketingLandingLeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsentAccepted",
                table: "MarketingLandingLeads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConsentSource",
                table: "CrmLeads",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentStatus",
                table: "CrmLeads",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentedAtUtc",
                table: "CrmLeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnsubscribedAtUtc",
                table: "CrmLeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_UserId_ConsentStatus",
                table: "CrmLeads",
                columns: new[] { "UserId", "ConsentStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_UserId_ConsentStatus",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "AgencyBrandColor",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AgencyName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AgencyReportFooter",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AgencyWorkspaceName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "ConsentText",
                table: "MarketingLandingLeads");

            migrationBuilder.DropColumn(
                name: "ConsentedAtUtc",
                table: "MarketingLandingLeads");

            migrationBuilder.DropColumn(
                name: "MarketingConsentAccepted",
                table: "MarketingLandingLeads");

            migrationBuilder.DropColumn(
                name: "ConsentSource",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "ConsentStatus",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "ConsentedAtUtc",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "UnsubscribedAtUtc",
                table: "CrmLeads");
        }
    }
}
