using Workvert.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvert.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260513010000_NotificationDeliveryChannels")]
    public partial class NotificationDeliveryChannels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlackWebhookUrl",
                table: "UserNotificationSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamsWebhookUrl",
                table: "UserNotificationSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlackWebhookUrl",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "TeamsWebhookUrl",
                table: "UserNotificationSettings");
        }
    }
}
