using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The control points of the open world (the game server's ControlPoints): control_point, one
    /// row per point the client has a map marker for in the fifteen open zones, and
    /// control_point_link, what belongs to each - the spawn pools of the Bane's and the AFS's
    /// garrison, its hospital and waypoint, and the bosses of its garrison (ControlPointSeed).
    ///
    /// No spawn pool, teleporter or creature row is changed: the pools already set aside as
    /// control point garrisons (mode 1) are linked as they stand, and the server runs them while
    /// the Bane hold the point. Who holds each point is kept in the character database
    /// (control_point_state).
    ///
    /// Down drops both tables.
    /// </summary>
    public partial class Add_control_points : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ControlPointEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    marker_entity_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    default_owner = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_point", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: ControlPointLinkEntry.TableName,
                columns: table => new
                {
                    control_point_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    object_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_point_link", x => new { x.control_point_id, x.kind, x.object_id });
                });

            foreach (var insert in ControlPointSeed.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: ControlPointLinkEntry.TableName);

            migrationBuilder.DropTable(
                name: ControlPointEntry.TableName);
        }
    }
}
