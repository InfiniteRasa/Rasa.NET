using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class MissionObjective
    {
        public uint ObjectiveId { get; set; }
        public MissionObjectiveState State { get; set; }
        public uint Ordinal { get; set; }
        public uint? TimeRemaining { get; set; }
        public Dictionary<uint, MissionObjectiveCounter> Counters { get; } = new();
        public Dictionary<uint, MissionObjectiveItemCounter> ItemCounters { get; } = new();
        public bool IsRequired { get; set; }
        public List<MissionIndicator> IndicatorList { get; } = new();
    }
}
