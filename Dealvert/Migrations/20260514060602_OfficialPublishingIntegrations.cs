using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class OfficialPublishingIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailProvider",
                table: "UserNotificationSettings",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FacebookAuthorized",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FacebookAuthorizedAtUtc",
                table: "UserNotificationSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageId",
                table: "UserNotificationSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageName",
                table: "UserNotificationSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookScopes",
                table: "UserNotificationSettings",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GoogleBusinessAuthorized",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleBusinessAuthorizedAtUtc",
                table: "UserNotificationSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleBusinessProfileName",
                table: "UserNotificationSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InstagramAuthorized",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstagramAuthorizedAtUtc",
                table: "UserNotificationSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramBusinessAccountId",
                table: "UserNotificationSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramBusinessAccountName",
                table: "UserNotificationSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramScopes",
                table: "UserNotificationSettings",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LinkedInAuthorized",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LinkedInAuthorizedAtUtc",
                table: "UserNotificationSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInOrganizationId",
                table: "UserNotificationSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInOrganizationName",
                table: "UserNotificationSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInScopes",
                table: "UserNotificationSettings",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppAuthorized",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppAuthorizedAtUtc",
                table: "UserNotificationSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppProviderName",
                table: "UserNotificationSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailProvider",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "FacebookAuthorized",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "FacebookAuthorizedAtUtc",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "FacebookPageId",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "FacebookPageName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "FacebookScopes",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "GoogleBusinessAuthorized",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "GoogleBusinessAuthorizedAtUtc",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "GoogleBusinessProfileName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "InstagramAuthorized",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "InstagramAuthorizedAtUtc",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "InstagramBusinessAccountId",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "InstagramBusinessAccountName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "InstagramScopes",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "LinkedInAuthorized",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "LinkedInAuthorizedAtUtc",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "LinkedInOrganizationId",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "LinkedInOrganizationName",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "LinkedInScopes",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppAuthorized",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppAuthorizedAtUtc",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppProviderName",
                table: "UserNotificationSettings");
        }
    }
}
