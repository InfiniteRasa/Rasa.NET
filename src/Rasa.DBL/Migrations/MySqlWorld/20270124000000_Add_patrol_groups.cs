using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// spawnpool_patrol_group: the pools whose group walks its patrol with an acting leader -
    /// one of its followers leads while it has no leader of its own, and a pool with an empty
    /// first slot is always led by one of them - and whether its leader comes back by the
    /// pool's top-up or by its own respawn time after its death (SpawnPoolPatrolGroupEntry).
    /// No rows: no pool has one yet.
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270124000000_Add_patrol_groups")]
    public class Add_patrol_groups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: SpawnPoolPatrolGroupEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    acting_leader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    leader_top_up = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool_patrol_group", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: SpawnPoolPatrolGroupEntry.TableName);
        }
    }
}
