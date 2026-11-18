using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Matching.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddExcludedCvSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "excluded_skills",
                schema: "matching",
                table: "cvs",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "excluded_skills",
                schema: "matching",
                table: "cvs");
        }
    }
}
