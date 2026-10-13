using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// world_destructible: the destructible world objects the server puts on maps
    /// (WorldDestructibleEntry; the game server's PlacedDestructibles) - Bane barrels, lockers,
    /// sleep pods, machinery, creature spawners, force fields - which players shoot down and which
    /// come back. No rows: they are put in by later migrations (WorldDestructiblePreloader).
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_world_destructibles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: WorldDestructibleEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    class_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    pos_x = table.Column<double>(type: "REAL", nullable: false),
                    pos_y = table.Column<double>(type: "REAL", nullable: false),
                    pos_z = table.Column<double>(type: "REAL", nullable: false),
                    rotation = table.Column<double>(type: "REAL", nullable: false),
                    hit_points = table.Column<uint>(type: "INTEGER", nullable: false),
                    creature_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    creature_count = table.Column<uint>(type: "INTEGER", nullable: false),
                    side = table.Column<uint>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "varchar(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_destructible", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: WorldDestructibleEntry.TableName);
        }
    }
}
