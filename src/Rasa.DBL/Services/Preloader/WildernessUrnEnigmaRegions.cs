using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Daghda's Urn Village and Enigma Falls and Cavern in the Wilderness
    /// (adv_foreas_concordia_wilderness, 1220; Wilderness_urn_enigma_regions).
    ///
    /// Daghda's Urn Village has three regions of that name. 14 (priority 36: REGION_HIGHLANDS,
    /// MUSIC_FOREAS_FOREAN_01) and 48 (priority 37: the same ambient, MUSIC_SAFE_ZONE_FOREAN)
    /// had the same 200 m circle at the village label, so 48 was everywhere 14 was and 14 never
    /// showed; 38 (priority 38: sky_foreas_cave_short, env_foreas_cave_interior,
    /// REGION_FOREAS_CAVE_WITH_ECHO) had the circle too, turned off by Region_sky_circles for
    /// putting its sky over the open village. The village is Forean platforms, stairs, huts and
    /// a large tent on the slope east of the urn (x -749 to -590, z 680 to 855), and the AFS's
    /// three corman buildings at its north-east: the housing (arch_corman_housing_v01, with the
    /// class trainer and crafting station), the research HQ (arch_corman_researchhq_v01, its
    /// vendors at the door) and the hospital (arch_corman_hospital_v01, two floors of beds and
    /// the revive point). So:
    ///
    /// - 48, the safe zone, becomes a box of the village and its buildings (x -755 to -530,
    ///   z 675 to 918, y 255 to 310), leaving 14's circle round it as the village's
    ///   surroundings;
    /// - 38, the interior, gets the three buildings, each its own box.
    ///
    /// Daghda's Urn itself (region 15, the urn-shaped cavern west of the village) keeps its
    /// circle.
    ///
    /// Enigma Falls (region 10, priority 32: REGION_HIGHLANDS and the name) is the waterfall at
    /// the "Enigma Caverns" label (vfx_waterfall_32m_48m at (-862, 163, -585)) and its pool
    /// (x -886 to -818, y 170), the river leaving it north-east: x -905 to -805, z -615 to -525,
    /// on the surface. Enigma Cavern (region 36, priority 33: the cave sky and env map,
    /// REGION_ELOH_WATER_CAVERN_01, MUSIC_FOREAS_FOREAN_01) is behind it: the Eloh cavern
    /// entrance at the foot of the falls (x -879 to -846, z -602 to -569) into the caves west and
    /// south, down to the cavern floor at y 157 and the Eloh corridor with its logos dispenser at
    /// y 141 (x -859 to -817, z -750 to -676): x -1045 to -815, z -755 to -520, under the
    /// terrain only, as the map's two minimap rectangles for it are.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; region 48's circle changes only while
    /// it is as seeded. Down takes out what went in and gives the circle back.
    /// </summary>
    public static class WildernessUrnEnigmaRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1220;
        public const uint VillageSurroundingsRegion = 14, VillageRegion = 48, VillageInteriorRegion = 38, EnigmaFallsRegion = 10, EnigmaCavernRegion = 36;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_concordia_wilderness - ";

        public static readonly Volume[] Volumes =
        {
            Of(VillageInteriorRegion, -571, -537, 863, 897, 276, 302, AnyLevel, Map + "Daghda's Urn Village (research HQ)"),
            Of(VillageInteriorRegion, -620, -589, 846, 899, 274, 295, AnyLevel, Map + "Daghda's Urn Village (housing)"),
            Of(VillageInteriorRegion, -666, -630, 870, 915, 281, 305, AnyLevel, Map + "Daghda's Urn Village (hospital)"),
            Of(EnigmaFallsRegion, -905, -805, -615, -525, 160, 222, SurfaceOnly, Map + "Enigma Falls (waterfall and pool)"),
            Of(EnigmaCavernRegion, -1045, -815, -755, -520, 125, 212, UndergroundOnly, Map + "Enigma Cavern (behind the falls)"),
        };

        /// <summary>Region 48's seeded circle at the village label and the box it becomes.</summary>
        public const uint VillageRowId = 23;
        public const double SeededX = -662.9, SeededY = 273.7, SeededZ = 799.5, SeededRadius = 200;
        public const string SeededComment = Map + "Daghda's Urn Village";
        public static readonly Volume Village = Of(VillageRegion, -755, -530, 675, 918, 255, 310, AnyLevel, Map + "Daghda's Urn Village (village and buildings)");

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(Village.X)}, pos_y = {N(Village.Y)}, pos_z = {N(Village.Z)}, radius = 0, " +
                $"half_x = {N(Village.HalfX)}, half_z = {N(Village.HalfZ)}, min_y = {N(Village.MinY)}, max_y = {N(Village.MaxY)}, underground = {AnyLevel}, comment = {S(Village.Comment)} " +
                $"WHERE id = {VillageRowId} AND map_context_id = {MapContextId} AND region_id = {VillageRegion} AND shape = {Circle} " +
                $"AND ABS(pos_x - {N(SeededX)}) < 0.01 AND ABS(pos_z - {N(SeededZ)}) < 0.01 AND ABS(radius - {N(SeededRadius)}) < 0.01;";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(SeededX)}, pos_y = {N(SeededY)}, pos_z = {N(SeededZ)}, radius = {N(SeededRadius)}, " +
                $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(SeededComment)} " +
                $"WHERE id = {VillageRowId} AND {At(Village, "map_region")} AND ABS(half_x - {N(Village.HalfX)}) < 0.01 AND ABS(half_z - {N(Village.HalfZ)}) < 0.01;";

            foreach (var v in Volumes)
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
