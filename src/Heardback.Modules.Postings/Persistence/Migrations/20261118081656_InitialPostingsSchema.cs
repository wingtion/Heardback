using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Postings.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class InitialPostingsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "postings");

            migrationBuilder.CreateTable(
                name: "job_postings",
                schema: "postings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    normalized_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    times_added = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_postings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_postings_content_hash",
                schema: "postings",
                table: "job_postings",
                column: "content_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_postings_normalized_url",
                schema: "postings",
                table: "job_postings",
                column: "normalized_url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_postings",
                schema: "postings");
        }
    }
}
