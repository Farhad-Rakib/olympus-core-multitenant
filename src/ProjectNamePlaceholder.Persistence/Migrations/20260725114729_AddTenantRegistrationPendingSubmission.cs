using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectNamePlaceholder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRegistrationPendingSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "tenant_registration_tokens",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingFullName",
                table: "tenant_registration_tokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPasswordHash",
                table: "tenant_registration_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "tenant_registration_tokens",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "tenant_registration_tokens");

            migrationBuilder.DropColumn(
                name: "PendingFullName",
                table: "tenant_registration_tokens");

            migrationBuilder.DropColumn(
                name: "PendingPasswordHash",
                table: "tenant_registration_tokens");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "tenant_registration_tokens");
        }
    }
}
