using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionEnv
    {
        public List<Mission> MissionList{get; set;} // synchronous access to any npc mission list
                               //hashTable_t ht_mission;
        public uint LoadState { get; set; }
        /*
            Important notice:
                After loading the mission Hashtable is accessed unsynchronized.
                Therefore only read access is safe. Never write to the Hashtable during runtime.
        */
        public MissionScriptLine MissionScriptLines { get; set; }
        public uint MissionScriptLineCount { get; set; }
        // mission table (used for missionIndex -> mission mapping)
        public Mission Missions { get; set; }
        public int MissionCount { get; set; }
    }
}
