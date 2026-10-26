using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Arioch's Lair (adv_foreas_concordia_wilderness, 1220; Ariochs_lair_region).
    ///
    /// Region 37 names Arioch's Lair and sets the foggy Wilderness environment map, rocky
    /// highland ambience and its own music (env_foreas_concordia_wilderness_foggy,
    /// REGION_HIGHLANDS_ROCKY, MUSIC_CONCORDIA_WILDERNESS_03); the map has no label for it.
    /// Xanx For The Memories places it: Apprentice Juvak and Shaman Lumin, bound from Alia Das,
    /// "took shelter from the rain in a canyon near the path to Guardian Prominence", and there
    /// the elder Xanx Arioch "tore him limb from limb"; the Codex "was lost in Arioch's Lair".
    ///
    /// That canyon is the dead-end gorge east of the Guardian Prominence highland. Its floor is
    /// at y 246 to 253 between walls rising to 280-340: a bowl at its head (x 68 to 158,
    /// z 700 to 768) and a channel (x 55 to 100, z 655 to 700) down to its mouth, where it opens
    /// onto the lowland at y 235-240. In the bowl stand the one wrecked Forean shelter on this
    /// stretch (arch_forean_hut_small_destroyed at (125, 246, 725), with debris, a log, a stump
    /// and broken trees round it), and Arioch's own spawn pool (580011, at (125.4, 246.8, 733.1),
    /// where the fork had placed creature 77). The boxes are the bowl and the channel, from just
    /// under the floor (235, and 230 in the channel) to 275: well over the floor and under the
    /// rims and the ledge on the bowl's south lip (277 and up). The navmesh puts the floor in the
    /// open, but the boxes are for any level.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class AriochsLairRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1220;
        public const uint LairRegion = 37;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(LairRegion, 68, 158, 700, 768, 235, 275, "foreas_concordia_wilderness - Arioch's Lair canyon head"),
            Of(LairRegion, 55, 100, 655, 700, 230, 275, "foreas_concordia_wilderness - Arioch's Lair canyon"),
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
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
