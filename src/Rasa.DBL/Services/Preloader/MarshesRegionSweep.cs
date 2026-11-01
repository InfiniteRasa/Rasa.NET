using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Valverde Marshes (adv_foreas_valverde_marshes, 1454;
    /// 20270719000000_Marshes_region_sweep).
    ///
    /// The Marsh overworld. Its swamp water surface (y ~214) is widely flagged underground, so the flag
    /// only singles out real interiors where they sit well below the surface.
    ///
    /// Falcon's Hold (11): the AFS outpost inside the fence at x -228..-100, z 564..648, holding the
    /// Falcon Hold hospital, waypoint, tower teleporter and vendors that the mission text places there.
    ///
    /// The Northern Mud Pit (22) recut: its seeded r 200 circle covered Falcon Hold and, at higher
    /// priority, named the outpost's hospital and tower 'The Northern Mud Pit'. Now a box over the
    /// circle's bounding box with the west edge cut at x -95, so Falcon's Hold shows inside the fence.
    ///
    /// The Refueling Station (17): the walled Bane compound with fuel pipes around the 'Bane Refueling
    /// Station' region label (196,-669), x 70..310, z -800..-535, up to The Repair Station's box.
    ///
    /// Region 29 (Eloh interior sky/env): the underground Eloh spirit room
    /// (arch_eloh_temples_room_spirit_v01, floor 112) below the swamp south-east of Retread City,
    /// underground only, so the surface above keeps its sky.
    ///
    /// Not placed: 2 (two Bane chunnel interiors, can't pick), 28 (four candidate caves), Temple of
    /// Paludos, Logos Research Base Camp, High Point Retreat, North East Watch, The Bane Line, The
    /// Suburbs, 4, 1. Also noted: around Paludos, the seeded circles stack at one centre (Paludos 8 and
    /// 27 at r 200, Paludos Road 26 at r 53), so Paludos Road (p25, inside 27's circle) and Exodus
    /// Point (p3) never show.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class MarshesRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1454;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_marshes - ";

        public static readonly Volume[] Volumes =
        {
            Of(11, -230, -98, 560, 652, 205, 250, AnyLevel, Map + "Falcon's Hold (AFS outpost)"),
            Of(17, 70, 310, -800, -535, 205, 260, AnyLevel, Map + "The Refueling Station (Bane walled compound)"),
            Of(29, -234, -86, -942, -794, 105, 185, UndergroundOnly, Map + "region 29 (underground Eloh spirit room)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(95, 22, 1, 22.9, 218.9, 525.5, 200, 0, 0, -2000, 2000, 2, "foreas_valverde_marshes - The Northern Mud Pit",
                Of(22, -95, 223, 326, 725, -2000, 2000, SurfaceOnly, Map + "The Northern Mud Pit (east of Falcon Hold)")),
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
