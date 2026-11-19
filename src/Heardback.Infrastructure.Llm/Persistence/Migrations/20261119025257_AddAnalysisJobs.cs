using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Infrastructure.Llm.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddAnalysisJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "llm");

            migrationBuilder.CreateTable(
                name: "analysis_jobs",
                schema: "llm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subject_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_kind_subject_key_created_at",
                schema: "llm",
                table: "analysis_jobs",
                columns: new[] { "kind", "subject_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_status_next_run_at",
                schema: "llm",
                table: "analysis_jobs",
                columns: new[] { "status", "next_run_at" });

            migrationBuilder.CreateIndex(
                name: "ux_analysis_jobs_one_unfinished",
                schema: "llm",
                table: "analysis_jobs",
                columns: new[] { "kind", "subject_key" },
                unique: true,
                filter: "status IN ('Pending', 'Running')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_jobs",
                schema: "llm");
        }
    }
}
