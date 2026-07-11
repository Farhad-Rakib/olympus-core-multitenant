using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympusCoreMultitenant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantIdNotNullAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_TenantId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_site_settings_Key",
                table: "site_settings");

            migrationBuilder.DropIndex(
                name: "IX_site_settings_TenantId",
                table: "site_settings");

            migrationBuilder.DropIndex(
                name: "IX_roles_Name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_TenantId",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAtUtc",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_TenantId",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_UserId_ExpiresAtUtc",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_notifications_TenantId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_IsRead",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_menus_TenantId",
                table: "menus");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "user_roles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "site_settings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "roles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "role_permissions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "refresh_tokens",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "password_reset_tokens",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "notifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "menus",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId_Email",
                table: "users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_TenantId_Key",
                table: "site_settings",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_TenantId_Name",
                table: "roles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId_UserId_ExpiresAtUtc",
                table: "refresh_tokens",
                columns: new[] { "TenantId", "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TenantId_UserId_ExpiresAtUtc",
                table: "password_reset_tokens",
                columns: new[] { "TenantId", "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TenantId_UserId",
                table: "notifications",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TenantId_UserId_IsRead",
                table: "notifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_menus_TenantId_Title",
                table: "menus",
                columns: new[] { "TenantId", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_TenantId_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_site_settings_TenantId_Key",
                table: "site_settings");

            migrationBuilder.DropIndex(
                name: "IX_roles_TenantId_Name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TenantId_UserId_ExpiresAtUtc",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_TenantId_UserId_ExpiresAtUtc",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_notifications_TenantId_UserId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_TenantId_UserId_IsRead",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_menus_TenantId_Title",
                table: "menus");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "user_roles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "site_settings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "roles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "role_permissions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "refresh_tokens",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "password_reset_tokens",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "notifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                table: "menus",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId",
                table: "users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_Key",
                table: "site_settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_TenantId",
                table: "site_settings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_TenantId",
                table: "roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAtUtc",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TenantId",
                table: "password_reset_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId_ExpiresAtUtc",
                table: "password_reset_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TenantId",
                table: "notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_menus_TenantId",
                table: "menus",
                column: "TenantId");
        }
    }
}
