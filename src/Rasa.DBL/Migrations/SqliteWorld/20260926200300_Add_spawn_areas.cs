using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Spawn pools become areas, and Concordia Divide gets its Bane camps and Xanx nests.
    ///
    /// The client's map files place the props the level designers built each camp from - sandbag
    /// runs, pillboxes, mortar bases, trench pieces, walls, barracks - and each Xanx nest. Clustered
    /// per map (spawn_areas.csv, cluster_props.py) they give a centre, a footprint and a size for
    /// every camp in the world. Wilderness, the one map with hand-placed pools, checks the method:
    /// the pools inside its Bane camps are all Thrax squads.
    ///
    /// spawnpool gains a radius column. A pool with a radius is an area: its creatures stand on
    /// walkable points anywhere inside it rather than on one point with two units of scatter.
    /// Zero keeps the old behaviour, so every existing pool is unchanged.
    ///
    /// Divide is the first zone placed, to be walked before the other fifteen: 79 pools on four
    /// maps - 36 posts, 16 camps, 9 outposts, 14 squad knots inside the three bases (Bane Forward
    /// Command, Fuel Bore Cavern, Torcastra Gateway) and 4 Xanx nests - and 9 creature rows for the
    /// roles they use. Positions and radii are the client's; class ids and action rows are the ones
    /// the fork's existing rows of each family use; levels step down from the zone band; health is
    /// the player's own base health at that level times the tier's share, pinned by the client's
    /// POWERLEVEL ladder having a player at tier 5. divide_spawns.csv and divide_creatures.csv
    /// carry every row with its source.
    ///
    /// Down deletes the closed id range these rows occupy and drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_spawn_areas : Migration
    {
        private const uint IdMin = 530001;
        private const uint IdMax = 530999;

        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new DivideCreaturePreloader(),
            new DivideCreatureStatsPreloader(),
            new DivideSpawnpoolPreloader()
        };

        private static readonly string[] Tables =
        {
            SpawnPoolEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "radius",
                table: SpawnPoolEntry.TableName,
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id between {IdMin} and {IdMax};");
            }

            migrationBuilder.DropColumn(
                name: "radius",
                table: SpawnPoolEntry.TableName);
        }
    }
}
