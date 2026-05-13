using Alivert.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alivert.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260513001500_UnlimitedAnnualPlans")]
    public partial class UnlimitedAnnualPlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "CreditPurchases",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "credits");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionDays",
                table: "CreditPurchases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE CreditPurchases
                SET PlanCode = CASE WHEN Credits = 0 THEN 'unlimited-monthly' ELSE 'credits' END,
                    SubscriptionDays = CASE WHEN Credits = 0 THEN 30 ELSE 0 END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "SubscriptionDays",
                table: "CreditPurchases");
        }
    }
}
