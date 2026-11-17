using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddRefreshTokenReplacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "replaced_by_token_id",
                schema: "identity",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "replaced_by_token_id",
                schema: "identity",
                table: "refresh_tokens");
        }
    }
}
