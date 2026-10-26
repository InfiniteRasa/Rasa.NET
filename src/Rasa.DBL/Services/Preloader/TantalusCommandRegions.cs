using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Tantalus High Command and the C.I.D. Headquarters in Tantalus Base on Torden Abyss
    /// (adv_arieki_torden_abyss, 2028; Tantalus_command_regions).
    ///
    /// Regions 28, "Tantalus High Command" (priority 10), and 29, "C.I.D. Headquarters"
    /// (priority 11), both REGION_HUMAN_BASE_COMMAND and their names, had no volumes. They are
    /// the two floors of the one office building in Tantalus Base, south-west of the dropship
    /// yard (arch_hum_corr_room_office_v07 at (-442, 536, -278), x -452 to -424, z -310 to -242),
    /// each named by its own holographic sign: ArchHumBaseHolosignCID (prop_hum_sign_holo_v119)
    /// on the ground floor's wall at (-436, 538, -284), with its workstations, monitors and
    /// table at y 536 to 538; and ArchHumBaseHolosignTantalusCommand (prop_hum_sign_holo_v120)
    /// on the upper floor's wall at (-430, 547, -270), with the rows of workstations, the
    /// midscreen and the base flag at y 544. General Perry, whom the Penumbra missions send the
    /// player to "at Tantalus High Command", belongs upstairs; the base's other two mobile signs
    /// for them stand at the yard (-440, 536, -160). The floors share the footprint, so the two
    /// boxes are split at y 541: the C.I.D. Headquarters from 534, Tantalus High Command to 556.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class TantalusCommandRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2028;
        public const uint HighCommandRegion = 28, CidHeadquartersRegion = 29;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(CidHeadquartersRegion, -453, -423, -311, -241, 534, 541, "arieki_torden_abyss - C.I.D. Headquarters (office ground floor)"),
            Of(HighCommandRegion, -453, -423, -311, -241, 541, 556, "arieki_torden_abyss - Tantalus High Command (office upper floor)"),
        };

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
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01 AND ABS({t}.min_y - {N(v.MinY)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
