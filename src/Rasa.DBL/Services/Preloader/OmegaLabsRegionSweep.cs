using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Omega Labs (adv_arieki_torden_abyss_omegalabs, 2203;
    /// 20270724010000_Omega_labs_region_sweep).
    ///
    /// Omega Labs floors (regions 10-15, recuts of the minimap rows 349-354). The labs drop storey by
    /// storey from the entrance in the south to the missile silo in the north. Navmesh floor heights:
    /// F5 268-270 (entrance cross, Entrance Medic Post, vendor, portable waypoint), F4 256-263 (the 260
    /// corridor, the west corridor and the Commitment Ward base, waypoint 256), F3 240-253 (corridor
    /// loop at 244, Bioweapons 240/251, Test Subject Holding 245-250, NOC 240-248), F2 224-241 (the
    /// Reeducation Ward hall and its ramp up to F3), F1 204-229 (Pharmacogenetics 216, catwalk 228 and
    /// the corridor down through 220/212/204), Sublevel 175-214 (stairwell, corridor grid at 188, the
    /// Omega Project Control office, silo floor). Priorities confirm it: each floor sits one step under
    /// its rooms (F4 3 &lt; Commitment Ward 4; F3 5 &lt; Bioweapons, Test Subject Holding, NOC 6-8; F2
    /// 9 &lt; Reeducation 10; F1 11 &lt; Pharmacogenetics 12; Sublevel 13 &lt; Sandorf's Quarters 14).
    /// The old rows were the minimap rectangles with y -2000..2000, so wherever rects overlapped the
    /// deeper floor (higher priority) won on every storey: F1 named the F4 and F5 corridors, Sublevel
    /// named F3's corridors, Test Subject Holding and the Reeducation ramp. Each row becomes a box
    /// limited to its storey: F5 y 264-280, F4 255-266, F3 238-254, F2 218-243 (Reeducation hall x
    /// 68..137 z -313..-190, plus a second box for the ramp x 96..112 z -190..-148, so the NOC's 240
    /// tier stays F3/NOC), F1 200-232 (x -58..73, ending where the ramp into Reeducation starts),
    /// Sublevel 168-216 (from the stairwell at z -148, so the flat 204 corridor from F1 is one region).
    /// The Sublevel recut only removes wrong coverage; there is no spot it newly gains, so its tests
    /// are keep-tests. Existing room circles are left as they are.
    ///
    /// Not placed: region 1 (dark interior sky; in the Sublevel priority group but no evidence for
    /// which space). Region 2 Molder.sky skipped.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class OmegaLabsRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2203;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_abyss_omegalabs - ";

        public static readonly Volume[] Volumes =
        {
            Of(13, 96, 112, -190, -148, 222, 243, AnyLevel, Map + "Omega Labs: Floor F2 (ramp up to F3)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(349, 10, 2, 0, 261, -369, 0, 82, 136, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Floor F5",
                Of(10, -82, 82, -505, -233, 264, 280, AnyLevel, Map + "Omega Labs: Floor F5 (entrance level, y 268)")),
            new(350, 11, 2, -47, 261, -268, 0, 89, 68.5, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Floor F4",
                Of(11, -136, 42, -337, -199, 255, 266, AnyLevel, Map + "Omega Labs: Floor F4 (y 256-263)")),
            new(351, 12, 2, 16, 240.6, -200, 0, 155.5, 93.5, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Floor F3",
                Of(12, -140, 172, -294, -106, 238, 254, AnyLevel, Map + "Omega Labs: Floor F3 (y 240-253)")),
            new(352, 13, 2, 104, 232.3, -220, 0, 36.5, 93, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Floor F2",
                Of(13, 68, 137, -313, -190, 218, 243, AnyLevel, Map + "Omega Labs: Floor F2 (Reeducation hall)")),
            new(353, 14, 2, 16, 261.1, -227, 0, 73.5, 113.5, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Floor F1",
                Of(14, -58, 73, -341, -114, 200, 232, AnyLevel, Map + "Omega Labs: Floor F1 (y 204-229)")),
            new(354, 15, 2, 0, 214.1, 24, 0, 125, 193.5, -2000, 2000, 0, "arieki_torden_abyss_omegalabs - Omega Labs: Sublevel",
                Of(15, -125, 125, -148, 218, 168, 216, AnyLevel, Map + "Omega Labs: Sublevel (y 175-214)")),
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
