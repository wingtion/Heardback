using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTrail.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddRefreshTokenSessionExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "session_expires_at",
                schema: "identity",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            // Hand-edited: existing sessions keep their current expiry as their absolute end,
            // instead of the generated year-0001 default that would end them on the next refresh.
            migrationBuilder.Sql("UPDATE identity.refresh_tokens SET session_expires_at = expires_at;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "session_expires_at",
                schema: "identity",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "session_expires_at",
                schema: "identity",
                table: "refresh_tokens");
        }
    }
}
