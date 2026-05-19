using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvert.Migrations
{
    /// <inheritdoc />
    public partial class ClientServiceRequestPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPaths",
                table: "ClientServiceRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUsageNote",
                table: "ClientServiceRequests",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: false,
                defaultValue: "Photos are used only to understand and document the requested service, not to evaluate personal characteristics.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPaths",
                table: "ClientServiceRequests");

            migrationBuilder.DropColumn(
                name: "PhotoUsageNote",
                table: "ClientServiceRequests");
        }
    }
}
