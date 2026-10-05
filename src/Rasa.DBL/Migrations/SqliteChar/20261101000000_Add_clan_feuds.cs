using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The clan feuds under way and the feud challenges not yet answered, so both last through a
    /// restart (the game server's ClanFeuds). A feud keeps its wargame id, its two clans, its score
    /// and when it ends (Unix milliseconds, UTC); a challenge keeps the wargame id its feud will
    /// have and its two clans. Duels and squad wargames are not kept.
    /// </summary>
    public partial class Add_clan_feuds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clan_feud",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    challenger_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    target_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    ends_at = table.Column<long>(type: "INTEGER", nullable: false),
                    challenger_kills = table.Column<int>(type: "INTEGER", nullable: false),
                    target_kills = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_feud", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clan_feud_challenge",
                columns: table => new
                {
                    wargame_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    challenger_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    target_clan_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_feud_challenge", x => x.wargame_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan_feud_challenge");

            migrationBuilder.DropTable(
                name: "clan_feud");
        }
    }
}
