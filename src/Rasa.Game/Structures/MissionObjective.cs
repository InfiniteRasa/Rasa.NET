using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionObjective
    {
        public uint State { get; set; } // min state
        public int ObjectiveId { get; set; }
        public uint CounterCount { get; set; }
        public List<MissionObjectiveCounter> Counter { get; set; }
    }
}
