using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Ojasa Atta Colony on Torden Incline and the Queen Chamber in its hive instance
    /// (Ojasa_atta_colony_regions).
    ///
    /// Incline (1761) has region 9, "Ojasa Atta Colony" (priority 8: its name and the cave
    /// ambient REGION_FITHIK_CAVE_LARGE_ROOM), which had no volume. It is the hive mound, not the
    /// whole highlands: the Atta queen's-hive growths (arch_arieki_atta_queens_obj01, x -594 to
    /// -505, z 253 to 327, y 323 to 419) heaped over the cavern ramp, door and instance piece
    /// that lead down to the hive door (x -562 to -536, z 281 to 358, from y 300), the bridge
    /// coupler on its south face, and the five bridge towers round it at (-643, 317),
    /// (-604, 245), (-582, 387), (-485, 295) and (-466, 383). Its box takes those, x -670 to
    /// -455 and z 225 to 405, y 280 to the tower tops at 430; nothing else stands in it. The
    /// Ojasa Highlands circle (region 17, priority 14, surface only, name only) still names its
    /// southern half on the surface, where the colony's ambient plays under it.
    ///
    /// The hive (adv_arieki_torden_incline_ojasaattahive, 1865) is itself named "Ojasa Atta
    /// Colony" (its region 0). Its region 5, "Queen Chamber" (priority 1: sky_arieki_atta_
    /// interior_cave_blue, env_arieki_atta_cave_interior, REGION_ATTA_INTERNAL_TUNNEL,
    /// MUSIC_ARIEKI_INCLINE_02), had no volume. It is the south-east of the hive's six domed
    /// caverns (terra_arieki_atta_cavern_roof_v04 at (214, 172, -297)), the one with Atta Queen
    /// Kardash's throne (arch_atta_throne_v01 at (182, 131, -291)) among her eggs, cover and
    /// obelisks over the sulfur ponds (y 97). Its roof's outline runs (7, -318), (86, -484),
    /// (214, -526), (383, -431), (429, -374), (407, -204), (338, -116), (271, -70),
    /// (127, -71), (63, -125), (19, -239); the chamber is three boxes inside it, from y 95 to
    /// 200: the body x 60 to 410 and z -470 to -150, its south x 90 to 330 to z -520, and its
    /// north x 175 to 345 to z -75, with the north door (248, 110, -112). They stay out of the
    /// middle cavern's outline (roof at (16, 156, 12)), which overlaps the chamber's to the
    /// north-west. Region 3 (priority 2), the map's whole-hive box, sets nothing, so the
    /// chamber's settings are what the client applies there.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class OjasaAttaColonyRegions
    {
        public sealed record Volume(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint InclineMapContextId = 1761, HiveMapContextId = 1865;
        public const uint ColonyRegion = 9, QueenChamberRegion = 5;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(InclineMapContextId, ColonyRegion, -670, -455, 225, 405, 280, 430, "arieki_torden_incline - Ojasa Atta Colony (hive mound)"),
            Of(HiveMapContextId, QueenChamberRegion, 60, 410, -470, -150, 95, 200, "arieki_torden_incline_ojasaattahive - Queen Chamber (dome)"),
            Of(HiveMapContextId, QueenChamberRegion, 90, 330, -520, -470, 95, 200, "arieki_torden_incline_ojasaattahive - Queen Chamber (south)"),
            Of(HiveMapContextId, QueenChamberRegion, 175, 345, -150, -75, 95, 200, "arieki_torden_incline_ojasaattahive - Queen Chamber (north door)"),
        };

        private static Volume Of(uint map, uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(map, region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {v.MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {v.MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes)
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {v.MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
