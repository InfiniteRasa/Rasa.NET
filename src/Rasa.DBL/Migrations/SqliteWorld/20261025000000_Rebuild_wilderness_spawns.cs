using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Concordia Wilderness gets areas, as the other zones did in Add_spawn_areas and
    /// Add_world_spawn_areas: the 96 hostile pools of the fork's hand-placed table go, and the 75
    /// of WildernessSpawnpoolPreloader (580001-580075) take their place - the Bane camps mined from
    /// the client's map placements, the fork's other Bane groups as squads, its nests and animal
    /// grounds as packs, and its bosses and named overseers one to a pool. See the preloader for
    /// the rules.
    ///
    /// The table's four test groups go with them: Test Npc 5, 7, 9 and 10 with a Hominis Machina and
    /// red-armour humans on the friendly side, 9 to 24 on a point, two of them in the middle of Lower
    /// Eloh Creek's Bane territory. Its other friendly pools (vendors, mission givers, the Forean and AFS
    /// fighters) are untouched.
    ///
    /// Down deletes 580001-580075 - closed at both ends, since spawnpool is an identity column and a
    /// pool made at runtime is allocated past the block - and puts the 100 back as
    /// SpawnpoolPreloader has them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Rebuild_wilderness_spawns : Migration
    {
        private const uint MapContextId = 1220;
        private const uint IdMin = 580001;
        private const uint IdMax = 580075;

        /// <summary>
        /// The fork's pools on 1220 that go: the 96 with a creature of faction 0 in them, and its four
        /// test groups (59, 71, 115, 134).
        /// </summary>
        private static readonly uint[] Removed =
        {
            1, 2, 3, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 52,
            54, 55, 56, 57, 58, 59, 60, 61, 69, 70, 71, 73, 74, 75, 76, 77,
            79, 80, 81, 82, 83, 85, 88, 91, 94, 96, 97, 98, 99, 102, 103, 104,
            105, 108, 109, 110, 111, 112, 115, 118, 121, 122, 123, 124, 125, 126, 127, 128,
            129, 130, 131, 132, 133, 134, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146,
            147, 148, 151, 152, 153, 154, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165,
            166, 167, 168, 169
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from {SpawnPoolEntry.TableName} where map_context_id = {MapContextId} and id in ({string.Join(", ", Removed)});");

            new WildernessSpawnpoolPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from {SpawnPoolEntry.TableName} where id between {IdMin} and {IdMax};");

            new SpawnpoolPreloader().Preload(migrationBuilder, new HashSet<uint>(Removed));
        }
    }
}
