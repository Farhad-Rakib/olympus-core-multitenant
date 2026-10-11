using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddProcessedBillingEventsAndPlanPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingInterval",
                table: "subscription_plans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                // Backfill for existing rows; the enum is stored as a string, and "" would not parse.
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "subscription_plans",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "ExternalPriceId",
                table: "subscription_plans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "subscription_plans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "processed_billing_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_billing_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_ExternalPriceId",
                table: "subscription_plans",
                column: "ExternalPriceId",
                unique: true,
                filter: "[ExternalPriceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processed_billing_events_Provider_ExternalEventId",
                table: "processed_billing_events",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_billing_events_TenantId",
                table: "processed_billing_events",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_billing_events");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_ExternalPriceId",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "BillingInterval",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "ExternalPriceId",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "subscription_plans");
        }
    }
}
