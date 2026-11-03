using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Temporal Chamber (adv_foreas_valverde_plateau_temporalchamber,
    /// 2029; 20270722020000_Temporal_chamber_region_sweep).
    ///
    /// Eloh Messenger Machine (region 4): the south room of the Eloh corridor complex is
    /// arch_eloh_corridor_room_messenger_machine_v01, with the Eloh Communications Array label in it.
    /// The region only adds the name to the map's default Eloh interior settings. Box over that room.
    ///
    /// Foreas Base (region 8): the NW island is an AFS fort (fort HQ, catwalks, holo fences, hospital
    /// and tents), the only human base on the map; the region carries Concordia Divide's Foreas Base
    /// name and settings. Box over the island.
    ///
    /// Alia Das (region 5): the SE island is a Forean village occupied by the Bane (village platforms,
    /// destroyed huts, Bane outpost walls, barracks and tower); the region is Concordia Wilderness'
    /// Alia Das with the war sky. Box over the island, any level.
    ///
    /// Alia Das (region 2, Bane interior): the island's only Bane interior is the control room under
    /// the NE cliff (sleep pods, bio tables), reached by a service entrance, ramp and tunnel; the
    /// minimap rect region 6 frames it. Two underground-only boxes (room; ramp and tunnel) at priority
    /// 5 over region 5.
    ///
    /// Not placed: nothing outstanding (region 1 is Molder.sky only; regions 6 and 7 are minimap rects
    /// that set nothing).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class TemporalChamberRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2029;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_plateau_temporalchamber - ";

        public static readonly Volume[] Volumes =
        {
            Of(4, -25, 25, -113, -64, 244, 268, AnyLevel, Map + "Eloh Messenger Machine (messenger machine room)"),
            Of(8, -680, -320, 330, 620, 150, 400, AnyLevel, Map + "Foreas Base (AFS fort vision area, NW)"),
            Of(5, 470, 790, -800, -300, 270, 440, AnyLevel, Map + "Alia Das (Forean village vision area, SE)"),
            Of(2, 650, 720, -402, -332, 286, 318, UndergroundOnly, Map + "Alia Das (Bane control room under the cliff)"),
            Of(2, 608, 690, -455, -402, 286, 312, UndergroundOnly, Map + "Alia Das (Bane service ramp and tunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
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
