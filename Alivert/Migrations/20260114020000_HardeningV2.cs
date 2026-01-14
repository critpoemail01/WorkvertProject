using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alivert.Migrations
{
    public partial class HardeningV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Alerts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<decimal>(
                name: "Threshold",
                table: "Alerts",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserId_IsEnabled",
                table: "Alerts",
                columns: new[] { "UserId", "IsEnabled" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_UserId_IsEnabled",
                table: "Alerts");

            migrationBuilder.AlterColumn<decimal>(
                name: "Threshold",
                table: "Alerts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Alerts");
        }
    }
}
