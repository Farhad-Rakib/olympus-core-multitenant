using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OlympusCoreMultitenant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "user_roles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "site_settings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "roles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "role_permissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "refresh_tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "password_reset_tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "menus",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId",
                table: "users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_TenantId",
                table: "user_roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_TenantId",
                table: "site_settings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_TenantId",
                table: "roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_TenantId",
                table: "role_permissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TenantId",
                table: "password_reset_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TenantId",
                table: "notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_menus_TenantId",
                table: "menus",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Slug",
                table: "tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_menus_tenants_TenantId",
                table: "menus",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_tenants_TenantId",
                table: "notifications",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_tokens_tenants_TenantId",
                table: "password_reset_tokens",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_tenants_TenantId",
                table: "refresh_tokens",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_tenants_TenantId",
                table: "role_permissions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_tenants_TenantId",
                table: "roles",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_site_settings_tenants_TenantId",
                table: "site_settings",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_tenants_TenantId",
                table: "user_roles",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_TenantId",
                table: "users",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill: create a "default" tenant and assign every existing row to it, so the
            // TenantId columns above can be tightened to NOT NULL in the follow-up migration.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    default_tenant_id BIGINT;
BEGIN
    INSERT INTO tenants (""Slug"", ""Name"", ""IsActive"", ""CreatedAt"", ""CreatedBy"", ""UpdatedBy"", ""UpdatedAt"")
    VALUES ('default', 'Default', TRUE, NOW(), 0, 0, NOW())
    RETURNING ""Id"" INTO default_tenant_id;

    UPDATE users SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE roles SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE menus SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE site_settings SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE notifications SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE refresh_tokens SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE password_reset_tokens SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE user_roles SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
    UPDATE role_permissions SET ""TenantId"" = default_tenant_id WHERE ""TenantId"" IS NULL;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menus_tenants_TenantId",
                table: "menus");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_tenants_TenantId",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_tokens_tenants_TenantId",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_tenants_TenantId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_role_permissions_tenants_TenantId",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_tenants_TenantId",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_site_settings_tenants_TenantId",
                table: "site_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_tenants_TenantId",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_TenantId",
                table: "users");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_users_TenantId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_roles_TenantId",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "IX_site_settings_TenantId",
                table: "site_settings");

            migrationBuilder.DropIndex(
                name: "IX_roles_TenantId",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_role_permissions_TenantId",
                table: "role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_TenantId",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_notifications_TenantId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_menus_TenantId",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "menus");
        }
    }
}
