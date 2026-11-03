using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Valverde Pools (adv_foreas_valverde_pools, 1304;
    /// 20270723000000_Valverde_pools_region_sweep).
    ///
    /// Southern AFS Listening Post (region 3): the buried human corridor complex at (164..256,
    /// -624..-566), floor 664.6-672.8, flagged underground, holding the South Listening Post POI label,
    /// hospital and waypoint. Two underground-only boxes (hub and west wing); both entrance mouths stay
    /// outdoors. Its minimap rect region 22 (row 40, Molder.sky) is cut to the hub's underground box
    /// under it.
    ///
    /// Eastern AFS Listening Post (region 10): the buried corridor complex at (910..1034, 300..388),
    /// floor 656.6, with the East Listening Post label, hospital, waypoint, trainer and crafting
    /// station. One underground-only box; rect region 23 (row 41) cut to it.
    ///
    /// Region 27 (unnamed Bane interior, Thrax Barracks' music, one priority above Thrax Barracks): the
    /// underground Bane complex beside the barracks: control room v08 with command consoles, sleep-pod
    /// chunnel, service rooms and entry tunnel (306..493, 568..730, floor 684-694). Three underground-
    /// only boxes; rect region 24 (row 42) cut to the main one.
    ///
    /// Retread Caves Entrance (20), Plateau Pass (19), Marshes Pass (18): the three cave stubs ending
    /// at the map links to the Retread Caves, Valverde Plateau and Valverde Marshes (MapLinkPreloader
    /// 30, 29, 27), each a terra_foreas_cavern_entrance plus cavern_instance. Underground-only boxes so
    /// the cave mouths stay outdoors.
    ///
    /// Turpis Refinery Dropship Pad (4): arch_hum_dropship_landingpad_v02 at (147,-607) where link 26
    /// to the Turpis Refinery (wetland refinery) instance stands.
    ///
    /// Lamnas Armory Entrance (13): the Bane tunnel entrance ramp ending at the link to the Lamna
    /// Armory instance (link 28 at -246.6,-288.1). Lamna Armory (29, medium): the machine yard flanking
    /// that ramp.
    ///
    /// Minimap rects: regions 22/23/24 are Molder.sky-only sub-rectangles that put the editor sky over
    /// open ground; each now sits wholly under its facility's region.
    ///
    /// Not placed: region 2 (human underground sky only, no third place), Lake Morass (no label; the
    /// lake is unnamed), regions 1 and 28 (skipped).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ValverdePoolsRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1304;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_pools - ";

        public static readonly Volume[] Volumes =
        {
            Of(3, 198, 256, -612, -566, 660, 682, UndergroundOnly, Map + "Southern AFS Listening Post (corridor hub)"),
            Of(3, 164, 200, -618, -582, 668, 688, UndergroundOnly, Map + "Southern AFS Listening Post (west wing to pad)"),
            Of(10, 910, 1034, 300, 388, 650, 668, UndergroundOnly, Map + "Eastern AFS Listening Post (corridors)"),
            Of(27, 306, 418, 585, 730, 680, 715, UndergroundOnly, Map + "region 27 (Thrax Barracks interior, control room)"),
            Of(27, 418, 493, 568, 622, 680, 715, UndergroundOnly, Map + "region 27 (Thrax Barracks interior, entry tunnel)"),
            Of(27, 418, 436, 662, 682, 680, 715, UndergroundOnly, Map + "region 27 (Thrax Barracks interior, service room)"),
            Of(20, -460, -414, 700, 768, 878, 925, UndergroundOnly, Map + "Retread Caves Entrance (cave to the caves zone)"),
            Of(19, -742, -694, 742, 785, 955, 990, UndergroundOnly, Map + "Plateau Pass (cave to Valverde Plateau)"),
            Of(18, 686, 730, 710, 756, 668, 705, UndergroundOnly, Map + "Marshes Pass (cave to Valverde Marshes)"),
            Of(4, 132, 160, -620, -595, 665, 690, AnyLevel, Map + "Turpis Refinery Dropship Pad (landing pad)"),
            Of(13, -255, -200, -305, -271, 676, 710, AnyLevel, Map + "Lamnas Armory Entrance (Bane tunnel mouth)"),
            Of(29, -265, -175, -350, -230, 670, 740, AnyLevel, Map + "Lamna Armory (Bane machine yard over it)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(40, 22, 2, 221, 0, -586, 0, 39, 25.5, -2000, 2000, 0, "foreas_valverde_pools - region 22 minimap",
                Of(22, 198, 256, -612, -566, 660, 682, UndergroundOnly, Map + "region 22 (south post minimap rect, underground)")),
            new(41, 23, 2, 993, 657.5, 330, 0, 78, 69, -2000, 2000, 0, "foreas_valverde_pools - region 23 minimap",
                Of(23, 910, 1034, 300, 388, 650, 668, UndergroundOnly, Map + "region 23 (east post minimap rect, underground)")),
            new(42, 24, 2, 378, 766.2, 648, 0, 72.5, 80, -2000, 2000, 0, "foreas_valverde_pools - region 24 minimap",
                Of(24, 306, 418, 585, 730, 680, 715, UndergroundOnly, Map + "region 24 (Thrax interior minimap rect, ug)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {r.Box.Underground}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {r.Underground};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
