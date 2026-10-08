using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The Proving Grounds' rifle guards and Training Officer as the client's ambient figures for
    /// them (BootcampAmbientGuards): eight UsableStatelessNPCMaleGuard, at ease with the rifle
    /// across the chest, on the spots of the eight rifleman pools, and the drill sergeant
    /// UsableStatelessNPCDrillSGTV01, who paces by his own animation, on the officer's beat.
    /// Takes out the nine pools, the riflemen's poses and the officer's patrol; the creature rows
    /// stay. No table changes.
    ///
    /// Down deletes the nine figures and puts the pools, poses and patrol back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_bootcamp_ambient_guards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var pools = string.Join(", ", BootcampAmbientGuards.PoolIds);

            migrationBuilder.Sql($"delete from {SpawnPoolPatrolEntry.TableName} where pool_id = {BootcampTrainingOfficer.PoolId};");
            migrationBuilder.Sql($"delete from {SpawnPoolPoseEntry.TableName} where id in ({pools});");
            migrationBuilder.Sql($"delete from {SpawnPoolEntry.TableName} where id in ({pools});");

            new BootcampAmbientGuardPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from {AmbientNpcEntry.TableName} where id between {BootcampAmbientGuards.FirstId} and {BootcampAmbientGuards.LastId};");

            var posted = new[] { BootcampPostedNpcs.RiflemanWestPoolId, BootcampPostedNpcs.RiflemanEastPoolId };

            new BootcampPostedNpcSpawnpoolPreloader().Preload(migrationBuilder, posted);
            new BootcampPostedNpcPosePreloader().Preload(migrationBuilder, posted);
            new BootcampGarrisonSpawnpoolPreloader().Preload(migrationBuilder);
            new BootcampGarrisonPosePreloader().Preload(migrationBuilder);
            new BootcampTrainingOfficerSpawnpoolPreloader().Preload(migrationBuilder);
            new BootcampTrainingOfficerPatrolPreloader().Preload(migrationBuilder);
        }
    }
}
