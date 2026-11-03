using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Concordia Divide's unnamed interiors: Foreas Base's buildings, the Eloh corridors, the caves
    /// and the Bane tunnel to Purgas Station (adv_foreas_concordia_divide, 1148;
    /// Concordia_divide_regions).
    ///
    /// Foreas Base (-42, 116, 479) has the fort buildings on its upper level and two levels of
    /// rooms under it (regions 34 and 35, Region_floor_ceilings). Its interior regions had no
    /// volume, so every building had the open sky:
    ///
    /// - Region 13 (sky_foreas_general_human_interior, env_human_interior, MUSIC_SAFE_ZONE_AFS_BASE,
    ///   REGION_HUMAN_HOSPITAL) is the base's hospital (arch_hum_fort_hospital, the "Hospital:
    ///   Foreas Base" safe zone and its medical vendor); region 33 (the human interior,
    ///   REGION_HUMAN_BASE_COMMAND) the headquarters (arch_hum_fort_headqtrs, the trainer in it);
    ///   The Swamp (region 12: the human interior, REGION_HUMAN_BAR and the name) the tavern
    ///   (arch_hum_fort_tavern), the base's one bar. Each gets a box of its building, from the
    ///   floor to under its roof.
    /// - Region 18 (REGION_HUMAN_GENERAL_STORE alone) is the armor and weapon shops
    ///   (arch_hum_fort_shop_armor and _weapon, the base's armor and weapon vendors), and region
    ///   14 (the human interior sky and env map, nothing else, priority 13) gives them their
    ///   light.
    /// - Region 37 (REGION_HUMAN_BASE_COMMAND alone; priority 35, over the lower level's 34) is the
    ///   office on the lower level (arch_hum_corr_room_office, x -81 to -15, z 494 to 513).
    ///
    /// - Region 36 (sky_eloh_interior_default, env_eloh_interior_default, MUSIC_ELOH_01; priority
    ///   32) is the map's three Eloh corridors (arch_eloh_corridor_*, each with a logos dispenser),
    ///   as on the other maps: in the west (x -809 to -785, z 440 to 490), the north (x 346 to
    ///   370, z 589 to 639) and the east (x 796 to 879, z 496 to 538), each at the end of a cave.
    /// - Region 9 (sky_foreas_cave_short, env_foreas_cave_interior, MUSIC_FOREAS_CONCORDIA_CAVE_1,
    ///   REGION_FOREAS_CAVE_WITH_ECHO; priority 31) is the map's caves (terra_foreas_cavern_*):
    ///   the tunnels to the Wilderness (two), the Palisades (two) and Minos Caverns, the three
    ///   caves leading to the Eloh corridors, and the crystal cave east of Benefactor Valley. Each
    ///   box is the cave's pieces and is underground-only, so it stops at the mouth.
    /// - Region 8 (sky_bane_interior_default, env_bane_interior_default, MUSIC_CONCORDIA_DIVIDE_03,
    ///   REGION_BANE_INTERIOR_01; priority 30, no name) is the settings of region 29, the Bane
    ///   Forward Command's rooms under its tower. It is the other Bane interior: the tunnel under
    ///   the Bane tower in Purgas Valley that the link to Purgas Station is at the end of
    ///   (arch_bane_industrial_chunnel_* and _tunnel_*), x -892 to -849, z -912 to -869.
    ///
    /// Not placed: regions 11, 16, 19 and 32, which Concordia_divide_rooms gives the base's other
    /// buildings, and 15 and 17 (the heavy room tone alone); 31 (the human underground sky with outpost ambience); 28 (Eloh ruins ambience
    /// and music); and the outdoor 2, 3, 4, 5, 6 and 7, which the map's area labels (Northwestern
    /// Highlands, Benefactor Valley, Purgas Valley, The Front Lines) may belong to but nothing
    /// bounds.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class ConcordiaDivideRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1148;
        public const uint BaneTunnelRegion = 8, CaveRegion = 9, SwampRegion = 12, HospitalRegion = 13, ShopLightRegion = 14, ShopRegion = 18,
            HeadquartersRegion = 33, ElohRegion = 36, OfficeRegion = 37;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "foreas_concordia_divide - ";

        public static readonly Volume[] Volumes =
        {
            Of(HospitalRegion, -22, 32, 510, 554, 114, 133, AnyLevel, Map + "region 13 (Foreas Base hospital)"),
            Of(HeadquartersRegion, -76, -8, 419, 473, 114, 141, AnyLevel, Map + "region 33 (Foreas Base headquarters)"),
            Of(SwampRegion, 44, 78, 522, 550, 122, 131, AnyLevel, Map + "The Swamp (Foreas Base tavern)"),
            Of(ShopRegion, 1, 30, 476, 496, 114, 122, AnyLevel, Map + "region 18 (Foreas Base armor shop)"),
            Of(ShopRegion, -102, -80, 476, 496, 114, 124, AnyLevel, Map + "region 18 (Foreas Base weapon shop)"),
            Of(ShopLightRegion, 1, 30, 476, 496, 114, 122, AnyLevel, Map + "region 14 (Foreas Base armor shop)"),
            Of(ShopLightRegion, -102, -80, 476, 496, 114, 124, AnyLevel, Map + "region 14 (Foreas Base weapon shop)"),
            Of(OfficeRegion, -81, -15, 494, 513, 46, 72, AnyLevel, Map + "region 37 (Foreas Base lower level office)"),
            Of(ElohRegion, -809, -785, 440, 490, 118, 132, AnyLevel, Map + "region 36 (Eloh corridor, west)"),
            Of(ElohRegion, 346, 370, 589, 639, 83, 97, AnyLevel, Map + "region 36 (Eloh corridor, north)"),
            Of(ElohRegion, 796, 879, 496, 538, 153, 167, AnyLevel, Map + "region 36 (Eloh corridor, east)"),
            Of(CaveRegion, -1017, -947, 637, 681, 170, 220, UndergroundOnly, Map + "region 9 (tunnel to the Wilderness, west)"),
            Of(CaveRegion, 438, 508, 1182, 1217, 167, 197, UndergroundOnly, Map + "region 9 (tunnel to the Wilderness, north)"),
            Of(CaveRegion, 753, 848, -514, -460, 126, 175, UndergroundOnly, Map + "region 9 (tunnel to the Palisades, north)"),
            Of(CaveRegion, 802, 846, -799, -705, 70, 119, UndergroundOnly, Map + "region 9 (tunnel to the Palisades, south)"),
            Of(CaveRegion, 630, 680, -146, -101, 155, 203, UndergroundOnly, Map + "region 9 (tunnel to Minos Caverns)"),
            Of(CaveRegion, -811, -768, 489, 562, 110, 160, UndergroundOnly, Map + "region 9 (cave to the west Eloh corridor)"),
            Of(CaveRegion, 349, 392, 638, 694, 82, 112, UndergroundOnly, Map + "region 9 (cave to the north Eloh corridor)"),
            Of(CaveRegion, 749, 801, 466, 514, 150, 193, UndergroundOnly, Map + "region 9 (cave to the east Eloh corridor)"),
            Of(CaveRegion, 680, 764, -49, 70, 78, 130, UndergroundOnly, Map + "region 9 (the crystal cave by Benefactor Valley)"),
            Of(BaneTunnelRegion, -892, -849, -912, -869, 118, 135, AnyLevel, Map + "region 8 (Bane tunnel to Purgas Station)"),
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
