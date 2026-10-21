using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Eir Crater Field Hospital on Torden Plains and the Pirolja Midja Terrorist Base on
    /// Torden Incline (Eir_crater_pirolja_midja_regions).
    ///
    /// Torden Plains (1764) region 21, "Eir Crater Field Hospital" (priority 10:
    /// sky_arieki_general_human_interior, env_human_interior, REGION_HUMAN_HOSPITAL,
    /// MUSIC_SAFE_ZONE_AFS_OUTPOST), had no volume. It is the small AFS bunker sunk at the west
    /// of Eir Crater Outpost (whose circle, region 19, is round the crater's label at
    /// (743.7, 236.2)): a corridor entrance and door at x 705 to 725 into a cross of human
    /// corridor pieces (x 683 to 705, z 222 to 250, floor y 414) with four beds and medical
    /// crates, where the "Hospital: Eir Crater Field" revive point stands (690.3, 413.5, 235.9).
    /// It is under the terrain, where the outpost's circle (surface only) does not reach. Its
    /// box is the bunker, x 682 to 726, z 221 to 251, y 410 to 427.
    ///
    /// Torden Incline (1761) region 20, "Pirolja Midja Terrorist Base" (priority 17, the name
    /// only), had no volume. Operation: Cauterize sends the player to plant a Bane GPS
    /// transponder "within the Pirolja Midja terrorist base north-east of Ortho Base", and
    /// north-east of Ortho (391.7, 33.0), past the Labbna Syndicate Camp, the one camp is a
    /// Brann one at y 310 on the high ground over the lava falls: tents with Brann beds, desks,
    /// a computer, lockers, crates, beer kegs and a toilet, two Brann relay towers, a solar
    /// tower and a wrecked AFS outpost's fences and generators with sandbags (x 880 to 1000,
    /// z 440 to 595), and Brann spotlights along the cliff edge east of it (x 1000 to 1040).
    /// Its box is the camp, x 870 to 1045, z 425 to 600, y 295 to 335.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where its map is
    /// loaded and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class EirCraterPiroljaMidjaRegions
    {
        public sealed record Volume(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint PlainsMapContextId = 1764, InclineMapContextId = 1761;
        public const uint FieldHospitalRegion = 21, TerroristBaseRegion = 20;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(PlainsMapContextId, FieldHospitalRegion, 682, 726, 221, 251, 410, 427, "arieki_torden_plains - Eir Crater Field Hospital (bunker)"),
            Of(InclineMapContextId, TerroristBaseRegion, 870, 1045, 425, 600, 295, 335, "arieki_torden_incline - Pirolja Midja Terrorist Base (camp)"),
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
