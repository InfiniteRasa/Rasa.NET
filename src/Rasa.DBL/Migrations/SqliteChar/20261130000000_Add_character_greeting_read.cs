using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The important lines a character has read (the game server's NpcGreetings): the
    /// character, the NPC's creature row, and the line it was - an id of the client's
    /// npcgreetinglanguage. One row per NPC a character has been to, written when the NPC's
    /// marked line is shown to them; the speech bubble is not over that NPC for them again
    /// until it has another line. No rows are added here: nobody has read anything yet.
    ///
    /// Down drops the table: every marked NPC is unread for everyone.
    /// </summary>
    public partial class Add_character_greeting_read : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_greeting_read",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    creature_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    greeting_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_greeting_read", x => new { x.character_id, x.creature_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_greeting_read");
        }
    }
}
