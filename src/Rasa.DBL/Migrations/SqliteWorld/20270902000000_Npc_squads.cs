using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The NPC squads' tables (NpcSquads): squad types and their members, the routes squads walk
    /// and their points, and the spawns that send them. Empty: squads are put in by GMs (.squad)
    /// or by later migrations.
    ///
    /// Down drops them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270902000000_Npc_squads")]
    public class Npc_squads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: NpcSquadTypeEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    formation = table.Column<byte>(type: "INTEGER", nullable: false),
                    spacing = table.Column<double>(type: "REAL", nullable: false),
                    engage_range = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_npc_squad_type", x => x.id));

            migrationBuilder.CreateTable(
                name: NpcSquadMemberEntry.TableName,
                columns: table => new
                {
                    type_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    slot = table.Column<byte>(type: "INTEGER", nullable: false),
                    creature_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    min_count = table.Column<byte>(type: "INTEGER", nullable: false),
                    max_count = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_npc_squad_member", x => new { x.type_id, x.slot }));

            migrationBuilder.CreateTable(
                name: NpcSquadPathEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    mode = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_npc_squad_path", x => x.id));

            migrationBuilder.CreateTable(
                name: NpcSquadPathPointEntry.TableName,
                columns: table => new
                {
                    path_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    step = table.Column<uint>(type: "INTEGER", nullable: false),
                    pos_x = table.Column<double>(type: "REAL", nullable: false),
                    pos_y = table.Column<double>(type: "REAL", nullable: false),
                    pos_z = table.Column<double>(type: "REAL", nullable: false),
                    pause_ms = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_npc_squad_path_point", x => new { x.path_id, x.step }));

            migrationBuilder.CreateTable(
                name: NpcSquadSpawnEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    type_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    path_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    pos_x = table.Column<double>(type: "REAL", nullable: false),
                    pos_y = table.Column<double>(type: "REAL", nullable: false),
                    pos_z = table.Column<double>(type: "REAL", nullable: false),
                    rotation = table.Column<double>(type: "REAL", nullable: false),
                    respawn_seconds = table.Column<uint>(type: "INTEGER", nullable: false),
                    enabled = table.Column<byte>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_npc_squad_spawn", x => x.id));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: NpcSquadSpawnEntry.TableName);
            migrationBuilder.DropTable(name: NpcSquadPathPointEntry.TableName);
            migrationBuilder.DropTable(name: NpcSquadPathEntry.TableName);
            migrationBuilder.DropTable(name: NpcSquadMemberEntry.TableName);
            migrationBuilder.DropTable(name: NpcSquadTypeEntry.TableName);
        }
    }
}
