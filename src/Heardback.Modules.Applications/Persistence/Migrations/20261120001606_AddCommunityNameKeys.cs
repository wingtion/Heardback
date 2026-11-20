using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Applications.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddCommunityNameKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_key",
                schema: "applications",
                table: "applications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role_key",
                schema: "applications",
                table: "applications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_company_key_role_key",
                schema: "applications",
                table: "applications",
                columns: new[] { "company_key", "role_key" });

            migrationBuilder.CreateIndex(
                name: "ix_applications_posting_id",
                schema: "applications",
                table: "applications",
                column: "posting_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_applications_company_key_role_key",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropIndex(
                name: "ix_applications_posting_id",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "company_key",
                schema: "applications",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "role_key",
                schema: "applications",
                table: "applications");
        }
    }
}
