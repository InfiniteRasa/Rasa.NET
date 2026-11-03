using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Ashen Desert's two unnamed interior regions put where their rooms stand
    /// (adv_arieki_ligo_ashendesert, 1734; Ashen_desert_interior_regions).
    ///
    /// - Region 4 (sky_arieki_general_human_interior, env_human_interior, MUSIC_ASHEN_DESERT_04;
    ///   priority 9) had no volume. Its settings are Shadow's Edge Post's: the post's music, and
    ///   the human interior sky and env map that its other two interior regions have, the Iron
    ///   Lung (23, the tavern, Iron_lung_region) and the post's own region 17 (the corridors
    ///   under it, the minimap rectangle). It is the post's other buildings, which had the open
    ///   desert's sky indoors: the two barracks (arch_hum_fort_barracks_v02), the hospital
    ///   (arch_hum_outpost_hospital_v02) and the armor and weapon shops (arch_hum_fort_shop_*),
    ///   a box round each. Over the post's surface it is under the post's region 5, which sets
    ///   no sky or env map and plays the same music. The two gate bunkers north of the post are
    ///   left out: the navmesh has their roofs, not a room in them.
    /// - Region 3 (sky_arieki_cave_sulfur_default, env_heavy_brown_fog; priority 13) had no
    ///   volume either. The map's only cave pieces are the three tunnels its links out are at
    ///   the end of (terra_arieki_ligo_mesa_cave_entrance, terra_arieki_ligo_cavern_*,
    ///   terra_arieki_cavern_rocky_instance_01): to Thunderhead, to the Indra Caverns and to
    ///   Avernus Outpost. They lit as the open desert. Each box is the tunnel's pieces and is
    ///   underground-only, so it takes the tunnel and leaves its mouth in the open.
    ///
    /// Not placed: region 2 (sky_arieki_default alone; priority 14), which nothing ties to a
    /// place, and regions 1 (Molder.sky) and 24 (nothing).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class AshenDesertInteriorRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1734;
        public const uint CaveRegion = 3, PostInteriorRegion = 4;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "arieki_ligo_ashendesert - ";

        public static readonly Volume[] Volumes =
        {
            Of(PostInteriorRegion, -111, -82, -805, -779, 294, 310, AnyLevel, Map + "region 4 (Shadow's Edge Post east barracks)"),
            Of(PostInteriorRegion, -175, -146, -805, -779, 294, 310, AnyLevel, Map + "region 4 (Shadow's Edge Post west barracks)"),
            Of(PostInteriorRegion, -106, -80, -835, -815, 294, 309, AnyLevel, Map + "region 4 (Shadow's Edge Post hospital)"),
            Of(PostInteriorRegion, -98, -69, -720, -701, 294, 310, AnyLevel, Map + "region 4 (Shadow's Edge Post armor shop)"),
            Of(PostInteriorRegion, -183, -161, -719, -699, 294, 316, AnyLevel, Map + "region 4 (Shadow's Edge Post weapon shop)"),
            Of(CaveRegion, 71, 147, -964, -931, 310, 336, UndergroundOnly, Map + "region 3 (tunnel to Thunderhead)"),
            Of(CaveRegion, 263, 331, 287, 352, 310, 340, UndergroundOnly, Map + "region 3 (tunnel to the Indra Caverns)"),
            Of(CaveRegion, -689, -612, -280, -240, 315, 345, UndergroundOnly, Map + "region 3 (tunnel to Avernus Outpost)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
