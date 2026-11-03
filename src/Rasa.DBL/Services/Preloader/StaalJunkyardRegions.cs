using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Staal Junkyard's sectors and Labbna Outpost (adv_arieki_ligo_staaljunkyard, 2138;
    /// Staal_junkyard_regions).
    ///
    /// The map was made from the Ustor yard on Foreas: its own region still has
    /// sky_foreas_foreas_ustor_yard, and the Ustor yard's sky fog (vfx_env_ustoryard_skyfog) is
    /// still placed in it. Its three named regions, West Melthouse Sector (4), East Melthouse
    /// Sector (5) and Labbna Outpost (6), each set the junkyard's own Crucible sky,
    /// env_heavy_brown_fog and REGION_JUNKYARD, and none had a volume, so the whole junkyard had
    /// the Foreas yard's sky.
    ///
    /// - Labbna Outpost is the Brann outpost on the west terrace, under the map's "Labbna Outpost"
    ///   label (-392, 266, 145): two domes, the Brann corridors and entrances between them, a field
    ///   tent, solar towers and relays, inside Brann prison walls: the Labbna syndicate's base in
    ///   the junkyard their salvage crews work (Junkyard Dogs: "We of the Labbna, our business is
    ///   salvage"). Its box is the outpost, x -476 to -280, z 15 to 250, from 255 to 320 (under
    ///   the terrace east of it, at 331).
    /// - The two Melthouse Sectors share every setting but the name, and nothing on the map is
    ///   labelled for either (no melthouse, arch_brann_melthouse_v01, is placed). The junkyard is
    ///   one bowl from x -470 to 450, mirrored about its middle: a lava field in each southern
    ///   corner, the Burning Steps entrance (29, 241, -337) at the south and the prison at the
    ///   top of the terraces (18, 334, 337). West Melthouse Sector gets the bowl west of x -20 and
    ///   East Melthouse Sector the bowl east of it, with the entrance and the prison; where the
    ///   line falls changes only the name shown.
    ///
    /// Not placed: region 7 (sky_dark_fall_test alone; priority 1, under all three): the bowl's
    /// lowest floor is the lava beds at 220, and nothing on the map is a drop.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class StaalJunkyardRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2138;
        public const uint WestRegion = 4, EastRegion = 5, LabbnaRegion = 6;
        public const byte Box = 2, AnyLevel = 0;

        public const double Divide = -20;

        private const string Map = "arieki_ligo_staaljunkyard - ";

        public static readonly Volume[] Volumes =
        {
            Of(WestRegion, -520, Divide, -520, 520, 200, 420, Map + "West Melthouse Sector (the bowl west of x -20)"),
            Of(EastRegion, Divide, 520, -520, 520, 200, 420, Map + "East Melthouse Sector (the bowl east of x -20)"),
            Of(LabbnaRegion, -476, -280, 15, 250, 255, 320, Map + "Labbna Outpost (the Brann outpost on the west terrace)"),
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
