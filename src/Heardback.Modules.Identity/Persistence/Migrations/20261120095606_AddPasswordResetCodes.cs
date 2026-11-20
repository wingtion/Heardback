using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddPasswordResetCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_changed_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "password_reset_codes",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_codes_users_issued_by_user_id",
                        column: x => x.issued_by_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_password_reset_codes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_codes_code_hash",
                schema: "identity",
                table: "password_reset_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_codes_issued_by_user_id",
                schema: "identity",
                table: "password_reset_codes",
                column: "issued_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_codes_user_id",
                schema: "identity",
                table: "password_reset_codes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_codes",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                schema: "identity",
                table: "users");
        }
    }
}
