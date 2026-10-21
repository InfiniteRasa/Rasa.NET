using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Convict Pass on Burning Steps (adv_arieki_ligo_burningsteps, 1993; Convict_pass_regions).
    ///
    /// The pass is a lava tube through the ridge between the plateau north of Staal (y 246)
    /// and the low ground to the north-east (y 165). Its south mouth
    /// (terra_arieki_ligo_lava_tube_entrance at (232, 208, 432)) opens on the Staal Overlook's
    /// ledge; inside, a straight and two 48 m tube rooms run north at y 208 to 212
    /// (x 209 to 255, z 431 to 553), then a ramp and an offset ramp go down to the north mouth
    /// (the second tube entrance at (264, 176, 648)), which opens at y 170 into a ravine
    /// between lava walls running north-east to the low ground.
    ///
    /// Region 13, "Convict Pass" (priority 26: sky_arieki_ligo_crucible_magma_caven,
    /// env_arieki_cave_interior, REGION_FOREAS_CAVE_WITH_ECHO), is the tube. Its row was the
    /// minimap's rectangle (x 185.5 to 306.5, z 395.5 to 662.5) at every height, which also put
    /// the cave over the tube's roof, the plateau east of it and the ledge beyond; it becomes
    /// three boxes along the tube under its roof: the south mouth, straight and rooms (y 198 to
    /// 226), the ramp (y 185 to 216), and the offset ramp to the north mouth (y 160 to 212).
    ///
    /// Region 19, "Convict Pass" (priority 25: the Torden mires sky outdoors and the name), had
    /// no volume: it is the ravine at the north mouth, x 245 to 345, z 685 to 790, floor
    /// y 164 to 175 under the walls' tops. The south mouth's side is the Staal Overlook's.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; the minimap row changes only while it
    /// is as seeded. Down takes out what went in and gives the row back as it was.
    /// </summary>
    public static class ConvictPassRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1993;
        public const uint TubeRegion = 13, RavineRegion = 19;
        public const byte Box = 2;

        private const string Map = "arieki_ligo_burningsteps - ";

        /// <summary>Region 13's minimap rectangle as MapRegionPreloader seeded it.</summary>
        public const uint TubeRowId = 217;
        public const double SeededX = 246.0, SeededY = 228.9, SeededZ = 529.0, SeededHalfX = 60.5, SeededHalfZ = 133.5;
        public const string SeededComment = Map + "Convict Pass";

        /// <summary>The south mouth, straight and rooms: what the seeded row becomes.</summary>
        public static readonly Volume SouthTube = Of(TubeRegion, 205, 262, 392, 557, 198, 226, Map + "Convict Pass (lava tube, south)");

        public static readonly Volume[] Volumes =
        {
            Of(TubeRegion, 212, 252, 555, 602, 185, 216, Map + "Convict Pass (lava tube, ramp)"),
            Of(TubeRegion, 220, 290, 600, 688, 160, 212, Map + "Convict Pass (lava tube, north)"),
            Of(RavineRegion, 245, 345, 685, 790, 150, 200, Map + "Convict Pass (ravine at the north mouth)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE map_region SET pos_x = {N(SouthTube.X)}, pos_y = {N(SouthTube.Y)}, pos_z = {N(SouthTube.Z)}, half_x = {N(SouthTube.HalfX)}, half_z = {N(SouthTube.HalfZ)}, " +
                $"min_y = {N(SouthTube.MinY)}, max_y = {N(SouthTube.MaxY)}, comment = {S(SouthTube.Comment)} " +
                $"WHERE {Row(SeededX, SeededZ, SeededHalfX, SeededHalfZ)} AND min_y = -2000 AND max_y = 2000;";

            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";

            yield return $"UPDATE map_region SET pos_x = {N(SeededX)}, pos_y = {N(SeededY)}, pos_z = {N(SeededZ)}, half_x = {N(SeededHalfX)}, half_z = {N(SeededHalfZ)}, " +
                $"min_y = -2000, max_y = 2000, comment = {S(SeededComment)} " +
                $"WHERE {Row(SouthTube.X, SouthTube.Z, SouthTube.HalfX, SouthTube.HalfZ)};";
        }

        private static string Row(double x, double z, double halfX, double halfZ) =>
            $"id = {TubeRowId} AND map_context_id = {MapContextId} AND region_id = {TubeRegion} AND shape = {Box} " +
            $"AND ABS(pos_x - {N(x)}) < 0.01 AND ABS(pos_z - {N(z)}) < 0.01 AND ABS(half_x - {N(halfX)}) < 0.01 AND ABS(half_z - {N(halfZ)}) < 0.01";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
