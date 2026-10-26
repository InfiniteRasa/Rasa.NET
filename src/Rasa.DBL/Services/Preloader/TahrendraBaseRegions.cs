using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Tahrendra Base's rooms, prison blocks and their levels (adv_arieki_torden_mires_tahrendrabase,
    /// 2125; Tahrendra_base_regions).
    ///
    /// The base is a Brann prison laid out east to west along one corridor at floor 113 (121 for the
    /// rooms on its galleries). Its fifteen named regions only set the name (the two prison-block
    /// security stations also the command-room ambience); a region label or the props place each:
    ///
    /// - Prisoner Shipping &amp; Supply (2): the way in. The instance puts you at (196, 120.5, 10.5);
    ///   past the entrance is the four-quarter hall x 76 to 172 (Bane crate and barrel stacks, cameras
    ///   on the walls), then the corridor to the check point's door at x 56.
    /// - Prisoner Check Point (3): the two rooms x -24 to 56 with their control rooms between them;
    ///   the "Security Check Point" label (16, 113, 11) is at that door.
    /// - Barracks (4): the two dormitories with beds, sleep pods, toilets and sinks, each under a
    ///   "Guard Barracks" label: x 20 to 52, z 40 to 64 and x 36 to 68, z -40 to -16.
    /// - Mess Hall (5): the four quarter rooms x -7 to 55, z -104 to -40, with the food dispensers,
    ///   tables and command chairs, under the "Guard Lounge" label.
    /// - Wormhole Security Station (6): the room over the lower block, x -100 to -52, z 24 to 64,
    ///   floor 121, under its label; Wormhole (9): the room with the wormhole in it, x -204 to -124;
    ///   Wormhole Power Plant (10): the computer quarters x -264 to -204 beyond it.
    /// - Upper Prison Block Security Station (7): the console room on the upper block's top level,
    ///   x -152 to -112, floor 144, under the "Upper Block Security" label; Lower Prison Block
    ///   Security Station (8): the console room with the stamper on the lower block's bottom level,
    ///   x -24 to 16, floor 80, under "Lower Block Security". These three are presumably the mission's "3
    ///   main security rooms".
    /// - The prison blocks are three levels of cell corridors each, both entered from the main floor
    ///   at 113: the upper block (z -104 to -16) climbs by ramps to 129 and 144, the lower block
    ///   (z 16 to 112) goes down to 97 and 81. Level One is the shared floor at 113 and the levels
    ///   count away from it, Two and Three up in the upper block and down in the lower: the block's
    ///   own security station is on its Level Three, as far from the entrance as the block goes. The
    ///   map's minimap layers say the same: one layer (region 17) covers both blocks at once, and
    ///   each block has two more of its own, the wider of which takes in its security station.
    ///
    /// The label circles of Barracks, Wormhole Security Station and Wormhole (40 m, at any height)
    /// ran across the rooms round them and the levels above and below; the Wormhole Power Plant
    /// circle (200 m) named half the base. They are recut to their rooms. The minimap layers
    /// (regions 17, 19, 20, 22, 23 and 24) stood at any height, so the topmost won on every level;
    /// they keep their rectangles but get the height of their level: 17 the blocks' Level One
    /// (106 to 119), 19 and 20 the upper block's Levels Two and Three, 22 and 23 the lower block's,
    /// and 24, with the command ambience, the Wormhole Security Station room above (119 to 142).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded and
    /// no box of that region is at that spot; recut rows change only while they are as seeded.
    /// Down puts every seeded row back and takes the new ones out.
    /// </summary>
    public static class TahrendraBaseRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded row as it is (circle or rectangle) and the volume it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ,
            double MinY, double MaxY, byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2125;
        public const uint ShippingRegion = 2, CheckPointRegion = 3, BarracksRegion = 4, MessHallRegion = 5, WormholeSecurityRegion = 6,
            UpperSecurityRegion = 7, LowerSecurityRegion = 8, WormholeRegion = 9, PowerPlantRegion = 10,
            UpperLevelOneRegion = 11, UpperLevelTwoRegion = 12, UpperLevelThreeRegion = 13,
            LowerLevelOneRegion = 14, LowerLevelTwoRegion = 15, LowerLevelThreeRegion = 16;
        public const byte Circle = 1, Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_mires_tahrendrabase - ";

        public static readonly Volume[] Volumes =
        {
            Of(ShippingRegion, 172, 206, 2, 54, 108, 130, Map + "Prisoner Shipping & Supply (entrance)"),
            Of(ShippingRegion, 76, 172, -4, 60, 107, 130, Map + "Prisoner Shipping & Supply (hall)"),
            Of(ShippingRegion, 56, 76, 3, 21, 110, 120, Map + "Prisoner Shipping & Supply (corridor to the check point)"),
            Of(CheckPointRegion, -24, 56, -13, 37, 108, 130, Map + "Prisoner Check Point (two rooms)"),
            Of(BarracksRegion, 34, 70, -42, -14, 117, 138, Map + "Barracks (south dormitory)"),
            Of(MessHallRegion, -8, 56, -105, -40, 117, 140, Map + "Mess Hall (quarter rooms)"),
            Of(UpperSecurityRegion, -154, -108, -94, -34, 140, 165, Map + "Upper Prison Block Security Station (console room)"),
            Of(LowerSecurityRegion, -26, 18, 44, 100, 76, 101, Map + "Lower Prison Block Security Station (console room)"),
            Of(UpperLevelOneRegion, -110, -28, -100, -16, 109, 122, Map + "Upper Prison Block Level One (floor 113)"),
            Of(UpperLevelTwoRegion, -116, -20, -105, -24, 124, 139, Map + "Upper Prison Block Level Two (floor 129)"),
            Of(UpperLevelThreeRegion, -108, -28, -105, -12, 139, 160, Map + "Upper Prison Block Level Three (floor 145)"),
            Of(LowerLevelOneRegion, -110, -28, 22, 112, 106, 119, Map + "Lower Prison Block Level One (floor 113)"),
            Of(LowerLevelTwoRegion, -118, -20, 16, 112, 92, 106, Map + "Lower Prison Block Level Two (floor 97)"),
            Of(LowerLevelThreeRegion, -110, -26, 16, 106, 76, 92, Map + "Lower Prison Block Level Three (floor 81)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20172, BarracksRegion, Circle, 35.5, 120.5, 51.9, 40, 0, 0, -2000, 2000, 0, Map + "Barracks",
                Of(BarracksRegion, 18, 54, 38, 66, 117, 138, Map + "Barracks (north dormitory)")),
            new(20173, WormholeSecurityRegion, Circle, -76.0, 120.0, 47.0, 40, 0, 0, -2000, 2000, 0, Map + "Wormhole Security Station",
                Of(WormholeSecurityRegion, -105, -47, 23, 66, 119, 140, Map + "Wormhole Security Station (room)")),
            new(20174, WormholeRegion, Circle, -165.6, 115.5, 11.3, 40, 0, 0, -2000, 2000, 0, Map + "Wormhole",
                Of(WormholeRegion, -204, -123, -25, 49, 108, 140, Map + "Wormhole (room)")),
            new(339, PowerPlantRegion, Circle, -231.8, 112.5, -27.9, 200, 0, 0, -2000, 2000, 2, Map + "Wormhole Power Plant",
                Of(PowerPlantRegion, -264, -204, -60, 20, 108, 130, Map + "Wormhole Power Plant (computer quarters)")),
            Layer(333, 17, -74.0, 117.8, 8.0, 54.5, 108.5, 106, 119),
            Layer(334, 19, -66.0, 131.6, -52.0, 44.0, 49.0, 122, 139),
            Layer(335, 20, -93.0, 136.4, -56.0, 68.5, 48.5, 139, 170),
            Layer(336, 22, -70.0, 109.3, 62.0, 44.0, 47.0, 92, 106),
            Layer(337, 23, -42.0, 109.0, 64.0, 68.0, 45.0, 70, 92),
            Layer(338, 24, -76.0, 109.3, 48.0, 30.5, 25.0, 119, 142),
        };

        /// <summary>A minimap layer's seeded rectangle, kept but given its level's height.</summary>
        private static Recut Layer(uint row, uint region, double x, double y, double z, double halfX, double halfZ, double floor, double roof)
        {
            var comment = Map + $"region {region} minimap";
            return new(row, region, Box, x, y, z, 0, halfX, halfZ, -2000, 2000, 0, comment,
                new(region, x, y, z, halfX, halfZ, floor, roof, comment));
        }

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
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {AnyLevel}, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 AND min_y = {N(r.MinY)} AND max_y = {N(r.MaxY)};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
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
