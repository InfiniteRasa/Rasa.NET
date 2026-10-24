using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The initial Bootcamp seed includes complete evidence notes longer than 256 characters.
    /// Widen before SeedWorldContent; the later Wilderness widening would run too late on MySQL.
    /// </summary>
    public partial class WidenMissionEvidenceNotesBeforeSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "reconstruction_note",
                table: "mission_evidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(256)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not shrink and truncate evidence notes on rollback.
        }
    }
}
