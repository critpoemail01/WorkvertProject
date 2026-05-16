using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class NotificationSettingsAndDeliveryLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertTriggerId = table.Column<int>(type: "int", nullable: true),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertDeliveryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertDeliveryLogs_AlertTriggers_AlertTriggerId",
                        column: x => x.AlertTriggerId,
                        principalTable: "AlertTriggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WebhookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscordWebhookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TelegramChatId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveryLogs_AlertId_CreatedAtUtc",
                table: "AlertDeliveryLogs",
                columns: new[] { "AlertId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveryLogs_AlertTriggerId",
                table: "AlertDeliveryLogs",
                column: "AlertTriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveryLogs_Status_CreatedAtUtc",
                table: "AlertDeliveryLogs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveryLogs_UserId_CreatedAtUtc",
                table: "AlertDeliveryLogs",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertDeliveryLogs");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings");
        }
    }
}
