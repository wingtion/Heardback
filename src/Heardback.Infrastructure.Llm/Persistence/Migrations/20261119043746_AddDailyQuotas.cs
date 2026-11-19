using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Infrastructure.Llm.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddDailyQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "quota_day",
                schema: "llm",
                table: "analysis_jobs",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "daily_call_counts",
                schema: "llm",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_call_counts", x => x.day);
                });

            migrationBuilder.CreateTable(
                name: "daily_usage",
                schema: "llm",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_usage", x => new { x.user_id, x.day, x.kind });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_call_counts",
                schema: "llm");

            migrationBuilder.DropTable(
                name: "daily_usage",
                schema: "llm");

            migrationBuilder.DropColumn(
                name: "quota_day",
                schema: "llm",
                table: "analysis_jobs");
        }
    }
}
