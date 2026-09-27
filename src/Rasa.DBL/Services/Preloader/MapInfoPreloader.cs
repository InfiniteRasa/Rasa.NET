using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class MapInfoPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, MapInfoEntry.TableName, typeof(MapInfoEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1115, "characterselection", 0, 0 };
            yield return new object[] { 1148, "adv_foreas_concordia_divide", 1584, 10 };
            yield return new object[] { 1220, "adv_foreas_concordia_wilderness", 1556, 0 };
            yield return new object[] { 1244, "adv_foreas_concordia_palisades", 1016, 0 };
            yield return new object[] { 1304, "adv_foreas_valverde_pools", 520, 0 };
            yield return new object[] { 1347, "adv_foreas_concordia_divide_minoscaverns", 293, 4 };
            yield return new object[] { 1348, "adv_foreas_concordia_divide_timoramines", 288, 3 };
            yield return new object[] { 1349, "adv_foreas_concordia_divide_torcastraprison", 468, 3 };
            yield return new object[] { 1384, "adv_foreas_concordia_palisades_warnetcaverns", 203, 0 };
            yield return new object[] { 1394, "adv_foreas_concordia_palisades_devilsden", 327, 0 };
            yield return new object[] { 1397, "adv_foreas_concordia_palisades_treebackcamp", 286, 0 };
            yield return new object[] { 1416, "adv_foreas_concordia_wilderness_guardianprom", 232, 0 };
            yield return new object[] { 1429, "adv_foreas_valverde_marshes_wetlandrefinery", 306, 0 };
            yield return new object[] { 1430, "adv_foreas_concordia_wilderness_pravusresearch", 555, 0 };
            yield return new object[] { 1451, "adv_foreas_valverde_marshes_banesupplydepot", 185, 0 };
            yield return new object[] { 1454, "adv_foreas_valverde_marshes", 663, 0 };
            yield return new object[] { 1465, "adv_foreas_valverde_pools_test_weapons_center", 133, 0 };
            yield return new object[] { 1497, "adv_foreas_valverde_plateau", 791, 0 };
            yield return new object[] { 1502, "adv_foreas_valverde_plateau_ustoryard", 187, 9 };
            yield return new object[] { 1506, "adv_foreas_concordia_wilderness_cavesofdonn02", 535, 8 };
            yield return new object[] { 1694, "adv_foreas_valverde_pools_retread_caves", 198, 0 };
            yield return new object[] { 1700, "adv_foreas_valverde_marshes_logosresearchfacility", 371, 0 };
            yield return new object[] { 1721, "adv_foreas_concordia_wilderness_clrf", 290, 0 };
            yield return new object[] { 1734, "adv_arieki_ligo_ashendesert", 373, 0 };
            yield return new object[] { 1737, "test_nexus2", 147, 0 };
            yield return new object[] { 1743, "adv_foreas_valverde_marshes_villageruins", 446, 0 };
            yield return new object[] { 1759, "adv_arieki_torden_mires", 628, 0 };
            yield return new object[] { 1761, "adv_arieki_torden_incline", 802, 0 };
            yield return new object[] { 1763, "adv_foreas_valverde_pools_livetargetpensv2", 178, 0 };
            yield return new object[] { 1764, "adv_arieki_torden_plains", 877, 0 };
            yield return new object[] { 1773, "adv_arieki_torden_plains_attacolony", 274, 0 };
            yield return new object[] { 1803, "adv_foreas_concordia_palisades_elohtemples", 250, 0 };
            yield return new object[] { 1806, "adv_foreas_concordia_divide_purgasstation2", 298, 0 };
            yield return new object[] { 1823, "adv_foreas_valverde_plateau_sanctusgrotto", 154, 0 };
            yield return new object[] { 1830, "adv_foreas_valverde_plateau_maligobasev3", 144, 0 };
            yield return new object[] { 1865, "adv_arieki_torden_incline_ojasaattahive", 222, 0 };
            yield return new object[] { 1911, "adv_arieki_ligo_thunderhead", 376, 2 };
            yield return new object[] { 1977, "adv_arieki_ligo_burningsteps_magmacaverns", 195, 0 };
            yield return new object[] { 1985, "adv_bootcamp", 783, 4 };
            yield return new object[] { 1988, "adv_arieki_ligo_ashendesert_baneconscriptfacility", 254, 0 };
            yield return new object[] { 1991, "test_lridout_outpostcombat", 114, 0 };
            yield return new object[] { 1993, "adv_arieki_ligo_burningsteps", 406, 0 };
            yield return new object[] { 2028, "adv_arieki_torden_abyss", 410, 0 };
            yield return new object[] { 2029, "adv_foreas_valverde_plateau_temporalchamber", 131, 0 };
            yield return new object[] { 2034, "adv_arieki_torden_plains_penalresearch", 230, 0 };
            yield return new object[] { 2047, "adv_foreas_valverde_descent", 330, 0 };
            yield return new object[] { 2051, "adv_foreas_howlingmaw1", 421, 0 };
            yield return new object[] { 2055, "adv_arieki_ligo_ashendesert_indracaverns", 219, 0 };
            yield return new object[] { 2084, "adv_foreas_concordia_elohcommtower2", 337, 0 };
            yield return new object[] { 2085, "adv_arieki_torden_incline_commtower", 226, 0 };
            yield return new object[] { 2093, "adv_arieki_torden_plains_brannwaterrefinery", 209, 0 };
            yield return new object[] { 2103, "adv_arieki_ligo_thunderhead_faultlever", 304, 0 };
            yield return new object[] { 2105, "adv_arieki_ligo_thunderhead_quassostation", 261, 0 };
            yield return new object[] { 2107, "adv_arieki_torden_mires_energyweaponcenter", 214, 0 };
            yield return new object[] { 2110, "adv_arieki_ligo_ashendesert_avernusoutpost", 139, 0 };
            yield return new object[] { 2111, "adv_arieki_torden_incline_wardenbotfactory", 191, 0 };
            yield return new object[] { 2112, "adv_arieki_ligo_thunderhead_rivasaattacolony", 86, 0 };
            yield return new object[] { 2115, "adv_arieki_torden_mires_banefluxitemines", 204, 0 };
            yield return new object[] { 2125, "adv_arieki_torden_mires_tahrendrabase", 96, 0 };
            yield return new object[] { 2136, "adv_foreas_howlingmaw_deathburrow", 218, 0 };
            yield return new object[] { 2138, "adv_arieki_ligo_staaljunkyard", 173, 0 };
            yield return new object[] { 2141, "adv_arieki_ligo_crucible_incurablesward", 199, 0 };
            yield return new object[] { 2146, "adv_arieki_ligo_crucible_wbfacility", 147, 0 };
            yield return new object[] { 2155, "adv_arieki_torden_abyss_tampei", 167, 0 };
            yield return new object[] { 2156, "adv_foreas_valverde_descent_therefuge", 56, 0 };
            yield return new object[] { 2162, "adv_foreas_howlingmaw_cuthahbase", 125, 0 };
            yield return new object[] { 2163, "adv_foreas_valverde_descent_inferno_outpost", 128, 0 };
            yield return new object[] { 2190, "adv_arieki_torden_abyss_dybukkar", 174, 0 };
            yield return new object[] { 2203, "adv_arieki_torden_abyss_omegalabs", 162, 0 };
            yield return new object[] { 2233, "test_pvpcontrolpoint", 140, 0 };
            yield return new object[] { 2259, "adv_wargame_indoorarena", 35, 0 };
            yield return new object[] { 2278, "adv_zepic_pve_arena", 41, 0 };
            yield return new object[] { 2327, "adv_earth_unitedstates_manhattan_01", 122, 0 };
            yield return new object[] { 2361, "adv_wargame_provinggroundsv002", 109, 0 };
            yield return new object[] { 2368, "adv_foreas_concordia_wilderness_cavesofdonn_epic", 13, 8 };
            yield return new object[] { 2374, "adv_wargame_edmundrange2", 24, 0 };
            yield return new object[] { 2375, "adv_earth_unitedstates_manhattan_01_shared", 13, 0 };
            yield return new object[] { 20000009, "adv_afs_arena", 56, 0 };

        }
    }
}
