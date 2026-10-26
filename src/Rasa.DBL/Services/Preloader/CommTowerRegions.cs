using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Brann Communications Tower's levels and its control room (adv_arieki_torden_incline_commtower,
    /// 2085; Comm_tower_regions).
    ///
    /// The tower is a Brann facility inside the hill, with the round control room
    /// (arch_brann_tower_comm_room, x -37 to 18, z 13 to 69, floor 402) alone on the summit above it.
    /// "The control room is accessible only by teleporter and is at the far reaches of the facility";
    /// "get up to the top control room". The facility's floors come in a 16 m rhythm, each level a
    /// floor and a half-floor 8 m above it: 225 and 233, 241 and 249 (the way in: the instance puts
    /// you at (98.5, 246.9, -84.1)), 257 and 265, 273 and 281.
    ///
    /// - Radio Control Room (region 4: the Incline sky, its own music and minimap) is the control room on the summit: its seeded rectangle is that room's footprint.
    ///   But the rectangle was underground-only and at any height, and the summit is in the open, so
    ///   it named the floors beneath the room instead. It now stands on the room, 396 to 430, at any
    ///   level.
    /// - Levels 1 to 4 (regions 6 to 9) are the four levels from the bottom: 220 to 237, 237 to 253,
    ///   253 to 269 and 269 to 295. Levels 1 to 3 have seeded minimap rectangles at any height, so
    ///   Level 3 (the highest priority of the three) won wherever they crossed; each keeps its
    ///   rectangle with its level's height, at any level. Level 2's rectangle leaves out the way in
    ///   and the two west rooms (x -116 to -68, floors 240 and 248), which are on its floors; it gets
    ///   two more boxes for them. Level 4, which has no minimap, gets a box over the top floors.
    /// - Region 10 (the same interior sky and environment map, no name) is the whole facility, 218
    ///   to 295, under the levels' names; Region 11 (Molder.sky) is the editor's base region.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded and
    /// no box of that region is at that spot; recut rows change only while they are as seeded.
    /// Down puts every seeded row back and takes the new ones out.
    /// </summary>
    public static class CommTowerRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded rectangle as it is (underground-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, string Comment, Volume Box);

        public const uint MapContextId = 2085;
        public const uint RadioControlRoomRegion = 4, LevelOneRegion = 6, LevelTwoRegion = 7, LevelThreeRegion = 8, LevelFourRegion = 9,
            InteriorRegion = 10;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "arieki_torden_incline_commtower - ";
        private const string Seeded = Map + "Brann Communications Tower: Le";

        public static readonly Volume[] Volumes =
        {
            Of(LevelTwoRegion, 30, 132, -96, -72, 237, 253, Map + "Level 2 (the way in)"),
            Of(LevelTwoRegion, -118, -80, -30, 122, 237, 253, Map + "Level 2 (west rooms)"),
            Of(LevelFourRegion, -92, 70, -76, 48, 269, 295, Map + "Level 4 (floors 273 and 281)"),
            Of(InteriorRegion, -150, 135, -100, 225, 218, 295, Map + "interior (the whole facility)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(300, Map + "Radio Control Room", Rect(RadioControlRoomRegion, -10, 41, 32.5, 29.5, 396, 430, Map + "Radio Control Room (summit control room)")),
            new(301, Seeded, Rect(LevelOneRegion, 4, 77, 102.5, 172.5, 220, 237, Map + "Level 1 (floors 225 and 233)")),
            new(302, Seeded, Rect(LevelTwoRegion, 26, 22, 106.5, 95, 237, 253, Map + "Level 2 (floors 241 and 249)")),
            new(303, Seeded, Rect(LevelThreeRegion, -58, 31, 106.5, 114, 253, 269, Map + "Level 3 (floors 257 and 265)")),
        };

        private static Volume Rect(uint region, double x, double z, double halfX, double halfZ, double floor, double roof, string comment) =>
            new(region, x, floor, z, halfX, halfZ, floor, roof, comment);

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {AnyLevel}, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {UndergroundOnly};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET min_y = -2000, max_y = 2000, underground = {UndergroundOnly}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND min_y = {N(r.Box.MinY)} AND max_y = {N(r.Box.MaxY)};";

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
