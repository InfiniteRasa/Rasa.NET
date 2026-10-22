using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Avernus Outpost's entrance corridor (adv_arieki_ligo_ashendesert_avernusoutpost, 2110;
    /// Avernus_entrance_region).
    ///
    /// Of the outpost's regions still without a volume, only region 3 is a place: it sets the
    /// human interior sky and environment map (sky_foreas_general_human_interior,
    /// env_human_interior) and nothing else. The fort's buildings (headquarters, tavern, shops,
    /// prison) all stand inside region 4's rectangle, which is one priority higher and would
    /// outrank it; the one human interior outside it is the corridor from the way in (map link
    /// at (84, 528.5, 107.7)) through the mesa to the fort: an endcap,
    /// two doorjambs and two turns (arch_hum_corridor_*) under fluorescent lights, with a
    /// stretcher, medical crate and sandbags, out to its entrance (arch_hum_corridor_entrance_v04
    /// at (100, 524, 68)) on the fort's north side. The navmesh calls it underground. Its box is
    /// x 77 to 107, z 51 to 115, y 524 to 545; the fort's own region 4 (the Ashen Desert sky,
    /// one priority higher, its minimap rectangle to z 72) keeps the entrance's mouth.
    ///
    /// The others are not places: region 1 (an Atta cave sky, lowest priority, nothing else) is
    /// the editor's base region, and regions 6 to 9 set nothing (minimap rectangles).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out.
    /// </summary>
    public static class AvernusEntranceRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2110;
        public const uint CorridorRegion = 3;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(CorridorRegion, 77, 107, 51, 115, 524, 545, "arieki_ligo_ashendesert_avernusoutpost - entrance corridor"),
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
