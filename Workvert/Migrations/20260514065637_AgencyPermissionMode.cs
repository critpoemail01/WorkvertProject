using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvert.Migrations
{
    /// <inheritdoc />
    public partial class AgencyPermissionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgencyPermissionMode",
                table: "UserNotificationSettings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgencyPermissionMode",
                table: "UserNotificationSettings");
        }
    }
}
