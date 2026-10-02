using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The characters who left a clan while it was at feud, with an item wagered: one row per
    /// feud and character, naming the clan they left. Their wager stays at stake for that feud.
    /// </summary>
    public partial class Add_clan_feud_stakes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clan_feud_stake",
                columns: table => new
                {
                    feud_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    clan_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_feud_stake", x => new { x.feud_id, x.character_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "clan_feud_stake");
        }
    }
}
