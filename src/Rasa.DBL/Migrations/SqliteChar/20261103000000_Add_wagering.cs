using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// Item wagering. A character's wagered item is a character_inventory row of the wager
    /// inventory type and needs no column; whether it is locked in its slot does. A clan feud and
    /// a challenge to one now say which character made the challenge, and a feud which accepted
    /// it: the wagered items a feud's losers forfeit are mailed to that character of the winning
    /// clan when its lockbox has no room. Existing rows start unlocked and name nobody.
    /// </summary>
    public partial class Add_wagering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "wager_locked",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "challenger_character_id",
                table: "clan_feud",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "target_character_id",
                table: "clan_feud",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "challenger_character_id",
                table: "clan_feud_challenge",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "wager_locked", table: "character");
            migrationBuilder.DropColumn(name: "challenger_character_id", table: "clan_feud");
            migrationBuilder.DropColumn(name: "target_character_id", table: "clan_feud");
            migrationBuilder.DropColumn(name: "challenger_character_id", table: "clan_feud_challenge");
        }
    }
}
