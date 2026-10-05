using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// Clan-owned control points (the game server's ControlPoints): control_point_state names
    /// the clan that holds a point for the AFS - 0 for none - and up to when that clan has been
    /// paid for holding it, Unix milliseconds, UTC. Every point there is starts with no clan.
    /// </summary>
    public partial class Add_control_point_clan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "clan_id",
                table: "control_point_state",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "clan_paid_at",
                table: "control_point_state",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "clan_paid_at", table: "control_point_state");
            migrationBuilder.DropColumn(name: "clan_id", table: "control_point_state");
        }
    }
}
