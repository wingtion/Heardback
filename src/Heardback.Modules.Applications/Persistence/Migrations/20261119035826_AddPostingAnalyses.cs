using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heardback.Modules.Applications.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddPostingAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "posting_analyses",
                schema: "applications",
                columns: table => new
                {
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    prompt_version = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seniority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    work_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    analyzed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posting_analyses", x => x.content_hash);
                });

            migrationBuilder.CreateIndex(
                name: "ix_applications_content_hash",
                schema: "applications",
                table: "applications",
                column: "content_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "posting_analyses",
                schema: "applications");

            migrationBuilder.DropIndex(
                name: "ix_applications_content_hash",
                schema: "applications",
                table: "applications");
        }
    }
}
