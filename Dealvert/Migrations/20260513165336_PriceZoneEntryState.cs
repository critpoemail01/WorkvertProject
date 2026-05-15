using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alivert.Migrations
{
    /// <inheritdoc />
    public partial class PriceZoneEntryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PriceZoneWasInside",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceZoneWasInside",
                table: "Alerts");
        }
    }
}
