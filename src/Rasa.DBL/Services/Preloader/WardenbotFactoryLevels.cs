using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Wardenbot Factory's three levels and the ramps between them
    /// (adv_arieki_torden_incline_wardenbotfactory, 2111; Wardenbot_factory_levels).
    ///
    /// The factory is built of wardenbot corridor pieces on three floors, with two domed rooms:
    /// Raw Material Handling (arch_wardenbot_corridor_room_01 at (-424, 79, -84), floor y 72,
    /// already given its room by Region_room_volumes) and the Bot Programming dome (the same
    /// room at (-216, 39, -44), floor y 32), and, east past a lava-walled passage, the Raksha
    /// Robotics control point's dome.
    ///
    /// - Level 1 (regions 5 and 11, priorities 6 and 7: its name, and the WBRoom sky and
    ///   env map with it): the top floor, y 95 to 103. The entrance from Torden Incline (the
    ///   instance door jambs at (-272, 103, 92)) and its hub, the corridors west along the north
    ///   of Raw Material Handling to the ramp at (-456, 79, 132) and down to its north door
    ///   (-424, 79, 64). Three boxes, kept off the dome.
    /// - Level 2 Access Ramp (region 9, priority 8): from Raw Material Handling's south door
    ///   (-424, 79, -88) three 32 m ramps switch back south and down to y 40 (x -445 to -419,
    ///   z -169 to -83): the long corridor south of the dome on the minimap.
    /// - Level 2 (region 6, priority 9): the corridors under Raw Material Handling at y 39,
    ///   with the side ramps down to y 24 (x -500 to -359, z -92 to 72), beside the Bot
    ///   Programming dome.
    /// - Level 3 Access Ramp (region 10, priority 10): east of the Bot Programming dome, three
    ///   ramps down from y 51 to 0 (x -229 to -203, z -129 to -39).
    /// - Level 3 (region 7, priority 11): the bottom corridors under the Bot Programming dome
    ///   at y 0 (x -348 to -196, z -53 to 24).
    ///
    /// The levels' regions turn on little but the name, and stand over the rooms' in
    /// priority, so their boxes keep to the corridors; the two domes and the control point's
    /// (Main Unit Processing and the Central Dispatch Unit, still without volumes) are left to
    /// their own regions.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is
    /// loaded and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class WardenbotFactoryLevels
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2111;
        public const uint Level1NameRegion = 5, Level1Region = 11, Level2AccessRampRegion = 9, Level2Region = 6, Level3AccessRampRegion = 10, Level3Region = 7;
        public const byte Box = 2;

        private const string Map = "arieki_torden_incline_wardenbotfactory - ";

        /// <summary>Level 1's boxes; regions 5 and 11 both get each.</summary>
        private static readonly (double West, double East, double South, double North, double Floor, double Roof, string Part)[] Level1 =
        {
            (-305, -243, 14, 98, 93, 118, "entrance and hub"),
            (-500, -305, 60, 145, 78, 110, "north corridors"),
            (-372, -305, 38, 60, 93, 110, "corridors at the dome's north-east"),
        };

        public static readonly Volume[] Volumes =
            Level1.SelectMany(b => new[]
            {
                Of(Level1NameRegion, b.West, b.East, b.South, b.North, b.Floor, b.Roof, Map + "Level 1 (" + b.Part + ")"),
                Of(Level1Region, b.West, b.East, b.South, b.North, b.Floor, b.Roof, Map + "Level 1 (" + b.Part + ")"),
            }).Concat(new[]
            {
                Of(Level2AccessRampRegion, -446, -418, -170, -83, 38, 92, Map + "Level 2 Access Ramp (south of Raw Material Handling)"),
                Of(Level2Region, -500, -359, -92, 72, 20, 70, Map + "Level 2 (corridors under Raw Material Handling)"),
                Of(Level3AccessRampRegion, -230, -202, -130, -39, -2, 52, Map + "Level 3 Access Ramp (east of the Bot Programming dome)"),
                Of(Level3Region, -349, -195, -53, 25, -2, 20, Map + "Level 3 (corridors under the Bot Programming dome)"),
            }).ToArray();

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes)
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
