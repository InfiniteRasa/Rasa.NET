using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Bane Supply Depot
    /// (adv_foreas_valverde_marshes_banesupplydepot, 1451;
    /// 20270719040000_Bane_supply_depot_region_sweep).
    ///
    /// Bane Supply Depot is an open swamp compound (surface 122..132) over an underground Bane complex
    /// (y 76..120), with an entrance chunnel at the west edge and the Depot HQ under the south-east
    /// corner.
    ///
    /// Region 7 (minimap rect, Bane interior sky/env) recut: the rect ran -2000..2000, so the whole
    /// open compound got the interior sky. It now covers only the underground chunnel complex (Maroxin
    /// Storage chunnels, ramps, chunnel-room hall, two control rooms) at y 72..112, underground only.
    ///
    /// Region 8 (minimap rect over Depot HQ) recut likewise: it is now limited to the underground HQ
    /// control room, its service rooms and ramp (y 90..122, ug 1), so the ground above shows the
    /// outdoor default.
    ///
    /// AFS Crash Site (5): the crashed dropship and wreckage at (134,346) beside the Survivor's Camp
    /// hospital; music MARSHES_01 and the name.
    ///
    /// Not placed: 9 (identical to the map default), 2, 3, 4 (unnamed interior regions with no
    /// distinguishing room), 1 (Molder.sky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class BaneSupplyDepotRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1451;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_marshes_banesupplydepot - ";

        public static readonly Volume[] Volumes =
        {
            Of(5, 85, 170, 290, 370, 110, 160, AnyLevel, Map + "AFS Crash Site (crashed dropship + camp)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(87, 7, 2, -56, 160.4, 17, 0, 142.5, 242.5, -2000, 2000, 0, "foreas_valverde_marshes_banesupplydepot - region 7 minimap",
                Of(7, -192, 90, -196, 236, 72, 112, UndergroundOnly, Map + "region 7 (underground chunnel complex)")),
            new(88, 8, 2, 189, 171.7, -175, 0, 60.5, 67, -2000, 2000, 0, "foreas_valverde_marshes_banesupplydepot - region 8 minimap",
                Of(8, 141, 242, -230, -114, 90, 122, UndergroundOnly, Map + "region 8 (Depot HQ underground room)")),
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
