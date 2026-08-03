using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLicensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiresAtUtc",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAtUtc",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LicenseExpiresAtUtc",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAtUtc",
                table: "tenants");
        }
    }
}
