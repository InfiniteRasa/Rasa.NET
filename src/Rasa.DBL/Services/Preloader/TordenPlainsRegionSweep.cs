using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Torden Plains (adv_arieki_torden_plains, 1764;
    /// 20270724050000_Torden_plains_region_sweep).
    ///
    /// Region 16 (sky_dark_fall_test.sky, priority 27): the 'falling' sky used on five other maps for
    /// the drop below a cliff. Torden Plains sits on a mesa at y 420-440 with the abyss south and east
    /// of it, its floor at y 88; Firefall Point's lava falls and the eastern lava falls pour into it,
    /// and a steam-vent canyon (floor 219) runs under the Brann bridge east of Eir Crater. Placed as
    /// the whole map below y 330 (the lowest walkable floors above it: the Abyssal Zone shelf at 365,
    /// the lava cave at 380, the Brann operations room at 372). Only the sky changes; names and other
    /// settings in the abyss are unchanged. Medium confidence: the evidence is the convention, not a
    /// label.
    ///
    /// Irendas Colony Support Facility (region 20, row 20087): the marker preloader had put its circle
    /// on the Irendas colony's auction house (the AUCTIONEER 'Military Surplus: Irendas Colony' at
    /// (375,-91)). The facility itself is the walled yard at the 'Irendas Support Facility'
    /// REGION_LABEL (786.6,430.4,565.7), where the matching control-point region 31 is: Bane outpost
    /// walls x 703..871 z 485..646, two Brann domes, sandbag rings, hospital and waypoint. Recut to a
    /// surface-only box over that yard (x 690..885 z 470..660 y 415..480). The control point (priority
    /// 28) still supplies the name there; region 20 now gives the yard its sky, ambient and music, and
    /// the colony's auction yard and the ground east of the colony walls lose the stray settings.
    ///
    /// Observation (not changed): the navmesh flags the flat sulfur ponds of Geyser Chimney Basin (y
    /// 428.8), the lava rivers round Firefall Point and ground under rock arches as underground, so the
    /// surface-only label circles there don't apply on those polygons (default name/sky while standing
    /// in a pond). Also the small Brann operations room by the east canyon
    /// (arch_brann_corridor_room_01_operations_v01 at (689,381,452)) and the corridor down to the
    /// refinery door at the Support Facility (769,422,504) have no region of their own.
    ///
    /// Not placed: region 32 (human interior, Irendas group; auction house or HQ top), region 8 (low
    /// battlefield background), region 1 (Molder.sky), region 33 (sets nothing).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class TordenPlainsRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1764;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_plains - ";

        public static readonly Volume[] Volumes =
        {
            Of(16, -1500, 1500, -1500, 1500, -100, 330, AnyLevel, Map + "region 16 (the abyss below the plains cliffs)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20087, 20, 1, 375, 433.8, -91, 84.4, 0, 0, -2000, 2000, 2, "arieki_torden_plains - Irendas Colony Support Facility",
                Of(20, 690, 885, 470, 660, 415, 480, SurfaceOnly, Map + "Irendas Colony Support Facility (walled yard)")),
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
