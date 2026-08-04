using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProvisioningAndRegistrationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProvisioned",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: any tenant that already has at least one Role was provisioned by
            // RbacSeeder before this column existed. Without this, "default" (and any other
            // already-active tenant) would wrongly appear unprovisioned -- blocking Assign Plan
            // and re-showing the Activate/Registration Link actions for a tenant that's already live.
            migrationBuilder.Sql(
                "UPDATE tenants SET \"IsProvisioned\" = true WHERE \"Id\" IN (SELECT DISTINCT \"TenantId\" FROM roles);");

            migrationBuilder.CreateTable(
                name: "tenant_registration_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_registration_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_registration_tokens_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_tokens_TenantId",
                table: "tenant_registration_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_registration_tokens_Token",
                table: "tenant_registration_tokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_registration_tokens");

            migrationBuilder.DropColumn(
                name: "IsProvisioned",
                table: "tenants");
        }
    }
}
