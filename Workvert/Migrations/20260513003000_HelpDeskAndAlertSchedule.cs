using System;
using Workvert.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvert.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260513003000_HelpDeskAndAlertSchedule")]
    public partial class HelpDeskAndAlertSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlertScheduleEnabled",
                table: "UserNotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AlertTimeZone",
                table: "UserNotificationSettings",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertWindowDays",
                table: "UserNotificationSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertWindowEnd",
                table: "UserNotificationSettings",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertWindowStart",
                table: "UserNotificationSettings",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_UserId_CreatedAtUtc",
                table: "SupportTickets",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "AlertScheduleEnabled",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AlertTimeZone",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AlertWindowDays",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AlertWindowEnd",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "AlertWindowStart",
                table: "UserNotificationSettings");
        }
    }
}
