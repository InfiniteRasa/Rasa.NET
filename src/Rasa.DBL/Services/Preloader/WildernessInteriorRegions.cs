using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Concordia Wilderness's unnamed regions put where the same rooms and places stand
    /// (adv_foreas_concordia_wilderness, 1220; Wilderness_interior_regions).
    ///
    /// - The AFS base on the floor of Ranja Gorge has two of the corman buildings Daghda's Urn
    ///   Village has: a hospital (arch_corman_hospital_v01) and a research HQ
    ///   (arch_corman_researchhq_v01). Daghdas_urn_building_regions gave the village's their
    ///   human interior regions, 26 (REGION_HUMAN_HOSPITAL) and 30 (REGION_HUMAN_OUTPOST), with
    ///   sky_foreas_general_human_interior and env_human_interior at priorities 44 and 46, the
    ///   highest on the map that set a sky. The gorge's were inside Ranja Gorge's own (region 9,
    ///   env_foreas_concordia_wilderness_foggy), so they had the foggy outdoor light indoors.
    ///   Each gets its region as the village's did, a box round the building: the hospital
    ///   x -706 to -660, z -360 to -327, the research HQ x -764 to -725, z -292 to -260.
    /// - Region 39 (sky_eloh_interior_default, env_eloh_interior_default, REGION_ELOH_RUINS_01,
    ///   MUSIC_ELOH_01; priority 39) is the map's one Eloh interior, and had no volume. The
    ///   map's Eloh corridors (arch_eloh_corridor_*) get it, as the same corridors got their
    ///   Eloh interior on Caves of Donn and Guardian Prominence:
    ///   the cross-shaped corridor off Enigma Cavern with the "Earth" logos (x -858 to -816,
    ///   z -751 to -675, under Enigma Cavern's cave sky until now); the corridor at the west end
    ///   of Daghda's Urn's cave with the "Here" logos (x -983 to -919, z 732 to 756, which no
    ///   region reached); the ten logos rooms stacked 64 m apart past the north-east edge of the
    ///   map, the one at 160 with the "Enhance" logos (x 806 to 858, z 919 to 986); and the
    ///   corridor behind the unlocked Eloh gate north of Alia Caverns (x 823 to 847, z 696 to
    ///   740).
    /// - Region 1 (sky_foreas_cave_short, env_foreas_cave_interior, REGION_FOREAS_CAVE_WITH_ECHO,
    ///   MUSIC_FOREAS_CONCORDIA_CAVE_1; the settings of Ranja Cavern, region 33) had no volume.
    ///   It gets the seven cave tunnels the map's links out are at the end of
    ///   (terra_foreas_cavern_entrance_24m, a ramp, terra_foreas_cavern_instance): to Guardian
    ///   Prominence, Pravus Research, the Caves of Donn, the CLRF and Manhattan, and both to
    ///   Concordia Divide. They lit as the open ground. Each box is the tunnel's pieces and is
    ///   underground-only, so it takes the tunnel and leaves its mouth in the open.
    /// - Regions 21 to 25 (env_human_interior, MUSIC_FOREAS_FOREAN_01; priorities 6 to 10, no
    ///   sky) had no volume. Their music is the one Alia Das's region 18 sets, not the AFS outpost
    ///   music of Twin Pillars or the Landing Zone's battle music, and Daghda's Urn's buildings
    ///   already have 26, 27 and 30. They are Alia Das's AFS field tents, which had the open
    ///   sky's light indoors. Region 25, the one with REGION_HUMAN_HOSPITAL, gets the medic tent
    ///   (arch_hum_tent_medic_01, x 783 to 791, z 357 to 374), where the Alia Das Hospital
    ///   marker stands and the vaccine missions' "Alia Das infirmary" is. Regions 21 to 24 are
    ///   alike, so which tent each gets changes nothing: 21 the west row of four (the cots, the
    ///   class trainer, the armor and weapon vendors, the stores), 22 the crafting tent, 23 the
    ///   communications tent and 24 the east tent with cots. Inside Alia Das's regions 18 and 50
    ///   (priorities 12 and 13) their ambience and music are Alia Das's; what they add is the
    ///   interior env map.
    /// - Region 56 (REGION_FOREAN_VILLAGE alone; priority 14) had no volume. Alia Das is the
    ///   one Forean village on the map where it would be heard: Daghda's Urn Village's regions
    ///   (36 to 38) and Stone Anvil's (15) set their own ambience over it. It gets the village
    ///   north of the AFS tents, the huts, platforms and walkways (arch_forean_village_*,
    ///   arch_forean_hut_*) from x 735 to 890, z 412 to 592, over Alia Das's
    ///   REGION_HUMAN_FIELD_BASE, which the tents keep.
    /// - Region 57 (sky_foreas_cave_short, env_eloh_interior_default, no music; priority 22) had
    ///   no volume. It is Alia Caverns (region 32, priority 21: the same sky, no music) with the
    ///   Eloh env map, and is the caverns' Eloh shrine room: the shrine Council Elder Solis sends
    ///   you to for the Logos ("acquire the Logos information from the shrine within"),
    ///   arch_forean_eloh_shrine_v02 between its Eloh columns at the west end of the lower level,
    ///   x 824 to 844, z 655 to 671, below the upper level's floor.
    /// - Daghda's Urn's cave (region 15, the circle of 80 round -797, 761) stops short of its
    ///   west end, the split and ramp down to the Here logos corridor (x -918 to -870), which had
    ///   the map's sky. A box of region 15 there, underground-only, leaves the cave mouth above
    ///   it in the open.
    ///
    /// Not placed: regions 5 (Molder.sky alone, priority 1) and 13 (REGION_HIGHLANDS alone,
    /// priority 4), which nothing ties to one place, and the Twin Pillars and Landing Zone
    /// buildings, which no unplaced region's music matches.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class WildernessInteriorRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1220;
        public const uint TunnelRegion = 1, HospitalRegion = 26, ResearchHqRegion = 30, ElohRegion = 39, DaghdasUrnRegion = 15;
        public const uint TentRegion = 21, CraftingTentRegion = 22, CommsTentRegion = 23, EastTentRegion = 24, MedicTentRegion = 25, VillageRegion = 56, ShrineRegion = 57;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "foreas_concordia_wilderness - ";

        public static readonly Volume[] Volumes =
        {
            Of(HospitalRegion, -706, -660, -360, -327, 168, 192, AnyLevel, Map + "region 26 (Ranja Gorge hospital)"),
            Of(ResearchHqRegion, -764, -725, -292, -260, 168, 194, AnyLevel, Map + "region 30 (Ranja Gorge research HQ)"),
            Of(ElohRegion, -858, -816, -751, -675, 138, 152, AnyLevel, Map + "region 39 (Eloh corridor off Enigma Cavern)"),
            Of(ElohRegion, -983, -919, 732, 756, 281, 295, AnyLevel, Map + "region 39 (Eloh corridor west of Daghda's Urn)"),
            Of(ElohRegion, 806, 858, 919, 986, -230, 375, AnyLevel, Map + "region 39 (the ten Eloh logos rooms)"),
            Of(ElohRegion, 823, 847, 696, 740, 283, 297, AnyLevel, Map + "region 39 (Eloh gate corridor north of Alia Caverns)"),
            Of(TunnelRegion, -61, -14, 655, 698, 240, 285, UndergroundOnly, Map + "region 1 (tunnel to Guardian Prominence)"),
            Of(TunnelRegion, -794, -727, 35, 82, 210, 256, UndergroundOnly, Map + "region 1 (tunnel to Pravus Research)"),
            Of(TunnelRegion, -446, -394, 208, 270, 160, 227, UndergroundOnly, Map + "region 1 (tunnel to the Caves of Donn)"),
            Of(TunnelRegion, -416, -373, -786, -739, 260, 306, UndergroundOnly, Map + "region 1 (tunnel to the CLRF)"),
            Of(TunnelRegion, -149, -68, -674, -601, 212, 250, UndergroundOnly, Map + "region 1 (tunnel to Manhattan)"),
            Of(TunnelRegion, 879, 926, 24, 67, 262, 313, UndergroundOnly, Map + "region 1 (tunnel to Concordia Divide, east)"),
            Of(TunnelRegion, 260, 308, -588, -545, 137, 188, UndergroundOnly, Map + "region 1 (tunnel to Concordia Divide, south)"),
            Of(MedicTentRegion, 783, 791, 357, 374, 293, 303, AnyLevel, Map + "region 25 (Alia Das medic tent)"),
            Of(TentRegion, 749, 766, 362, 370, 293, 303, AnyLevel, Map + "region 21 (Alia Das west tent 1)"),
            Of(TentRegion, 748, 765, 378, 387, 293, 303, AnyLevel, Map + "region 21 (Alia Das west tent 2, cots)"),
            Of(TentRegion, 750, 767, 393, 401, 293, 303, AnyLevel, Map + "region 21 (Alia Das west tent 3, vendors)"),
            Of(TentRegion, 750, 767, 403, 411, 293, 303, AnyLevel, Map + "region 21 (Alia Das west tent 4, stores)"),
            Of(CraftingTentRegion, 792, 804, 358, 368, 293, 303, AnyLevel, Map + "region 22 (Alia Das crafting tent)"),
            Of(CommsTentRegion, 856, 873, 384, 392, 293, 305, AnyLevel, Map + "region 23 (Alia Das communications tent)"),
            Of(EastTentRegion, 861, 878, 398, 407, 293, 303, AnyLevel, Map + "region 24 (Alia Das east tent, cots)"),
            Of(VillageRegion, 735, 890, 412, 592, 280, 370, AnyLevel, Map + "region 56 (Alia Das Forean village)"),
            Of(ShrineRegion, 824, 844, 655, 671, 265, 282, AnyLevel, Map + "region 57 (Alia Caverns Eloh shrine)"),
            Of(DaghdasUrnRegion, -918, -870, 729, 760, 266, 302, UndergroundOnly, Map + "Daghda's Urn (the cave's west end)"),
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
