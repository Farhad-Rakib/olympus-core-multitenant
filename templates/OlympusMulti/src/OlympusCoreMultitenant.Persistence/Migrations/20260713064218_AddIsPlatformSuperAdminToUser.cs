using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympusCoreMultitenant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPlatformSuperAdminToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformSuperAdmin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlatformSuperAdmin",
                table: "users");
        }
    }
}
