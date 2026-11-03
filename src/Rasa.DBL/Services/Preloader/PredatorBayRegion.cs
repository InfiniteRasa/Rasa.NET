using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Timora Mines' Predator Bay (adv_foreas_concordia_divide_timoramines, 1348;
    /// Predator_bay_region).
    ///
    /// Of the mines' regions, two had no volume. Region 1 (Molder.sky, lowest priority, nothing
    /// else) is the editor's base region, not a place; the whole map is region 0's, Timora Mines.
    /// Region 8, "Predator Bay" (priority 7: sky_foreas_bootcamp, env_bootcamp), is named in no
    /// mission, but the map has one bay: the round lobe west of the Expeditionary Excavation,
    /// ringed by cliffs and open to the sky, which holds nothing but a Bane Predator launch pad
    /// (arch_bane_outpost_launch_predator at (-343, 216, 279), x -418 to -270, z 198 to 349). Its
    /// floor is 211 to 227 and its ledges climb to about 265; a narrow neck by a Bane pillbox
    /// (x -234 to -197) joins it to the excavation, whose circle keeps the neck. The box is the
    /// lobe, x -425 to -245, z 190 to 355, y 195 to 280.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out.
    /// </summary>
    public static class PredatorBayRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1348;
        public const uint PredatorBay = 8;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(PredatorBay, -425, -245, 190, 355, 195, 280, "foreas_concordia_divide_timoramines - Predator Bay (the launch pad's lobe)"),
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
