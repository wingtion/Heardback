using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddDemoAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.CreateIndex(
                name: "ix_users_expires_at",
                schema: "identity",
                table: "users",
                column: "expires_at",
                filter: "expires_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_expires_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "kind",
                schema: "identity",
                table: "users");
        }
    }
}
