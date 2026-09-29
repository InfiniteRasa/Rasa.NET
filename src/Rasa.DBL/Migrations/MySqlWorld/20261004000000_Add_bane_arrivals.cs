using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Bane arrival points. spawnpool_arrival (SpawnPoolArrivalEntry): where a pool's creatures
    /// arrive - kind 1 a landing pad or dropship bay the Bane dropship comes down on, kind 2 a Bane
    /// teleporter, named by the map's own entity id, that is switched on while they come through.
    ///
    /// Seeded from the maps (gen_arrivals.py; bane_arrivals.csv has every point and its reason):
    /// 62 of the 73 arrival points - 22 landing pads, 7 dropship bays, 33 teleporters. 13 Bane camps
    /// whose ground comes within 25 m of one arrive there (one of them dormant on a control point);
    /// the rest get 22 squads of their own (Thrax soldiers 2-3, a grenadier 0-1, respawn 90 s),
    /// clustered so a room of teleporters is one squad, with two new creature rows for Wilderness's
    /// Pravus Research. Left out: 8 pads with no deck on the navmesh or indoors, 2 by control points,
    /// the Bootcamp pad (no Bane rows there).
    ///
    /// Down drops the table and deletes the squads (560001-560999).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_bane_arrivals : Migration
    {
        private const uint IdMin = 560001;
        private const uint IdMax = 560999;

        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new BaneSquadCreaturePreloader(),
            new BaneSquadCreatureStatsPreloader(),
            new BaneSquadSpawnpoolPreloader(),
            new SpawnPoolArrivalPreloader()
        };

        private static readonly string[] Tables =
        {
            SpawnPoolEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: SpawnPoolArrivalEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    pool_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    entity_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(96)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool_arrival", x => x.id);
                });

            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: SpawnPoolArrivalEntry.TableName);

            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"delete from {table} where id between {IdMin} and {IdMax};");
            }
        }
    }
}
