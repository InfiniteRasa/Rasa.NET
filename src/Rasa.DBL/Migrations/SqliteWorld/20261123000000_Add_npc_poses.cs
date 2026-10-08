using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// spawnpool_pose: how a pool's creatures stand at their post (the game server's NpcPose) -
    /// at ease on the spot, with the weapon out, crouched, or one of the client's five ambient
    /// poses. A pool with no row has no pose, so every existing pool is unchanged.
    ///
    /// A new table rather than a column on spawnpool, as Add_promo_vendors did for creature and
    /// vendor: the preloaders of earlier migrations insert spawnpool by its entity's columns.
    ///
    /// And the first four NPCs placed with a pose, at the Proving Grounds' refugee base
    /// (BootcampPostedNpcPreloaders): a Forean Warrior standing guard with his spear, an
    /// Infantryman with his arms at his sides, and two Infantrymen holding rifles.
    ///
    /// Down drops the table and deletes the closed id range those rows occupy.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_npc_poses : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new BootcampPostedNpcCreaturePreloader(),
            new BootcampPostedNpcAppearancePreloader(),
            new BootcampPostedNpcStatsPreloader(),
            new BootcampPostedNpcSpawnpoolPreloader(),
            new BootcampPostedNpcPosePreloader()
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
                name: SpawnPoolPoseEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    pose = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool_pose", x => x.id);
                });

            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: SpawnPoolPoseEntry.TableName);

            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id between {BootcampPostedNpcs.FirstId} and {BootcampPostedNpcs.LastId};");
            }
        }
    }
}
