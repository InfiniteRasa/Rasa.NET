using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>A camera script a map carries: its map, its id there, how many keyframes it has and how long it runs.</summary>
    public sealed class CameraScriptInfo
    {
        public CameraScriptInfo(string mapName, uint scriptId, int keyframes, long lengthMs)
        {
            MapName = mapName;
            ScriptId = scriptId;
            Keyframes = keyframes;
            LengthMs = lengthMs;
        }

        public string MapName { get; }
        public uint ScriptId { get; }
        public int Keyframes { get; }

        /// <summary>Every keyframe's arrive and hold time and the script's return time, added up.</summary>
        public long LengthMs { get; }
    }

    /// <summary>
    /// The camera scripts in the client's maps, read from each .map the way gamemap.py's
    /// _LoadCameraScripts2 reads them: (scriptId, returnTimeMs, keyframes of position, rotation,
    /// focal length, arriveTimeMs and holdTimeMs). 129 scripts on 37 maps, numbered from 1 on each.
    /// The Wargame maps' sixteen are single keyframes held for 1,999,998 ms - cameras that stay
    /// until the player leaves them.
    /// </summary>
    public static class CameraScriptTable
    {
        public static readonly CameraScriptInfo[] All =
        {
            new CameraScriptInfo("adv_arieki_ligo_ashendesert_baneconscriptfacility", 1, 3, 9000),
            new CameraScriptInfo("adv_arieki_ligo_ashendesert_indracaverns", 1, 12, 11000),
            new CameraScriptInfo("adv_arieki_ligo_burningsteps_magmacaverns", 1, 4, 10000),
            new CameraScriptInfo("adv_arieki_ligo_burningsteps_magmacaverns", 2, 5, 10000),
            new CameraScriptInfo("adv_arieki_ligo_burningsteps_magmacaverns", 3, 4, 10000),
            new CameraScriptInfo("adv_arieki_ligo_crucible_wbfacility", 1, 34, 31250),
            new CameraScriptInfo("adv_arieki_ligo_crucible_wbfacility", 2, 7, 7000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_faultlever", 1, 10, 21500),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_faultlever", 2, 8, 14000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_faultlever", 3, 6, 8000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_faultlever", 4, 6, 8000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_faultlever", 5, 11, 20000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_quassostation", 1, 44, 50500),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_quassostation", 2, 9, 13000),
            new CameraScriptInfo("adv_arieki_ligo_thunderhead_quassostation", 3, 8, 14000),
            new CameraScriptInfo("adv_arieki_torden_abyss_dybukkar", 1, 7, 12000),
            new CameraScriptInfo("adv_arieki_torden_abyss_dybukkar", 2, 5, 12500),
            new CameraScriptInfo("adv_arieki_torden_abyss_dybukkar", 3, 8, 11000),
            new CameraScriptInfo("adv_arieki_torden_abyss_dybukkar", 4, 7, 28000),
            new CameraScriptInfo("adv_arieki_torden_abyss_omegalabs", 1, 2, 5000),
            new CameraScriptInfo("adv_arieki_torden_abyss_omegalabs", 2, 3, 4000),
            new CameraScriptInfo("adv_arieki_torden_abyss_omegalabs", 3, 2, 2000),
            new CameraScriptInfo("adv_arieki_torden_abyss_omegalabs", 4, 3, 3000),
            new CameraScriptInfo("adv_arieki_torden_abyss_omegalabs", 5, 9, 25000),
            new CameraScriptInfo("adv_arieki_torden_incline_ojasaattahive", 1, 16, 16000),
            new CameraScriptInfo("adv_arieki_torden_incline_wardenbotfactory", 1, 2, 6000),
            new CameraScriptInfo("adv_arieki_torden_mires_banefluxitemines", 1, 4, 9000),
            new CameraScriptInfo("adv_arieki_torden_mires_banefluxitemines", 2, 8, 9000),
            new CameraScriptInfo("adv_arieki_torden_mires_banefluxitemines", 3, 6, 9500),
            new CameraScriptInfo("adv_arieki_torden_mires_banefluxitemines", 4, 10, 14500),
            new CameraScriptInfo("adv_arieki_torden_mires_energyweaponcenter", 1, 1, 3000),
            new CameraScriptInfo("adv_arieki_torden_mires_energyweaponcenter", 2, 2, 5000),
            new CameraScriptInfo("adv_arieki_torden_mires_energyweaponcenter", 3, 4, 8000),
            new CameraScriptInfo("adv_arieki_torden_mires_energyweaponcenter", 4, 2, 6000),
            new CameraScriptInfo("adv_arieki_torden_mires_energyweaponcenter", 5, 8, 11500),
            new CameraScriptInfo("adv_arieki_torden_mires_tahrendrabase", 1, 2, 5000),
            new CameraScriptInfo("adv_arieki_torden_plains_attacolony", 1, 8, 15000),
            new CameraScriptInfo("adv_arieki_torden_plains_attacolony", 2, 10, 19000),
            new CameraScriptInfo("adv_arieki_torden_plains_attacolony", 3, 18, 35000),
            new CameraScriptInfo("adv_arieki_torden_plains_attacolony", 4, 14, 27000),
            new CameraScriptInfo("adv_arieki_torden_plains_attacolony", 5, 15, 29000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 1, 1, 31000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 2, 1, 31000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 3, 1, 30000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 4, 1, 5000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 5, 1, 5000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 6, 1, 5000),
            new CameraScriptInfo("adv_arieki_torden_plains_penalresearch", 7, 1, 5000),
            new CameraScriptInfo("adv_bootcamp", 1, 3, 8000),
            new CameraScriptInfo("adv_earth_unitedstates_manhattan_01", 1, 3, 10000),
            new CameraScriptInfo("adv_earth_unitedstates_manhattan_01_shared", 1, 3, 10000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 1, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 2, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 3, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 4, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 5, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 6, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 7, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 8, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 9, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 10, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 11, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide", 12, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_divide_minoscaverns", 1, 1, 3000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 1, 3, 7500),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 2, 2, 6000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 3, 1, 120000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 4, 1, 3500),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 5, 1, 120000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 6, 6, 27000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 7, 6, 27000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 8, 2, 7000),
            new CameraScriptInfo("adv_foreas_concordia_divide_purgasstation2", 9, 4, 21000),
            new CameraScriptInfo("adv_foreas_concordia_divide_torcastraprison", 1, 1, 10000),
            new CameraScriptInfo("adv_foreas_concordia_divide_torcastraprison", 2, 1, 10000),
            new CameraScriptInfo("adv_foreas_concordia_elohcommtower2", 1, 2, 9500),
            new CameraScriptInfo("adv_foreas_concordia_elohcommtower2", 2, 2, 10000),
            new CameraScriptInfo("adv_foreas_concordia_palisades_elohtemples", 1, 2, 15510),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_cavesofdonn02", 1, 3, 8000),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_cavesofdonn02", 2, 1, 6000),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_cavesofdonn02", 3, 1, 4000),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_pravusresearch", 1, 1, 30000),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_pravusresearch", 2, 2, 30000),
            new CameraScriptInfo("adv_foreas_concordia_wilderness_pravusresearch", 3, 1, 30000),
            new CameraScriptInfo("adv_foreas_howlingmaw_cuthahbase", 1, 2, 10000),
            new CameraScriptInfo("adv_foreas_howlingmaw_cuthahbase", 2, 1, 10000),
            new CameraScriptInfo("adv_foreas_howlingmaw_deathburrow", 1, 5, 21000),
            new CameraScriptInfo("adv_foreas_valverde_marshes_banesupplydepot", 1, 2, 7500),
            new CameraScriptInfo("adv_foreas_valverde_marshes_logosresearchfacility", 1, 2, 6000),
            new CameraScriptInfo("adv_foreas_valverde_marshes_logosresearchfacility", 2, 3, 9000),
            new CameraScriptInfo("adv_foreas_valverde_marshes_logosresearchfacility", 3, 1, 4000),
            new CameraScriptInfo("adv_foreas_valverde_marshes_logosresearchfacility", 4, 3, 9000),
            new CameraScriptInfo("adv_foreas_valverde_marshes_logosresearchfacility", 5, 2, 6000),
            new CameraScriptInfo("adv_foreas_valverde_plateau", 1, 2, 7000),
            new CameraScriptInfo("adv_foreas_valverde_plateau", 2, 2, 7000),
            new CameraScriptInfo("adv_foreas_valverde_plateau", 3, 2, 7000),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 1, 2, 5500),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 2, 2, 5500),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 3, 1, 2500),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 4, 2, 5400),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 5, 5, 5800),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 6, 1, 4000),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 7, 1, 8000),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 8, 2, 5000),
            new CameraScriptInfo("adv_foreas_valverde_plateau_maligobasev3", 9, 2, 10250),
            new CameraScriptInfo("adv_foreas_valverde_plateau_sanctusgrotto", 1, 4, 4500),
            new CameraScriptInfo("adv_foreas_valverde_plateau_sanctusgrotto", 2, 7, 7000),
            new CameraScriptInfo("adv_foreas_valverde_pools_livetargetpensv2", 1, 3, 5250),
            new CameraScriptInfo("adv_foreas_valverde_pools_livetargetpensv2", 2, 3, 4500),
            new CameraScriptInfo("adv_foreas_valverde_pools_livetargetpensv2", 3, 2, 7500),
            new CameraScriptInfo("adv_wargame_edmundrange2", 1, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 2, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 3, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 4, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 5, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 6, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 7, 1, 1999998),
            new CameraScriptInfo("adv_wargame_edmundrange2", 8, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 1, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 2, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 3, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 4, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 5, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 6, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 7, 1, 1999998),
            new CameraScriptInfo("adv_wargame_provinggroundsv002", 8, 1, 1999998),
            new CameraScriptInfo("test_pvpcontrolpoint01", 1, 1, 60000),
            new CameraScriptInfo("test_pvpcontrolpoint01", 2, 1, 60000),
            new CameraScriptInfo("test_pvpcontrolpoint01", 3, 1, 60000),
        };

        public static IEnumerable<CameraScriptInfo> OnMap(string mapName) =>
            All.Where(s => string.Equals(s.MapName, mapName, System.StringComparison.OrdinalIgnoreCase));

        public static CameraScriptInfo Find(string mapName, uint scriptId) => OnMap(mapName).FirstOrDefault(s => s.ScriptId == scriptId);
    }
}
