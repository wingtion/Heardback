using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Matching.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class InitialMatchingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "matching");

            migrationBuilder.CreateTable(
                name: "cvs",
                schema: "matching",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cvs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cvs_user_id",
                schema: "matching",
                table: "cvs",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cvs",
                schema: "matching");
        }
    }
}
