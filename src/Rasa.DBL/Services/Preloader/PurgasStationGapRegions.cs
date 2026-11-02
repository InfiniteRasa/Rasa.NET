using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The three corridor ends Purgas_station_regions left without a region
    /// (adv_foreas_concordia_divide_purgasstation2, 1806; Purgas_station_gap_regions).
    ///
    /// Anywhere in the station no box covers shows the map's own name, "Level 01: Entrance
    /// Hall", which belongs to the top floor's entrance. Of the navmesh outside every box, all
    /// but three places are that entrance hall:
    ///
    /// - Level 03's chunnel turn (arch_bane_industrial_chunnel_turn_02 at (-8, 8, -216), x -24
    ///   to 5, z -229 to -200) that joins the east fuel basin room to the corridor north to the
    ///   control room. The east basin room's box stops at x -14 and the corridor's at z -200;
    ///   the bend between them was the Entrance Hall. It gets Level 03: Main Corridor (18), from
    ///   x -14, clear of the basin room's Level 03: Power Chamber (17, under it in priority).
    /// - Level 03's service tube (arch_bane_industrial_service_straight_01 and _ramp, x 7 to 41,
    ///   z -197 to -188, floor 9 to 17) from the catwalk at the corridor's north end east to the
    ///   console nook south of the control room, which the Main Corridor's east box takes from
    ///   x 38. It gets Level 03: Main Corridor too.
    /// - The top of the service ramp up from Level 02's west maze to Level 01's circuit corridor
    ///   (arch_bane_industrial_service_ramp_16m at (-168, 56, 32), x -172 to -164, z 15 to 33):
    ///   above 61, where Level 02's Service Corridor stops, it was the Entrance Hall. It gets
    ///   Level 01: Service Corridor (9), as the floor's other service tubes have.
    ///
    /// Not placed: Level 01: Power Chamber (8). Level 02's and Level 03's Power Chambers are the
    /// rooms with Bane fuel pools (terra_bane_goo_lake); Level 01 has none. Level 02 has two more
    /// pool chambers in its west maze (x -257 to -191, z 16 to 77, and x -141 to -83, z 15 to
    /// 49, each with command chairs), but both are on Level 02's floor.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class PurgasStationGapRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1806;
        public const uint LevelOneServiceRegion = 9, LevelThreeMainRegion = 18;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_divide_purgasstation2 - ";

        /// <summary>map_region.comment's MySQL length; a longer text stops the migration there.</summary>
        public const int CommentLength = 96;

        public static readonly Volume[] Volumes =
        {
            Of(LevelThreeMainRegion, -14, 6, -230, -200, -10, 24, AnyLevel, Map + "Level 03: Main Corridor (bend from the east basin room)"),
            Of(LevelThreeMainRegion, 6, 38, -198, -187, -10, 24, AnyLevel, Map + "Level 03: Main Corridor (service tube to the console)"),
            Of(LevelOneServiceRegion, -173, -163, 14, 34, 61, 72, AnyLevel, Map + "Level 01: Service Corridor (top of the Level 02 ramp)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        /// <summary>Not by the comment: a SQLite world may have the longer texts these had before.</summary>
        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
