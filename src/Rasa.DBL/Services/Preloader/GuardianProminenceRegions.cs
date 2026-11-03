using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Guardian Prominence's caves, its ruins and the drop below it
    /// (adv_foreas_concordia_wilderness_guardianprom, 1416; Guardian_prominence_regions).
    ///
    /// The map is a mountain top over the clouds: the plateau with the Temple of the Ages, Mount
    /// Epoch and two crashed dropships is walked at 303 to 357, the cloud planes
    /// (vfx_env_foreas_cloud_plane) lie round its cliffs at 257 to 292, and under them the
    /// mountain's flanks run down to 171. The instance puts you in a cave east of the plateau
    /// (map link 36 at (163.5, 314.3, 200.3), the First Aid Station beside it). The map's own
    /// region, "Guardian Prominence", has the mountain's own sky. Of the rest:
    ///
    /// - Region 12, also "Guardian Prominence" (sky_dark_fall_test and the name, nothing else, the
    ///   highest priority), had no volume. sky_dark_fall_test is the dark sky the maps give the
    ///   depths a player falls into (the Abyssal Zone under Torden Abyss' cliffs, Eloh Vale's
    ///   canyon); here it is everything under the clouds, where a player who goes over the edge
    ///   ends up, still named for the mountain. Its box is the whole map below 296, under every
    ///   floor of the plateau (the lowest, the cave by the temple, is at 303).
    /// - Prominence Ruins (region 11: the cave hall sky, the cave environment map, music and
    ///   echo) is the roofed cavern of Eloh ruins north-east of the plateau, under
    ///   terra_foreas_cavern_roof_500m_x_280m: its floor is x 150 to 300, z 238 to 274 at 326 to
    ///   334, between the Eloh cavern entrances at its west and east ends (x 140 to 163 and 261
    ///   to 284), the way to the Chamber of Eternity. Its label circle (115 m round (185.4,
    ///   244.4), surface-only) also took the plateau south of it, between the cavern and the
    ///   cave of the way in, and put the cave's sky over open ground.
    ///   It becomes a box over the cavern, x 140 to 290, z 230 to 282, y 318 to 360.
    /// - Region 2 (the same cave settings, no name) is the other caves: the one the instance puts
    ///   you in, with the passage up to the ruins' west entrance (x 90 to 175, z 148 to 266; the
    ///   minimap rectangles of regions 4 and 6), and the cave under the temple's north wall where
    ///   Professor Anand and the Callisto's survivors are (stretchers, footlockers, sleeping bags
    ///   at (-55, 303, 79); "the caves near the Callisto crash site"; regions 7 and 8's
    ///   rectangles), x -134 to -35, z 60 to 160. Both boxes are underground-only, as the caves are
    ///   and the plateau over them is not.
    ///
    /// Region 1 (Molder.sky) is the editor's base region and is left alone.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; the circle changes only while it is as seeded.
    /// Down puts it back and takes the new rows out.
    /// </summary>
    public static class GuardianProminenceRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>A seeded label circle (surface-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 1416;
        public const uint CaveRegion = 2, RuinsRegion = 11, BelowRegion = 12;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_concordia_wilderness_guardianprom - ";

        public static readonly Volume[] Volumes =
        {
            Of(BelowRegion, -330, 480, -260, 400, 100, 296, AnyLevel, Map + "Guardian Prominence (under the clouds)"),
            Of(CaveRegion, 90, 175, 148, 266, 300, 365, UndergroundOnly, Map + "region 2 (the cave of the way in)"),
            Of(CaveRegion, -134, -35, 60, 160, 295, 340, UndergroundOnly, Map + "region 2 (Professor Anand's cave)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(78, RuinsRegion, 185.4, 325.8, 244.4, 115, Map + "Prominence Ruins",
                Of(RuinsRegion, 140, 290, 230, 282, 318, 360, AnyLevel, Map + "Prominence Ruins (the roofed cavern)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {r.Box.Underground}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {SurfaceOnly};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
