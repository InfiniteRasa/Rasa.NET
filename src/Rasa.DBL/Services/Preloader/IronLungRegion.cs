using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Iron Lung, the tavern at Shadow's Edge Post on Ashen Desert (adv_arieki_ligo_ashendesert,
    /// 1734; Iron_lung_region).
    ///
    /// Region 23, "The Iron Lung" (priority 11: sky_arieki_general_human_interior,
    /// env_human_interior, REGION_HUMAN_BAR), had no volume: no label or mission names its place.
    /// It is the post's tavern, the map's one bar: arch_hum_fort_tavern_v02 at (-173, 296, -831),
    /// inside the post's fence west of the hospital, with two beer kegs and a row of beer bottles at its
    /// counter, a sink, tables and chairs, speakers and monitors (x -187 to -160, z -848 to -814,
    /// floor y 297). Its box is the building, y 294 to 311. No other region of the map is
    /// there.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out.
    /// </summary>
    public static class IronLungRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1734;
        public const uint IronLungRegionId = 23;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(IronLungRegionId, -188, -159, -849, -813, 294, 311, "arieki_ligo_ashendesert - The Iron Lung (Shadow's Edge Post tavern)"),
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
