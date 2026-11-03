using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Bane shard's fog down the Fithik Trench on Palisades (adv_foreas_concordia_palisades,
    /// 1244; Palisades_fithik_trench_region).
    ///
    /// Region 14 (env_foreas_concordia_wilderness, MUSIC_CONCORDIA_PALISADES_03,
    /// REGION_BANE_TERRITORY_01; priority 25, no name) had no volume. Its settings are the Bane
    /// Shard's (region 16) without the name. The Bane shard (arch_bane_shard, (172, 84, -536)) stands
    /// at the bottom of the Fithik Trench, over the tunnel into the Devil's Den
    /// (arch_bane_shard_tunnel_ramp and _instance, the link at (160, 63, -581)), and its fog
    /// (vfx_env_bane_shard_groundfog, 30 of them) lies along the trench floor from the shard's
    /// foot (169, 77, -541) up to (246, 83, -230), between its spikes
    /// (arch_bane_shard_spike) and the Fithik egg clusters: "the Fithik-infested Bane shard",
    /// whose interior is the Devil's Den, "down the Fithik Trench". The trench floor is at 63 to
    /// 100 and the ground either side at 105 and up. Region 14 gets that floor, from the shard's
    /// foot to the fog's end, in three boxes along the trench, under 100; over the Fithik
    /// Trench's own region (4, priority 18) it plays the shard's Bane territory sound and music,
    /// and the name stays the trench's.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class PalisadesFithikTrenchRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1244;
        public const uint ShardFogRegion = 14;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_palisades - ";

        public static readonly Volume[] Volumes =
        {
            Of(ShardFogRegion, 128, 235, -600, -430, 60, 100, Map + "region 14 (Fithik Trench at the Bane shard's foot)"),
            Of(ShardFogRegion, 145, 252, -430, -330, 60, 100, Map + "region 14 (Fithik Trench, middle)"),
            Of(ShardFogRegion, 178, 282, -330, -220, 60, 100, Map + "region 14 (Fithik Trench, to the fog's end)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
