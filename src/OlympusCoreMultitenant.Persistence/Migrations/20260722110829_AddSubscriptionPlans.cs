using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OlympusCoreMultitenant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubscriptionPlanId",
                table: "tenants",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plan_modules",
                columns: table => new
                {
                    SubscriptionPlanId = table.Column<long>(type: "bigint", nullable: false),
                    ModuleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plan_modules", x => new { x.SubscriptionPlanId, x.ModuleId });
                    table.ForeignKey(
                        name: "FK_subscription_plan_modules_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscription_plan_modules_subscription_plans_SubscriptionPl~",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_SubscriptionPlanId",
                table: "tenants",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_modules_ModuleId",
                table: "subscription_plan_modules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Key",
                table: "subscription_plans",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tenants_subscription_plans_SubscriptionPlanId",
                table: "tenants",
                column: "SubscriptionPlanId",
                principalTable: "subscription_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenants_subscription_plans_SubscriptionPlanId",
                table: "tenants");

            migrationBuilder.DropTable(
                name: "subscription_plan_modules");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_tenants_SubscriptionPlanId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "tenants");
        }
    }
}
