using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDisabledAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAtUtc",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledAtUtc",
                table: "tenants");
        }
    }
}
