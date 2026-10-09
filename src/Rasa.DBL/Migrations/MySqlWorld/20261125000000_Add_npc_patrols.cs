using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// spawnpool_patrol: the steps of a pool's patrol - a point its creature walks to in a
    /// straight line from the step before, the way it turns to face there, and how long it
    /// stands - walked round and round (the game server's Patrols). A pool with no row has no
    /// patrol, so every existing pool is unchanged.
    ///
    /// A new table rather than columns on spawnpool, as Add_npc_poses did for a pool's pose: the
    /// preloaders of earlier migrations insert spawnpool by its entity's columns.
    ///
    /// And the first NPC placed with one, at the Proving Grounds' refugee base
    /// (BootcampTrainingOfficerPreloaders): a Training Officer in an officer's cap who walks an
    /// 8 m line, stopping twice on the way north to turn a quarter turn and stand 4 s.
    ///
    /// Down drops the table and deletes the one id those rows have.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_npc_patrols : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new BootcampTrainingOfficerCreaturePreloader(),
            new BootcampTrainingOfficerAppearancePreloader(),
            new BootcampTrainingOfficerStatsPreloader(),
            new BootcampTrainingOfficerSpawnpoolPreloader(),
            new BootcampTrainingOfficerPatrolPreloader()
        };

        private static readonly string[] Tables =
        {
            SpawnPoolEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureAppearanceEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: SpawnPoolPatrolEntry.TableName,
                columns: table => new
                {
                    pool_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    step = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    facing = table.Column<double>(type: "double", nullable: true),
                    pause_ms = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool_patrol", x => new { x.pool_id, x.step });
                });

            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: SpawnPoolPatrolEntry.TableName);

            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id = {BootcampTrainingOfficer.CreatureId};");
            }
        }
    }
}
