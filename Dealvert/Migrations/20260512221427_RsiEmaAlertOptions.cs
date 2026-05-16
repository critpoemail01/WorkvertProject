using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class RsiEmaAlertOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FastEmaPeriod",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "IndicatorArmed",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEvaluatedAtUtc",
                table: "Alerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastIndicatorValue",
                table: "Alerts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RsiPeriod",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddColumn<int>(
                name: "SlowEmaPeriod",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "Timeframe",
                table: "Alerts",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "4h");

            migrationBuilder.AddColumn<decimal>(
                name: "ZonePercent",
                table: "Alerts",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 1.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FastEmaPeriod",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IndicatorArmed",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedAtUtc",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "LastIndicatorValue",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "RsiPeriod",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "SlowEmaPeriod",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Timeframe",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ZonePercent",
                table: "Alerts");
        }
    }
}
