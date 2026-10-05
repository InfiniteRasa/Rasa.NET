using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// The bosses a character has killed (the game server's BossTitles): the character and the
    /// boss's creature name id, the name the client knows it by. One row per boss a character
    /// has killed, written on the first kill; a battlefield's boss title is given when every
    /// boss on its list has a row. No rows are added here: kills made before this are not known.
    /// </summary>
    public partial class Add_character_boss_kill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_boss_kill",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_name_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_boss_kill", x => new { x.character_id, x.creature_name_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_boss_kill");
        }
    }
}
