namespace Rasa.Structures
{
    public class Mission
    {
        public uint MissionIndex { get; set; } // index of the mission in the mission table
        public int MissionId { get; set; } // the missionId used internally by the game
        public int StateCount { get; set; } // number of states
        public uint StateMapping { get; set; } // fast state -> line conversion array (plus one entry at the end that stores the value of stateCount, used to allow fast access to number of lines for a state)
        public MissionScriptLine ScriptLines { get; set; }
        public int ScriptLineCount { get; set; }
        public int ObjectiveCount { get; set; }
        public MissionObjective ObjectiveList { get; set; }
        // mission info
        public int CategoryId {get; set;}
        public int Level { get; set; } // level of the mission (not required level)
    }
}
