using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The wagered items of recorded PvP matches (the game server's PvpRecords): pvp_match_wager,
    /// one row a match and character who had an item wagered when the match ended, with the
    /// item and what became of it - kept, or forfeit to the winning clan's lockbox or to a
    /// member's pick-up box.
    /// </summary>
    public partial class Add_pvp_wagers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pvp_match_wager",
                columns: table => new
                {
                    match_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    side = table.Column<byte>(type: "INTEGER", nullable: false),
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    quality_id = table.Column<int>(type: "INTEGER", nullable: false),
                    stack_size = table.Column<uint>(type: "INTEGER", nullable: false),
                    result = table.Column<byte>(type: "INTEGER", nullable: false),
                    recipient_clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    recipient_character_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pvp_match_wager", x => new { x.match_id, x.character_id });
                });

            migrationBuilder.CreateIndex(
                name: "pvp_match_wager_index_character_id",
                table: "pvp_match_wager",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "pvp_match_wager_index_item_id",
                table: "pvp_match_wager",
                column: "item_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "pvp_match_wager");
        }
    }
}
