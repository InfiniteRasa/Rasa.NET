using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Maligo Base (adv_foreas_valverde_plateau_maligobasev3, 1830;
    /// 20270722010000_Maligo_base_region_sweep).
    ///
    /// Maligo Base is a two-level underground Bane facility under a crater. Level 1 is the lower floor
    /// (-139..-130), Level 2 the one above (-104..-60); the map default is 'Level 01: Service Corridor'
    /// with the Bane interior sky, so until now the open crater, all of Level 2 and every Level 1 room
    /// showed that.
    ///
    /// Communication Tower Deconstruction Project (region 3): the open crater with the infected Eloh
    /// communications tower in its excavated pit (label 'Eloh Dig Site'). Region 3 is the only outdoor-
    /// sky region and the top priority. Two boxes: north part (with the pit) any level, south part
    /// surface-only so the Construction Control room underneath keeps its name.
    ///
    /// Construction Control (region 15, recut): its minimap rect over the crater-level control room had
    /// no height and named rooms on both levels 130 units below. Limited to y 0..70.
    ///
    /// Level 1: Power Chamber (region 5) on the reactor room floor and its south arm; Control Room
    /// (region 7) on both control rooms either side of the reactor; Main Corridor (region 6, medium) on
    /// the chunnel hall south of the rooms; Security Corridor (region 12) on the console-filled north
    /// wing (missions put the security CPUs in the north of the first level). The entrance, north
    /// tunnel, service passages and west tunnel loop keep the default Level 01: Service Corridor.
    ///
    /// Level 2: Wormhole Chamber (region 13) on the floor of the big chunnel_room_maligo in the west,
    /// Wormhole Overlook (region 14) on the -90 walkway around it; Control Room (region 11) on the
    /// three control rooms (two above Level 1's, one west of the chamber); Power Chamber (region 10) on
    /// the reactor's upper walkways; Service Corridor (region 8) on the -92 service ring round the
    /// reactor with the Barricade.
    ///
    /// Not placed: Level 02: Main Corridor (region 9), the duplicate Level 02: Service Corridor (region
    /// 4).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class MaligoBaseRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1830;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_plateau_maligobasev3 - ";

        public static readonly Volume[] Volumes =
        {
            Of(3, -260, 40, 135, 390, 2, 160, AnyLevel, Map + "Comm Tower Deconstruction Project (crater N)"),
            Of(3, -260, 40, -10, 135, 2, 160, SurfaceOnly, Map + "Comm Tower Deconstruction Project (crater S)"),
            Of(5, -77, 49, 89, 192, -142, -106, UndergroundOnly, Map + "Level 01: Power Chamber (reactor floor)"),
            Of(5, -24, -4, 64, 89, -142, -106, UndergroundOnly, Map + "Level 01: Power Chamber (south arm)"),
            Of(7, 49, 115, 95, 161, -142, -106, UndergroundOnly, Map + "Level 01: Control Room (east room)"),
            Of(7, -143, -77, 95, 161, -142, -106, UndergroundOnly, Map + "Level 01: Control Room (west room)"),
            Of(6, -141, 113, 24, 88, -142, -106, UndergroundOnly, Map + "Level 01: Main Corridor (chunnel hall)"),
            Of(12, -55, 50, 246, 340, -145, -106, UndergroundOnly, Map + "Level 01: Security Corridor (north wing)"),
            Of(13, -305, -180, 42, 192, -106, -95, UndergroundOnly, Map + "Level 02: Wormhole Chamber (chamber floor)"),
            Of(14, -312, -158, 40, 192, -95, -82, UndergroundOnly, Map + "Level 02: Wormhole Overlook (walkway at -90)"),
            Of(11, -158, -78, 96, 161, -106, -80, UndergroundOnly, Map + "Level 02: Control Room (west room)"),
            Of(11, 50, 130, 95, 160, -106, -80, UndergroundOnly, Map + "Level 02: Control Room (east room)"),
            Of(11, -400, -312, 50, 170, -106, -80, UndergroundOnly, Map + "Level 02: Control Room (far west room)"),
            Of(10, -64, 36, 78, 178, -106, -35, UndergroundOnly, Map + "Level 02: Power Chamber (reactor upper rings)"),
            Of(8, -115, 65, 0, 245, -97, -84, UndergroundOnly, Map + "Level 02: Service Corridor (ring at -92)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(184, 15, 2, -124, 0, 63, 0, 48, 67.5, -2000, 2000, 0, "foreas_valverde_plateau_maligobasev3 - Maligo Base: Construction",
                Of(15, -172, -76, -5, 131, 0, 70, AnyLevel, Map + "Maligo Base: Construction Control (crater room)")),
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
