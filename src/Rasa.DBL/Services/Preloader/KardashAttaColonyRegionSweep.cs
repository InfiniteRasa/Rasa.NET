using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Kardash Atta Colony (adv_arieki_torden_plains_attacolony,
    /// 1773; 20270724060000_Kardash_atta_colony_region_sweep).
    ///
    /// Research Outposts Alpha, Gamma, Delta and Beta (regions 17, 19, 20, 18): each has a marker-
    /// preloaded surface-only label circle round its POI_LABEL, but the outposts are small Brann
    /// corridor complexes whose floors are largely underground-flagged, so the circles miss them and
    /// the rooms show the map default (Kardash Atta Colony, default sky). Added a box per outpost over
    /// its corridor meshes, underground 0, leaving the circles alone: Alpha x -13..29 z -217..-163 y
    /// 268..286 (entrance, airlock, cross, mind-control computer, camp); Gamma x -190..-147 z 143..233
    /// y 218..240 (ruined corridors and the computer cross group the circle stopped short of); Delta x
    /// 327..409 z -454..-419 y 96..120 (entrance ramp from the Inner Nest cave, crosses, mind-control
    /// computer); Beta x 396..453 z 273..307 y 185..205 (the ramp and the two cross-group rooms under
    /// the dome). Epsilon is a single field tent on surface-flagged floor inside its circle and needs
    /// nothing.
    ///
    /// Broken existing, not fixed: region 11 'Harvest Grotto' (Brann interior sky/env/ambient, priority
    /// 22) sits as a label circle (row 164) over the whole Harvest Grotto cave and overrides its proper
    /// yellow-cave region 9 there. No Brann interior exists in the grotto to move it to; disabling the
    /// row is the fix.
    ///
    /// Not placed: region 24 (yellowV02 cave sky; candidate the unnamed yellow-lit cave north of the
    /// centre, too weak), region 25 (greenV02 cave sky; green-lit caves are in the Royal Nest, where
    /// region 10 already sets the sky), region 1 (Molder.sky), marker-only 13 and 23.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class KardashAttaColonyRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1773;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_plains_attacolony - ";

        public static readonly Volume[] Volumes =
        {
            Of(17, -13, 29, -217, -163, 268, 286, AnyLevel, Map + "Research Outpost Alpha (Brann corridors)"),
            Of(19, -190, -147, 143, 233, 218, 240, AnyLevel, Map + "Research Outpost Gamma (Brann corridors)"),
            Of(20, 327, 409, -454, -419, 96, 120, AnyLevel, Map + "Research Outpost Delta (Brann corridors)"),
            Of(18, 396, 453, 273, 307, 185, 205, AnyLevel, Map + "Research Outpost Beta (rooms under the dome)"),
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
