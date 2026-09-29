using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// The cooldowns a character leaves the world with: one row per action still cooling down by
    /// at least a few seconds, with the time it is ready again (Unix milliseconds, UTC). Written
    /// when the character logs out or drops, read back and removed when it is next loaded, so an
    /// hour-long account reward or a five-minute class wave survives a relog as it did on the
    /// live servers ("the persisted reuse times of actions", the client's Recv_ActionReuseTimes).
    /// </summary>
    public partial class Add_character_action_reuse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_action_reuse",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ready_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_action_reuse", x => new { x.character_id, x.action_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_action_reuse");
        }
    }
}
