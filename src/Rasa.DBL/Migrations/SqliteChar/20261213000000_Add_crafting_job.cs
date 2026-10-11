using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The crafting stations' jobs (the game server's KraftwerksManager): crafting_job, one row an
    /// item a station is making or holding for a character - its result, how many are left to
    /// take, when it is done - so that a restart, a station moved or a map closed does not lose
    /// the ingredients and credits already taken for it.
    /// </summary>
    public partial class Add_crafting_job : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crafting_job",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    kraftwerks_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    result_class_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    result_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    count = table.Column<uint>(type: "INTEGER", nullable: false),
                    crafting_page = table.Column<uint>(type: "INTEGER", nullable: false),
                    quality_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    finish_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crafting_job", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "crafting_job_index_character_id",
                table: "crafting_job",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "crafting_job_index_kraftwerks_id",
                table: "crafting_job",
                column: "kraftwerks_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "crafting_job");
        }
    }
}
