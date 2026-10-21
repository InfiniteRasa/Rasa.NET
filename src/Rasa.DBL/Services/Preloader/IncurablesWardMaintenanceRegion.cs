using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Maintenance Corridors of the Incurables Ward (adv_arieki_ligo_crucible_incurablesward,
    /// 2141; Incurables_ward_maintenance_region).
    ///
    /// Region 6, "Maintenance Corridors" (priority 4: the Brann interior sky and env map,
    /// REGION_BRANN_INTERNAL_COORIDORS), had no volume. Engineer Dhimand's mission sends the
    /// player "into the Maintenance Corridors behind Jaikev's throne room" to the water
    /// processing system, and the map labels both: "Maintenance Tunnels" at (-234, 87, 222)
    /// and "Water Processing System" at (-366, 87, 212). They are the ward's west wing at y 86,
    /// off the throne room (the quarters at (-118, 94, 330), the "Jaikev's Throne Room" label):
    /// a grid of wrecked corridor crosses, airlocks and dorm rooms (x -298 to -110, z 142 to
    /// 314), and at its west end the four quarters of the water processing system (x -396 to
    /// -334, z 174 to 237), where most of the ward's pipework runs. Its boxes are the wing,
    /// x -400 to -140, z 140 to 316, and the airlock out of it towards the throne room,
    /// x -140 to -110, z 245 to 290, both y 82 to 104, clear of the throne room's floor
    /// (y 94, from z 298).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class IncurablesWardMaintenanceRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2141;
        public const uint MaintenanceCorridorsRegion = 6;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(MaintenanceCorridorsRegion, -400, -140, 140, 316, 82, 104, "arieki_ligo_crucible_incurablesward - Maintenance Corridors (west wing)"),
            Of(MaintenanceCorridorsRegion, -140, -110, 245, 290, 82, 104, "arieki_ligo_crucible_incurablesward - Maintenance Corridors (airlock)"),
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
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
