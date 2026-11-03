using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Rivasa Atta Colony's chambers by their light (adv_arieki_ligo_thunderhead_rivasaattacolony,
    /// 2112; Rivasa_atta_colony_regions).
    ///
    /// The colony is four great caverns under cavern roofs (terra_arieki_atta_cavern_roof) joined
    /// by cave rooms and passages, each lit by its own colour of Atta cavern lights
    /// (terra_arieki_atta_cavern_light*): the cavern by the way in and the rooms between are blue,
    /// the Rivasa Burial Chamber cavern green, the Queen's Chamber yellow and the Rivasa Food
    /// Storage cavern blue. The map's own sky is sky_arieki_atta_interior_cave_blueV03.
    ///
    /// - The three named chambers had 177 to 200 m label circles, surface-only, that reached far
    ///   past their caverns (the Queen's took the passage from the Food Storage and the room with
    ///   the Bane pipeline south of it) and missed their floors where the navmesh puts them under
    ///   the ground. Each becomes a box of its cavern roof: the Burial Chamber x -32 to 208,
    ///   z -164 to 58, from the pit at -46 to 110 (under the entrance cavern, whose floor over it
    ///   is at 116); the Queen's Chamber, with the throne and the historical obelisks, x -170 to
    ///   41, z 288 to 485; the Food Storage x -493 to -263, z 156 to 387.
    /// - Region 7 (sky_arieki_atta_interior_cave_greenV03, MUSIC_THUNDERHEAD_04,
    ///   REGION_ATTA_INTERNAL_LARGE_CAVE_ROOM; priority 6, no name) had no volume. It is the green
    ///   light: the Burial Chamber cavern, lit by 18 green lights (and three blue at its west
    ///   door), over the Burial Chamber's own region (5, priority 4) whose name stays; and the
    ///   passage from the Food Storage to the Queen's Chamber door, 12 more green lights
    ///   (x -233 to -147, z 272 to 401).
    /// - Bane Incursion (region 10: sky_bane_interior_default, env_bane_interior_default,
    ///   MUSIC_THUNDERHEAD_04 and the name) had no volume: the map has a "Bane Incursion" point
    ///   label (-180, 108, 122) but no region label. It is the Bane chunnel cut into the colony
    ///   there (arch_bane_industrial_chunnel_jamb, two door bulkheads, a catwalk and pipes),
    ///   x -207 to -178, z 91 to 154.
    ///
    /// Not placed: regions 4 (the blue sky and the large cave room ambience), 6 (the Ojasa hive's
    /// sky), 11 and 12 (the Burial Chamber's blueV02 sky alone) and 1 (the Bane interior sky
    /// alone), which nothing ties to one room; region 2 is the editor's base region.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; the circles change only while they are as
    /// seeded. Down puts them back and takes the new rows out.
    /// </summary>
    public static class RivasaAttaColonyRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded label circle (surface-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 2112;
        public const uint BurialRegion = 5, GreenRegion = 7, QueenRegion = 8, FoodStorageRegion = 9, IncursionRegion = 10;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_thunderhead_rivasaattacolony - ";

        public static readonly Volume[] Volumes =
        {
            Of(GreenRegion, -32, 208, -164, 58, -60, 110, Map + "region 7 (the Burial Chamber's green light)"),
            Of(GreenRegion, -233, -147, 272, 401, 45, 95, Map + "region 7 (the green passage to the Queen's Chamber)"),
            Of(IncursionRegion, -207, -178, 91, 154, 100, 130, Map + "Bane Incursion (the Bane chunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(330, BurialRegion, 110.2, 44.3, -40.5, 200, Map + "Rivasa Burial Chamber",
                Of(BurialRegion, -32, 208, -164, 58, -60, 110, Map + "Rivasa Burial Chamber (its cavern)")),
            new(331, QueenRegion, -62.6, 64.6, 395.2, 177, Map + "Queen's Chamber",
                Of(QueenRegion, -170, 41, 288, 485, 25, 112, Map + "Queen's Chamber (its cavern)")),
            new(332, FoodStorageRegion, -397.6, 41.8, 279.4, 177, Map + "Rivasa Food Storage",
                Of(FoodStorageRegion, -493, -263, 156, 387, -20, 112, Map + "Rivasa Food Storage (its cavern)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {AnyLevel}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {SurfaceOnly};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
