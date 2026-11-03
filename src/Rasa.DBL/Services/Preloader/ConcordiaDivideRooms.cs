using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Foreas Base's other buildings and the Bane Forward Command's rooms on Concordia Divide
    /// (adv_foreas_concordia_divide, 1148; Concordia_divide_rooms).
    ///
    /// - Concordia_divide_regions gave Foreas Base's hospital, headquarters, tavern and shops
    ///   their interiors. The base's other buildings take its three remaining interiors by size:
    ///   the garage (arch_hum_fort_garage, the largest, 19 by 51 m) region 11 (the human interior,
    ///   MUSIC_SAFE_ZONE_AFS_BASE, REGION_GENERIC_ROOM_TONE_HEAVY); the prison
    ///   (arch_hum_fort_prison, the next, 32 by 26 m) region 32 (the human interior,
    ///   REGION_GENERIC_ROOM_TONE_MEDIUM); the two barracks (arch_hum_fort_barracks) region 19
    ///   (REGION_HUMAN_GUILD_HALL) for their sound and region 16 (the human interior with the heavy
    ///   room tone, under 19) for their light. Each box is the building from its floor to under
    ///   its roof (the barracks' roofs, at 132, stay outside).
    /// - Region 29 (sky_bane_interior_default, env_bane_interior_default, MUSIC_CONCORDIA_DIVIDE_03,
    ///   REGION_BANE_INTERIOR_01) has the minimap rectangle of the Bane Forward Command's rooms
    ///   under its tower (arch_bane_industrial_room_control_24m, chunnels, overlooks and the
    ///   tunnels up), seeded from -2000 to 2000, so the Bane interior sky was over the Bane camp
    ///   on the ground above it too. As Region_floor_ceilings did for Foreas Base's lower levels,
    ///   the rectangle keeps its x and z and gets the rooms' height: their floors from 68, their
    ///   ceilings to 100, a metre either side; the camp above is at 104 and up.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot, and the rectangle changes only while it is as
    /// seeded; Down takes the rows out and gives the rectangle its full height again.
    /// </summary>
    public static class ConcordiaDivideRooms
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1148;
        public const uint GarageRegion = 11, BarracksLightRegion = 16, BarracksRegion = 19, BaneCommandRegion = 29, PrisonRegion = 32;
        public const byte Box = 2, AnyLevel = 0;

        /// <summary>Region 29's minimap rectangle as seeded (MapRegionPreloader row 1) and the height it gets.</summary>
        public const uint BaneCommandRowId = 1;
        public const double BaneCommandX = -26, BaneCommandZ = -589, BaneCommandHalfX = 74.5, BaneCommandHalfZ = 98;
        public const double SeededMinY = -2000, SeededMaxY = 2000, BaneCommandMinY = 67, BaneCommandMaxY = 101;

        private const string Map = "foreas_concordia_divide - ";

        public static readonly Volume[] Volumes =
        {
            Of(GarageRegion, -128, -109, 436, 487, 114, 121, Map + "region 11 (Foreas Base garage)"),
            Of(PrisonRegion, 27, 59, 438, 464, 122, 135, Map + "region 32 (Foreas Base prison)"),
            Of(BarracksRegion, -143, -113, 513, 539, 122, 131, Map + "region 19 (Foreas Base south barracks)"),
            Of(BarracksRegion, -141, -112, 545, 571, 122, 131, Map + "region 19 (Foreas Base north barracks)"),
            Of(BarracksLightRegion, -143, -113, 513, 539, 122, 131, Map + "region 16 (Foreas Base south barracks)"),
            Of(BarracksLightRegion, -141, -112, 545, 571, 122, 131, Map + "region 16 (Foreas Base north barracks)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            yield return $"UPDATE map_region SET min_y = {N(BaneCommandMinY)}, max_y = {N(BaneCommandMaxY)} WHERE {BaneCommandRectangle} " +
                $"AND min_y = {N(SeededMinY)} AND max_y = {N(SeededMaxY)};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE map_region SET min_y = {N(SeededMinY)}, max_y = {N(SeededMaxY)} WHERE {BaneCommandRectangle} " +
                $"AND ABS(min_y - {N(BaneCommandMinY)}) < 0.01 AND ABS(max_y - {N(BaneCommandMaxY)}) < 0.01;";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string BaneCommandRectangle =>
            $"id = {BaneCommandRowId} AND map_context_id = {MapContextId} AND region_id = {BaneCommandRegion} AND shape = {Box} " +
            $"AND ABS(pos_x - {N(BaneCommandX)}) < 0.01 AND ABS(pos_z - {N(BaneCommandZ)}) < 0.01 " +
            $"AND ABS(half_x - {N(BaneCommandHalfX)}) < 0.01 AND ABS(half_z - {N(BaneCommandHalfZ)}) < 0.01";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
