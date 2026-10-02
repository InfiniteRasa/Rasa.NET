using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// Who holds each control point that has changed hands (the game server's ControlPoints): the
    /// world database's control_point id, the owner (0 the Bane, 1 the AFS) and when it changed
    /// (Unix milliseconds, UTC). Written on every capture and read back when the server starts,
    /// so ownership lasts through a restart; a point with no row is with its default owner.
    /// </summary>
    public partial class Add_control_point_state : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "control_point_state",
                columns: table => new
                {
                    control_point_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    owner = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    changed_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_point_state", x => x.control_point_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_point_state");
        }
    }
}
