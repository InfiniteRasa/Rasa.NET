using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Abyssal Zone below the Abyssal Shelf (adv_arieki_torden_abyss, 2028;
    /// Abyssal_zone_region).
    ///
    /// Region 39 names "The Abyssal Zone" (the Torden Plains string) and sets only a dark
    /// falling sky and dark blue fog (sky_dark_fall_test, env_heavy_dark_bluefog); it has the
    /// map's second-highest priority. The soldiers at Icarus Post say what it is: "They call
    /// this chasm 'The Abyssal Zone'", "watch your step out near the edge", and the zone
    /// description puts Torden Abyss "directly adjacent to the seemingly bottomless chasm known
    /// as The Abyssal Zone". The chasm is the open drop south of the Abyssal Shelf: the shelf's
    /// floor is at y 519 to 547 and ends at about z -500 to -600 (its spit runs out to the
    /// Icarus Watchtower at z -700), the chasm's cloud layer (vfx_env_arieki_cloud_plane,
    /// y 378 to 581) fills x -572 to 373 and z -885 to -513 below it, and the only ground under
    /// it is the map's floor at y 0, which runs across the whole map south of z -500 and up to
    /// z -350 in the south-east corner.
    ///
    /// So the region is the chasm below the shelf: x -1000 to 1000, z -1000 to -500, and
    /// x 300 to 1000, z -500 to -340, from y -50 up to 510. That stays under the lowest point of
    /// the shelf (518.7) and of the cave floors north of the edge (515), and clear of the lower
    /// cave at y 183 around (-325, -425); nothing else walkable lies in it but the chasm floor.
    /// Anyone who goes over the edge is in it about 10 m down; everyone on the shelf keeps the
    /// shelf's own regions.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class AbyssalZoneRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2028;
        public const uint ZoneRegion = 39;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(ZoneRegion, -1000, 1000, -1000, -500, -50, 510, "arieki_torden_abyss - The Abyssal Zone chasm"),
            Of(ZoneRegion, 300, 1000, -500, -340, -50, 510, "arieki_torden_abyss - The Abyssal Zone chasm, south-east"),
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
