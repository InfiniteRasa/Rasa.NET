using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Bootcamp (Luna) (adv_bootcamp, 1985;
    /// 20270726040000_Bootcamp_region_sweep).
    ///
    /// Region 5 (Denzil's Caldera, priority 6) is the Caldera's interior variant: human interior sky,
    /// heavy dark blue fog, REGION_GENERIC_ROOM_TONE_HEAVY and the same MUSIC_BOOTCAMP_BATTLE as the
    /// Caldera's outdoor regions 4 and 9. Its seeded label circle (row 213) is disabled. The Denzil's
    /// Caldera Outpost (POI 95.7,109,136.8) has two furnished walk-in buildings: the damaged barracks
    /// arch_hum_fort_barracks_v01_damaged at 79,109,179 (beds, dressers, shelves, fan, ceiling light)
    /// and the damaged field hospital arch_hum_outpost_hospital_v01_damaged at 116,109,109 (desk,
    /// tables, chairs, monitors, lab counter, ceiling fan). Both are surface navmesh, floor about y
    /// 109.5; their roofs carry navmesh at y 117-118 and 115.2. Region 5 gets a box inside each wall
    /// line, capped under the roof, so courtyard and rooftops stay region 4. Probable: the settings say
    /// 'interior of the Caldera' and these are the only furnished interiors there.
    ///
    /// Row 212 (region 4, the hand-cut outdoor box west of the Eloh cavern entrance) is surface-only,
    /// but this map's navmesh flags the caldera's lakes and river (y 79 water) and scattered terrain
    /// tiles as 0x02. Those spots currently fall back to region 0, i.e. Luna Cavern name,
    /// sky_foreas_bootcamp_lunacave and the cave env map, out in the open caldera. The recut keeps the
    /// extent (x -512..208, z -512..512) and sets underground 0; all cavern geometry lies east of x
    /// 209, so nothing cave-like is pulled in. Probable.
    ///
    /// Noted, not changed: Luna Cavern (region 2, row 211) is also surface-only and its pond (y 116) is
    /// flagged, but there the fallback is region 0, which is also Luna Cavern with the dedicated
    /// lunacave sky, so it is not clearly wrong. Regions 9 and 10 share an identical circle (rows
    /// 214/215) and region 9 is one priority higher with the same flags, so region 10
    /// (MUSIC_BOOT_CAMP_ELOH) never shows anywhere; and row 212 covers both circles except a sliver at
    /// x 208-208.8. Not in the backlog, left alone.
    ///
    /// Not placed: region 1 (Molder.sky only).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class BootcampRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1985;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "bootcamp - ";

        public static readonly Volume[] Volumes =
        {
            Of(5, 69, 89, 166, 192, 107, 116, AnyLevel, Map + "Denzil's Caldera (outpost barracks interior)"),
            Of(5, 106, 125, 96, 120, 107, 112, AnyLevel, Map + "Denzil's Caldera (outpost field hospital interior)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(212, 4, 2, -152, 120.7, 0, 0, 360, 512, -2000, 2000, 2, "bootcamp - Denzil's Caldera: the outdoors, west of the Eloh cavern entrance",
                Of(4, -512, 208, -512, 512, -2000, 2000, AnyLevel, Map + "Denzil's Caldera (outdoors, west of cave mouth)")),
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
