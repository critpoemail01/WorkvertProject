using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealvert.Migrations
{
    /// <inheritdoc />
    public partial class AudienceLocationTargeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudienceCity",
                table: "MarketingPlans",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudienceCountry",
                table: "MarketingPlans",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AudienceLatitude",
                table: "MarketingPlans",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudienceLocationScope",
                table: "MarketingPlans",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "World");

            migrationBuilder.AddColumn<double>(
                name: "AudienceLongitude",
                table: "MarketingPlans",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudienceRadiusKm",
                table: "MarketingPlans",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudienceCity",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "AudienceCountry",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "AudienceLatitude",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "AudienceLocationScope",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "AudienceLongitude",
                table: "MarketingPlans");

            migrationBuilder.DropColumn(
                name: "AudienceRadiusKm",
                table: "MarketingPlans");
        }
    }
}
