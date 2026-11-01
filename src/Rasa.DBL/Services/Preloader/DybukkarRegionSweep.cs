using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Dybukkar Garrison (adv_arieki_torden_abyss_dybukkar, 2190;
    /// 20270724020000_Dybukkar_region_sweep).
    ///
    /// Repair Yard (region 16, recut of row 20190 plus two boxes). The seeded surface-only circle,
    /// radius 180 round the POI label at (-40, 279, 58), spills out of the yard bowl. Because Repair
    /// Yard (priority 16, exterior Dybukkar sky, plains env map) outranks Main Production Chamber (13),
    /// Maintenance Area C (9) and North Concourse (15), the north third of the Main Production Chamber
    /// (z -64 to -122, floor 320-330, not flagged underground) and all of the Maintenance Area C
    /// service tunnels showed the Repair Yard name and its outdoor sky. The yard is the open bowl at
    /// navmesh 279-314: Bane outpost trenches at 279-290, catwalk gantries at 299, slopes up to about
    /// 314. The recut box covers x -98..66, z -48..200, y 270-322, surface only. It stops at the
    /// service tunnels on the west and the North Concourse box on the east. Two extra boxes add the
    /// west slope (x -160..-98, z 39..200) and the north-east slope (x 66..140, z 90..200). The
    /// Dropship Area circles keep the north.
    ///
    /// Not placed: region 22 (the interior variant of Repair Yard; no roofed yard room identifiable),
    /// 11 Drug Development Ward and 15 Transport Annex (name only, no evidence). Region 1 Molder.sky
    /// skipped.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class DybukkarRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2190;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_abyss_dybukkar - ";

        public static readonly Volume[] Volumes =
        {
            Of(16, -160, -98, 39, 200, 270, 322, SurfaceOnly, Map + "Repair Yard (west side of the yard bowl)"),
            Of(16, 66, 140, 90, 200, 270, 322, SurfaceOnly, Map + "Repair Yard (north-east slope)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20190, 16, 1, -39.8, 279.2, 57.8, 180, 0, 0, -2000, 2000, 2, "arieki_torden_abyss_dybukkar - Repair Yard",
                Of(16, -98, 66, -48, 200, 270, 322, SurfaceOnly, Map + "Repair Yard (open yard, trenches and gantries)")),
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
