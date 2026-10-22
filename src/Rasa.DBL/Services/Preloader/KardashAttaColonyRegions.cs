using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Kardash Atta Colony's Queen's Chamber, Outer Junction and Eloh Cache
    /// (adv_arieki_torden_plains_attacolony, 1773; Kardash_atta_colony_regions).
    ///
    /// The colony is a set of Atta caverns under Torden Plains joined by cavern tunnels. Three
    /// of its named regions had no volume, their labels being named otherwise or not at all:
    ///
    /// - Queen's Chamber (7: sky_arieki_atta_interior_cave_queeneggroom, MUSIC_ARIEKI_PLAINS_04):
    ///   the map's label for it reads "Kardash Queen's Chamber" (-348.5, 42.2, 222.5), so no
    ///   circle was seeded. It is the north-west cavern with Atta Queen Kardash's throne
    ///   (arch_atta_throne_v01 at (-349, 41, 239)), under its roof (terra_arieki_atta_cavern_roof_v01,
    ///   x -444 to -193, z 63 to 333), floor y 39 to 47: the box of that roof, y 30 to 110, below
    ///   the Shining Cavern's floor (y 200) to the east.
    /// - Outer Junction (8, the Atta corridor sky): the cavern tunnel down the colony's west edge
    ///   between the Queen's Chamber and the Royal Nest, the one tunnel with a chamber in it (two
    ///   terra_arieki_cavern_room_01_48m at (-352, 40, -24), x -375 to -329, z -73 to 25) where
    ///   the two nests' ways meet: x -397 to -308, z -121 to 63, up to the Queen's Chamber.
    ///   The colony's other tunnels end at a teleporter or a research outpost.
    /// - Eloh Cache (15, sky_eloh_interior_default): the map's "Eloh Retreat" label
    ///   (-111.5, 191.8, 322.6) stands in its one Eloh room (arch_eloh_corridor_room_v02 at
    ///   (-68, 192, 324), its four obelisks and logos signs), reached from the north cavern by an
    ///   Eloh door (-4, 192, 324) and two offset corridors: x -147 to 3, z 286 to 362, y 192.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class KardashAttaColonyRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1773;
        public const uint QueensChamberRegion = 7, OuterJunctionRegion = 8, ElohCacheRegion = 15;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_plains_attacolony - ";

        public static readonly Volume[] Volumes =
        {
            Of(QueensChamberRegion, -444, -193, 63, 333, 30, 110, Map + "Queen's Chamber (throne cavern)"),
            Of(OuterJunctionRegion, -398, -307, -122, 63, 35, 80, Map + "Outer Junction (west tunnel)"),
            Of(ElohCacheRegion, -148, 4, 285, 363, 186, 216, Map + "Eloh Cache (Eloh room)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
