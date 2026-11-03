using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Pravus Research (adv_foreas_concordia_wilderness_pravusresearch,
    /// 1430; 20270725000000_Pravus_research_region_sweep).
    ///
    /// Pravus Interior Halls (4, pri 10): the Bane research facility under the Pravus HQ shell is one
    /// underground complex (arch_bane_industrial tunnel/service/chunnel meshes, x 108..352 z -62..236,
    /// floors y 8..18, all navmesh-underground). Nothing named it inside: the Pravus Research Facility
    /// circle is surface-only, so the interior showed the map default. One underground box y 0..28 over
    /// the whole complex plus one for the entry spiral ramp (up to y 38, under the surface courtyard at
    /// 40.3) makes the halls the base; the four room regions above it in priority override it in their
    /// rooms.
    ///
    /// Pravus Holding Cells (13, pri 11): the prison chunnel
    /// (arch_bane_industrial_chunnel_straight_02_prison_32m_v03, x 296..335 z 23..57) and its overlook;
    /// prison ambient. Definite.
    ///
    /// Pravus Examination Chambers (16, pri 12): the three 16 m service rooms west of the round chamber
    /// (x 112..161 z 71..121, y 16), each with a Bane extraction chair, sensors and consoles; prison
    /// ambient shared with the cells; Nylla's 'experiments'. Probable.
    ///
    /// Pravus Production Chamber (15, pri 13): the great round chunnel room (x 160..288 z 80..234,
    /// floor y 8, six teleporter spawn pads including the west bay) and its south console gallery; the
    /// mission text's 'large chambers containing unspeakable machines' that excrete the Machina.
    /// Probable for the round room, medium for the west teleporter bay.
    ///
    /// Pravus Control Rooms (14, pri 14): arch_bane_industrial_room_control_24m_v08 (x 184..264 z
    /// 7..80), the console-ringed room round the stasis chamber with its upper side rooms. Probable.
    ///
    /// Region 6 (pri 3, cave sky/env/ambient/music): the arrival cave tunnel (terra_foreas_cavern_* at
    /// x -204..-168 z -266..-236, underground floor y 10..15) holding the map arrival point. Probable.
    ///
    /// Recuts: the three Frontlines fortification regions (9 Northwest, 10 Southwest, 11 North) all had
    /// identical surface circles on the Frontlines label, so the Frontlines centre showed 'North
    /// Fortification' and none reached its fortification. Each becomes a box at its own POI_LABEL,
    /// around its radar dish (the 'communication dishes' of mission 50) and sandbag/bunker works.
    /// Definite.
    ///
    /// Not placed: 5 (Molder.sky editor default), 17 (minimap rect, sets nothing).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class PravusResearchRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1430;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_concordia_wilderness_pravusresearch - ";

        public static readonly Volume[] Volumes =
        {
            Of(4, 108, 352, -62, 236, 0, 28, UndergroundOnly, Map + "Pravus Interior Halls (tunnels and corridors)"),
            Of(4, 174, 212, -26, 12, 0, 40, UndergroundOnly, Map + "Pravus Interior Halls (entry spiral ramp)"),
            Of(13, 296, 336, 22, 57, 0, 28, UndergroundOnly, Map + "Pravus Holding Cells (prison chunnel)"),
            Of(16, 110, 160, 70, 122, 0, 28, UndergroundOnly, Map + "Pravus Examination Chambers (extraction rooms)"),
            Of(15, 160, 288, 80, 236, 0, 28, UndergroundOnly, Map + "Pravus Production Chamber (great round chamber)"),
            Of(15, 126, 160, 138, 198, 0, 28, UndergroundOnly, Map + "Pravus Production Chamber (west teleporter bay)"),
            Of(14, 184, 264, 7, 80, 0, 28, UndergroundOnly, Map + "Pravus Control Rooms (control room complex)"),
            Of(6, -204, -168, -266, -236, 0, 25, UndergroundOnly, Map + "region 6 (arrival cave tunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20031, 9, 1, -116.4, 26, 1.6, 81.6, 0, 0, -2000, 2000, 2, "foreas_concordia_wilderness_pravusresearch - Frontlines - Northwest Fortification",
                Of(9, -95, -40, 135, 205, 10, 70, AnyLevel, Map + "Frontlines - Northwest Fortification (bunker)")),
            new(20032, 10, 1, -116.4, 26, 1.6, 81.6, 0, 0, -2000, 2000, 2, "foreas_concordia_wilderness_pravusresearch - Frontlines - Southwest Fortification",
                Of(10, -80, -25, -270, -182, 10, 70, AnyLevel, Map + "Frontlines - Southwest Fortification (bunker)")),
            new(20033, 11, 1, -116.4, 26, 1.6, 81.6, 0, 0, -2000, 2000, 2, "foreas_concordia_wilderness_pravusresearch - Frontlines - North Fortification",
                Of(11, -45, 30, 238, 315, 10, 75, AnyLevel, Map + "Frontlines - North Fortification (ruined huts)")),
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
