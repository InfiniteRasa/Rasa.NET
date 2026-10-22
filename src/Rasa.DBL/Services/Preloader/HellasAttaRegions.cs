using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Mount Hellas Outpost and Atta Territory on Torden Plains (adv_arieki_torden_plains, 1764;
    /// Hellas_atta_regions).
    ///
    /// The missions put the outpost "northwest of Irendas" with Colonel Franks in it, its lookouts
    /// "in the watchtower outside at Mt. Hellas", and the Lightning Fields east of it. The outpost
    /// has two parts, and the map has a region for each:
    ///
    /// - Region 7, "Mount Hellas Outpost" (priority 15: the human interior sky, env_human_interior,
    ///   MUSIC_SAFE_ZONE_AFS_OUTPOST, its own minimap), is the hall dug into the mountain
    ///   (arch_hum_corr_room_straight_128m, x -628 to -490, z 383 to 515, floor 438, roof 459): the
    ///   hospital with five beds, its holographic sign and the revive point (-534.6, 442, 416.8),
    ///   the barracks with the Challenge Arena sign and door (map link 77 at (-612.5, 439, 417)),
    ///   the armor and weapon shops, the office, the crafting station and the waypoint. A 24 m
    ///   corridor runs east from it to the entrance in the cliff (arch_hum_corridor_entrance at
    ///   (-466, 438, 449)). Its seeded minimap rectangle
    ///   (x -637.5 to -468.5, z 377 to 521) is the hall's footprint, but at any height, so it also
    ///   put the interior sky on the mountain over it (its ground is 469 and up). It keeps the
    ///   rectangle from 430 to 465: everything walkable under it is at 462 or below.
    /// - Region 3, "Mount Hellas Outpost" (priority 14: the Lightning Fields sky,
    ///   REGION_BATTLEFIELD_DISTANT_01), had no volume. It is the outpost outside: the fenced yard
    ///   east of the entrance (fence x -403 to -347, z 389 to 508) with the dropship pad and the two
    ///   dropships, and the mountain over the hall with its two bunkers. The three watchtowers
    ///   (arch_hum_outpost_tower) are the coffee mission's three lookouts: two at the yard's east
    ///   corners, (-363, 403) and (-363, 496), and one on the mountain over the entrance
    ///   (-477, 472, 449). Its box is x -515 to -340, z 385 to 515, y 425 to 510. The AFS trench
    ///   line further east (x -323 to -248) faces the Lightning Fields and is left out.
    /// - Region 14, "Atta Territory" (priority 4: the map's own sky, REGION_ATTA_OUTDOOR_AREA),
    ///   had no volume. The missions send you to "the Atta Colony Hive Entrance west of Irendas
    ///   Colony", the way into the Kardash Atta Colony: map link 74 at (-510.4, 432.2, -60.3), at
    ///   the foot of a cave ramp (terra_arieki_cavern_offsetramp_48m) into the rock mound west of
    ///   it, with the Atta queen's growths (arch_arieki_atta_queens_obj01) round its mouth. The Atta
    ///   bridges and bridge towers run out from there: west along the mound's south side to a ramp
    ///   at (-778, -66), north to (-411, 141), east to a ramp at (-179, -126) and south to a ramp at
    ///   (-475, -333). On the road at its north-east corner a holographic sign reads "Atta"
    ///   (ArchHumBaseHolosignAtta01 at (-206.5, 84.5)), just west of the crossroads whose signs
    ///   point to Irendas and Mt. Hellas (-154, 53 and -148, 71). Four boxes follow the bridges,
    ///   y 390 to 700: the mound with the north and south arms (x -640 to -405, z -370 to 165), the
    ///   east network (x -405 to -150, z -320 to -40) and its north half up to the sign (x -405 to
    ///   -200, z -40 to 165), and the west arm (x -810 to -640, z -250 to -30), north of the Bane
    ///   Drill Control Outpost.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded and
    /// no box of that region is at that spot; the recut row changes only while it is as seeded.
    /// Down puts the seeded row back and takes the new ones out.
    /// </summary>
    public static class HellasAttaRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded rectangle as it is (at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, string Comment, Volume Box);

        public const uint MapContextId = 1764;
        public const uint OutpostRegion = 3, OutpostHallRegion = 7, AttaTerritoryRegion = 14;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_plains - ";

        public static readonly Volume[] Volumes =
        {
            Of(OutpostRegion, -515, -340, 385, 515, 425, 510, Map + "Mount Hellas Outpost (the yard and the mountain over the hall)"),
            Of(AttaTerritoryRegion, -640, -405, -370, 165, 390, 700, Map + "Atta Territory (the mound, the hive entrance, north and south bridges)"),
            Of(AttaTerritoryRegion, -405, -150, -320, -40, 390, 700, Map + "Atta Territory (the east bridges)"),
            Of(AttaTerritoryRegion, -405, -200, -40, 165, 390, 700, Map + "Atta Territory (the north-east bridges, to the Atta sign)"),
            Of(AttaTerritoryRegion, -810, -640, -250, -30, 390, 700, Map + "Atta Territory (the west bridges)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(150, Map + "Mount Hellas Outpost", Rect(OutpostHallRegion, -553, 449, 84.5, 72, 430, 465, Map + "Mount Hellas Outpost (the hall in the mountain)")),
        };

        private static Volume Rect(uint region, double x, double z, double halfX, double halfZ, double floor, double roof, string comment) =>
            new(region, x, floor, z, halfX, halfZ, floor, roof, comment);

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {AnyLevel};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET min_y = -2000, max_y = 2000, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND min_y = {N(r.Box.MinY)} AND max_y = {N(r.Box.MaxY)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
