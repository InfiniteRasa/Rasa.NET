using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The records of PvP matches (the game server's PvpRecords): pvp_match, one row a clan feud,
    /// squad wargame or battleground match, with when it was fought, how it ended, who won and
    /// the two sides' scores; and pvp_match_player, one row a match and character, with the side
    /// they were on and their own score.
    /// </summary>
    public partial class Add_pvp_records : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pvp_match",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    kind = table.Column<byte>(type: "INTEGER", nullable: false),
                    wargame_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    instance_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ended_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    outcome = table.Column<byte>(type: "INTEGER", nullable: false),
                    winner_side = table.Column<byte>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "varchar(32)", nullable: false),
                    side1_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    side1_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    side1_score = table.Column<int>(type: "INTEGER", nullable: false),
                    side1_kills = table.Column<int>(type: "INTEGER", nullable: false),
                    side2_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    side2_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    side2_score = table.Column<int>(type: "INTEGER", nullable: false),
                    side2_kills = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pvp_match", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pvp_match_player",
                columns: table => new
                {
                    match_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    side = table.Column<byte>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    kills = table.Column<int>(type: "INTEGER", nullable: false),
                    deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    damage = table.Column<int>(type: "INTEGER", nullable: false),
                    healing = table.Column<int>(type: "INTEGER", nullable: false),
                    captures = table.Column<int>(type: "INTEGER", nullable: false),
                    prestige = table.Column<int>(type: "INTEGER", nullable: false),
                    present_at_end = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pvp_match_player", x => new { x.match_id, x.character_id });
                });

            migrationBuilder.CreateIndex(
                name: "pvp_match_player_index_character_id",
                table: "pvp_match_player",
                column: "character_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "pvp_match_player");
            migrationBuilder.DropTable(name: "pvp_match");
        }
    }
}
