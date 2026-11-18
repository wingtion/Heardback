using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Applications.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddPerUserPostingKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                schema: "applications",
                table: "applications",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_url",
                schema: "applications",
                table: "applications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_user_id_content_hash",
                schema: "applications",
                table: "applications",
                columns: new[] { "user_id", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_user_id_normalized_url",
                schema: "applications",
                table: "applications",
                columns: new[] { "user_id", "normalized_url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_applications_user_id_content_hash",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropIndex(
                name: "ix_applications_user_id_normalized_url",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "content_hash",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "normalized_url",
                schema: "applications",
                table: "applications");
        }
    }
}
