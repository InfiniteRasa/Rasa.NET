using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Foreas Base's wormhole on Concordia Divide (adv_foreas_concordia_divide, 1148;
    /// Foreas_base_wormhole_region).
    ///
    /// Region 23, "Foreas Base: Wormhole" (priority 3: REGION_HUMAN_BASE_DISTANT_BATTLE_01 and
    /// MUSIC_SAFE_ZONE_AFS_BASE, like Foreas Base's, and its own name), had no volume. The
    /// wormhole is not in the .map: it is the server's teleporter 343, "Wormhole: Concordia
    /// Divide" (UsableTwoStateHumWormhole, prop_hum_wormhole_v02, 27 m across), at
    /// (-35.1, 115.5, 765.4), with the client's "Wormhole: Foreas Base" marker beside it. It
    /// stands in a clearing at y 116 north of the base, past the end of Foreas Base's 200 m
    /// circle (which reaches z 679): a holographic fence with lights, crates, barrels and a
    /// loudspeaker on its west side (x -81 to -53, z 737 to 768), a Forean shrine with a logos
    /// dispenser and a pond on its east (x -11 to 53), the cliff rising behind it at z 790, and
    /// the track from the base coming up the gully at x -100 to -55. Its box is the clearing
    /// and the top of the track, x -100 to 55, z 700 to 792, y 105 to 140.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out as it went in.
    /// </summary>
    public static class ForeasBaseWormholeRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1148;
        public const uint WormholeRegion = 23;
        public const byte Box = 2;

        public static readonly Volume Clearing = Of(WormholeRegion, -100, 55, 700, 792, 105, 140, "foreas_concordia_divide - Foreas Base: Wormhole (clearing)");

        public static readonly Volume[] Volumes = { Clearing };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
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
