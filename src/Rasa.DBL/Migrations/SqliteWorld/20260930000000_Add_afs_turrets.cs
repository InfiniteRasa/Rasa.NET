using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// AFS turrets on the mounts the client's maps build for them: 127 emplacements on 24 maps,
    /// 74 standard (Emplacement_AFS_Turret_Standard on ArchHumanBaseTurretPlatformV01) and 53 light
    /// (Emplacement_AFS_Turret_Mini on a tripod or track base), with 27 creature rows - one per
    /// turret and zone - their guns, stats and weapon appearances.
    ///
    /// Of the 183 mounts in the maps, 56 get no turret: 24 belong to a control point (in the same
    /// AFS footprint as the control point marker or one of its "(Control Point)" services, or within
    /// 120 m of one) and wait for control point ownership; 21 carry a destroyed turret the designers
    /// placed; 3 are Brann turrets, whose map prop is already the emplacement's mesh; 4 are on the
    /// Wargame maps; 1 is a duplicate; and 3 would stand within the 18 m scan of a hostile camp or
    /// hand-placed pool (Wilderness 2 and 40, Palisades 531497) and fight it for as long as the
    /// server runs.
    ///
    /// Three open-country packs from Add_region_spawns stand inside a turret's scan - they were
    /// placed before the turrets were - and become scripted (mode 2): 540016, 540068, 540098.
    ///
    /// gen_turrets.py made the rows; turret_mounts.csv has every mount with its reason. Down deletes
    /// the closed id ranges and gives the three packs back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_afs_turrets : Migration
    {
        private const uint IdMin = 550001;
        private const uint IdMax = 550999;
        private const uint ActionIdMin = 59001;
        private const uint ActionIdMax = 59999;
        private const string QuietedPacks = "540016, 540068, 540098";

        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new TurretCreatureActionPreloader(),
            new TurretCreaturePreloader(),
            new TurretCreatureStatsPreloader(),
            new TurretCreatureAppearancePreloader(),
            new TurretSpawnpoolPreloader()
        };

        private static readonly string[] Tables =
        {
            SpawnPoolEntry.TableName,
            CreatureAppearanceEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }

            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set mode = 2 where id in ({QuietedPacks});");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set mode = 0 where id in ({QuietedPacks});");

            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id between {IdMin} and {IdMax};");
            }

            migrationBuilder.Sql($"delete from {CreatureActionEntry.TableName} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}
