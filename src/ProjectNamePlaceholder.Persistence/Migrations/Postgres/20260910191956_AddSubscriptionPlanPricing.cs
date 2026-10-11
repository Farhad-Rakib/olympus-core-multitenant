using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingInterval",
                table: "subscription_plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // NOT "" as EF scaffolded: an empty string is not a BillingInterval member and
                // every existing plan would fail to materialise. Plans that predate pricing are
                // by definition not sold on a cycle.
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "subscription_plans",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                // "" is not a valid ISO 4217 code; fall back to the configured default currency.
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "ExternalPriceId",
                table: "subscription_plans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "subscription_plans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_ExternalPriceId",
                table: "subscription_plans",
                column: "ExternalPriceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
