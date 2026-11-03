using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Brann Water Refinery
    /// (adv_arieki_torden_plains_brannwaterrefinery, 2093;
    /// 20270724070000_Brann_water_refinery_region_sweep).
    ///
    /// Region 32 (priority 1, unnamed): sets exactly the Torden Plains map's default sky and env map.
    /// The refinery instance contains open plains above and around the buried facility (surface-flagged
    /// navmesh at y 420-445 over the whole map), reached through the entrance dome and the Bane
    /// chunnel; without a volume they show the Brann interior sky of the map default. Placed as
    /// surface-only boxes over the whole map from y 340 to 700, cut around the Main Access box so the
    /// entrance dome (whose floor is surface-flagged but which sets no sky) keeps the interior sky.
    /// Underground-flagged facility rooms are unaffected; the Skybridge and Central Operations set
    /// their own torden sky above it.
    ///
    /// Region 24 (priority 8, sky_dark_fall_test.sky): the 'falling' sky used on five other maps for
    /// the drop below a cliff. Here: the turbine canyon through the facility (floor y 151, 250 m under
    /// the Skybridge) and the abyss off the south-east edge of the plains (floor y 92). Placed as the
    /// whole map below y 340; the lowest facility floor is 370. Medium confidence (convention, not a
    /// label).
    ///
    /// Not placed: region 1 (sky_arieki_default, priority 10: another outdoor sky, nothing ties it to a
    /// place), Maintenance (mission text puts it next to the Reservoir Tanks, 'the maintenance room
    /// that powers it', but the rooms there are already assigned; a candidate is the south-east ramp
    /// corridor, or Final Processing's room), Procurement Sector West/East and Water Regulation (no
    /// evidence picks the corridor networks), region 23 (Molder.sky), marker-only regions 26-31.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class BrannWaterRefineryRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2093;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_plains_brannwaterrefinery - ";

        public static readonly Volume[] Volumes =
        {
            Of(32, -1300, -52, -1300, 1300, 340, 700, SurfaceOnly, Map + "region 32 (plains surface, west)"),
            Of(32, 12, 1300, -1300, 1300, 340, 700, SurfaceOnly, Map + "region 32 (plains surface, east)"),
            Of(32, -52, 12, -40, 1300, 340, 700, SurfaceOnly, Map + "region 32 (plains surface, north of the dome)"),
            Of(32, -52, 12, -1300, -130, 340, 700, SurfaceOnly, Map + "region 32 (plains surface, south of the dome)"),
            Of(24, -1300, 1300, -1300, 1300, -100, 340, AnyLevel, Map + "region 24 (canyon and abyss below y 340)"),
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
