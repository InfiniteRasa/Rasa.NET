using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Quasso Station (adv_arieki_ligo_thunderhead_quassostation, 2105;
    /// 20270724080000_Quasso_station_region_sweep).
    ///
    /// Dropship Zone Observation Level (region 15): the station's upper level (floor y 44) is the
    /// arrival level - the instance door and hospital at (9,46,-182), tunnel cross and tees - and
    /// region 13's minimap rect (name only) covers it. Region 15 repeats that name and adds
    /// sky_arieki_ligo_default.sky, the outdoor sky of the Dropship Zone yard. The one room on that
    /// level that sees outside is arch_bane_industrial_room_control_24m_v07 at (11,44,-93) with
    /// arch_bane_industrial_overlook_exterior_window_v01 at (11,52,-53) facing the landing pads, so
    /// region 15 goes there: box x -22..43 z -126..-46 y 43..72, underground only (the room floor is
    /// underground-flagged; the rock surface above it is not). The tunnels keep region 13's name and
    /// the interior sky.
    ///
    /// Dropship Zone Access Floor (region 14, row 316): its minimap rect ran from -2000 to 2000 high
    /// and so also named the window room above it 'Access Floor' (priority 4 beats regions 13/15).
    /// Recut to the same x/z with y 0..42: the two ground-floor control rooms
    /// (arch_bane_industrial_room_control_24m_v14 at (-38,12,-77) and (58,12,-77), y 12..40) and their
    /// entrances to the yard.
    ///
    /// Quasso Subterranean Command (region 11, row 314): its minimap rect (priority 8) ran at all
    /// heights and overlaps the Bane Barracks rect (priority 7) at x -531..-271 and the Supply
    /// Warehouse rect (priority 6) at x -271..-80. The station is stacked: navmesh floors at y -60..-36
    /// (lower level: Subterranean Command, prison, Central Command), -24..4 (barracks: Bane Stasis
    /// Rooms, infirmary, storeroom) and 12..32 (warehouse level: Juggernaut Warehouses). So all four
    /// Bane Stasis Rooms and Juggernaut Warehouse 02 showed 'Quasso Subterranean Command' and its
    /// music. Recut to the same x/z with y -70..-26; the lower level lies wholly inside the rect, above
    /// -26 there is only outdoor terrain outside the other two rects.
    ///
    /// Not placed: region 1 (Molder.sky editor default).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class QuassoStationRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2105;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_thunderhead_quassostation - ";

        public static readonly Volume[] Volumes =
        {
            Of(15, -22, 43, -126, -46, 43, 72, UndergroundOnly, Map + "Dropship Zone Observation Level (window room)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(316, 14, 2, -1, 62.8, -65, 0, 116.5, 45, -2000, 2000, 0, "arieki_ligo_thunderhead_quassostation - Dropship Zone Access Flo",
                Of(14, -118, 116, -110, -20, 0, 42, AnyLevel, Map + "Dropship Zone Access Floor (ground floor rooms)")),
            new(314, 11, 2, -306, 57.1, 357, 0, 225.5, 283, -2000, 2000, 0, "arieki_ligo_thunderhead_quassostation - Quasso Subterranean Comm",
                Of(11, -532, -80, 74, 640, -70, -26, AnyLevel, Map + "Quasso Subterranean Command (lower level)")),
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
