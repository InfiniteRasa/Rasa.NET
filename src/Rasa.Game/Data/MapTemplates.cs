namespace Rasa.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// The map template each loaded map is built from, by its map context id: the client's
    /// generated.client.gamecontext (classname, type, description, mapTemplateId, ...). The client
    /// names a map by its template in a few places - the instance picker's rows
    /// (Recv_ChooseInstanceList) are one - and finds the context from it
    /// (gamemap.GetContextIdForMapTemplateId).
    /// </summary>
    public static class MapTemplates
    {
        private static readonly Dictionary<uint, uint> ByContext = new Dictionary<uint, uint>
        {
            [1115] = 1271,      // characterselection
            [1148] = 1306,      // adv_foreas_concordia_divide
            [1220] = 1378,      // adv_foreas_concordia_wilderness
            [1244] = 1402,      // adv_foreas_concordia_palisades
            [1304] = 1461,      // adv_foreas_valverde_pools
            [1347] = 1504,      // adv_foreas_concordia_divide_minoscaverns
            [1348] = 1505,      // adv_foreas_concordia_divide_timoramines
            [1349] = 1506,      // adv_foreas_concordia_divide_torcastraprison
            [1384] = 1541,      // adv_foreas_concordia_palisades_warnetcaverns
            [1394] = 1551,      // adv_foreas_concordia_palisades_devilsden
            [1397] = 1554,      // adv_foreas_concordia_palisades_treebackcamp
            [1416] = 1573,      // adv_foreas_concordia_wilderness_guardianprom
            [1429] = 1586,      // adv_foreas_valverde_marshes_wetlandrefinery
            [1430] = 1587,      // adv_foreas_concordia_wilderness_pravusresearch
            [1451] = 1608,      // adv_foreas_valverde_marshes_banesupplydepot
            [1454] = 1611,      // adv_foreas_valverde_marshes
            [1465] = 1622,      // adv_foreas_valverde_pools_test_weapons_center
            [1497] = 1654,      // adv_foreas_valverde_plateau
            [1502] = 1659,      // adv_foreas_valverde_plateau_ustoryard
            [1506] = 1663,      // adv_foreas_concordia_wilderness_cavesofdonn02
            [1694] = 1694,      // adv_foreas_valverde_pools_retread_caves
            [1700] = 1700,      // adv_foreas_valverde_marshes_logosresearchfacility
            [1721] = 1721,      // adv_foreas_concordia_wilderness_clrf
            [1734] = 1734,      // adv_arieki_ligo_ashendesert
            [1737] = 1737,      // test_nexus2
            [1743] = 1743,      // adv_foreas_valverde_marshes_villageruins
            [1759] = 1759,      // adv_arieki_torden_mires
            [1761] = 1761,      // adv_arieki_torden_incline
            [1763] = 1763,      // adv_foreas_valverde_pools_livetargetpensv2
            [1764] = 1764,      // adv_arieki_torden_plains
            [1773] = 1773,      // adv_arieki_torden_plains_attacolony
            [1803] = 1803,      // adv_foreas_concordia_palisades_elohtemples
            [1806] = 1806,      // adv_foreas_concordia_divide_purgasstation2
            [1823] = 1823,      // adv_foreas_valverde_plateau_sanctusgrotto
            [1830] = 1830,      // adv_foreas_valverde_plateau_maligobasev3
            [1865] = 1865,      // adv_arieki_torden_incline_ojasaattahive
            [1911] = 1911,      // adv_arieki_ligo_thunderhead
            [1977] = 1977,      // adv_arieki_ligo_burningsteps_magmacaverns
            [1985] = 1985,      // adv_bootcamp
            [1988] = 1988,      // adv_arieki_ligo_ashendesert_baneconscriptfacility
            [1993] = 1993,      // adv_arieki_ligo_burningsteps
            [2028] = 2028,      // adv_arieki_torden_abyss
            [2029] = 2029,      // adv_foreas_valverde_plateau_temporalchamber
            [2034] = 2034,      // adv_arieki_torden_plains_penalresearch
            [2047] = 2047,      // adv_foreas_valverde_descent
            [2051] = 2051,      // adv_foreas_howlingmaw1
            [2055] = 2055,      // adv_arieki_ligo_ashendesert_indracaverns
            [2084] = 2084,      // adv_foreas_concordia_elohcommtower2
            [2085] = 2085,      // adv_arieki_torden_incline_commtower
            [2093] = 2093,      // adv_arieki_torden_plains_brannwaterrefinery
            [2103] = 2103,      // adv_arieki_ligo_thunderhead_faultlever
            [2105] = 2105,      // adv_arieki_ligo_thunderhead_quassostation
            [2107] = 2107,      // adv_arieki_torden_mires_energyweaponcenter
            [2110] = 2110,      // adv_arieki_ligo_ashendesert_avernusoutpost
            [2111] = 2111,      // adv_arieki_torden_incline_wardenbotfactory
            [2112] = 2112,      // adv_arieki_ligo_thunderhead_rivasaattacolony
            [2115] = 2115,      // adv_arieki_torden_mires_banefluxitemines
            [2125] = 2125,      // adv_arieki_torden_mires_tahrendrabase
            [2136] = 2136,      // adv_foreas_howlingmaw_deathburrow
            [2138] = 2138,      // adv_arieki_ligo_staaljunkyard
            [2141] = 2141,      // adv_arieki_ligo_crucible_incurablesward
            [2146] = 2146,      // adv_arieki_ligo_crucible_wbfacility
            [2155] = 2155,      // adv_arieki_torden_abyss_tampei
            [2156] = 2156,      // adv_foreas_valverde_descent_therefuge
            [2162] = 2162,      // adv_foreas_howlingmaw_cuthahbase
            [2163] = 2163,      // adv_foreas_valverde_descent_inferno_outpost
            [2190] = 2190,      // adv_arieki_torden_abyss_dybukkar
            [2203] = 2203,      // adv_arieki_torden_abyss_omegalabs
            [2233] = 2237,      // test_pvpcontrolpoint
            [2259] = 2263,      // adv_wargame_indoorarena
            [2278] = 2282,      // adv_zepic_pve_arena
            [2327] = 2331,      // adv_earth_unitedstates_manhattan_01
            [2361] = 2365,      // adv_wargame_provinggroundsv002
            [2368] = 2371,      // adv_foreas_concordia_wilderness_cavesofdonn_epic
            [2374] = 2377,      // adv_wargame_edmundrange2
            [2375] = 2378,      // adv_earth_unitedstates_manhattan_01_shared
            [20000009] = 2232,  // adv_afs_arena
        };

        /// <summary>The map's template id, or 0 for a context the client has no template for.</summary>
        public static uint Of(uint mapContextId) => ByContext.TryGetValue(mapContextId, out var templateId) ? templateId : 0;
    }
}
