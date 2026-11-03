using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The drop off Torden Mires' southern cliffs (adv_arieki_torden_mires, 1759;
    /// Torden_mires_dark_fall_region).
    ///
    /// Region 5 (sky_dark_fall_test alone; priority 9, no name) had no volume.
    /// sky_dark_fall_test is the dark sky the maps give the depths a player falls into (the
    /// Abyssal Zone under Torden Abyss' cliffs, Eloh Vale's canyon, the drop below Guardian
    /// Prominence's clouds). The Mires plateau ends in cliffs along its whole south side, from
    /// (-1000, -480) round to (950, -440) by way of (0, -740), its edge at 185 to 250, and
    /// 153 cloud planes (vfx_env_arieki_cloud_plane) and 126 cloud banks lie along them from
    /// 80 to 280, most at 110 to 200. Under them the navmesh has the abyss floor at 0 to 20
    /// and the tops of the rock pillars (terra_arieki_rock_vista) that stand in it. No floor of
    /// the plateau, its bases or its tunnels is below 180 (the lowest is the Orsa basin, at
    /// 185). The box is the whole map below 150: the abyss and the lower pillars, under most of
    /// the cloud layer, and nothing a player stands on above the cliffs.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out.
    /// </summary>
    public static class TordenMiresDarkFallRegion
    {
        public const uint MapContextId = 1759;
        public const uint DarkFallRegion = 5;
        public const byte Box = 2, AnyLevel = 0;

        public const double West = -1100, East = 1050, South = -1100, North = 950, Floor = -100, Roof = 150;
        public const string Comment = "arieki_torden_mires - region 5 (the drop below the southern cliffs)";

        private static double X => (West + East) / 2;
        private static double Z => (South + North) / 2;
        private static double HalfX => (East - West) / 2;
        private static double HalfZ => (North - South) / 2;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                $"SELECT {MapContextId}, {DarkFallRegion}, {Box}, {N(X)}, {N(Floor)}, {N(Z)}, 0, {N(HalfX)}, {N(HalfZ)}, {N(Floor)}, {N(Roof)}, {AnyLevel}, 1, {S(Comment)} " +
                $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At("x")});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM map_region WHERE {At("map_region")} AND ABS(half_x - {N(HalfX)}) < 0.01 AND ABS(half_z - {N(HalfZ)}) < 0.01 " +
                $"AND ABS(min_y - {N(Floor)}) < 0.01 AND ABS(max_y - {N(Roof)}) < 0.01 AND comment = {S(Comment)};";
        }

        private static string At(string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {DarkFallRegion} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(X)}) < 0.01 AND ABS({t}.pos_z - {N(Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
