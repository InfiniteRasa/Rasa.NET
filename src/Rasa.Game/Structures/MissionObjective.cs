using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionObjective
    {
        public uint ObjectiveId { get; set; }
        public uint ObjectiveStatus { get; set; }
        public uint Ordinal { get; set; }
        public uint TimeRemaining { get; set; }
        public object CounterDict { get; set; }
        public Dictionary<uint, MissionObjectiveCounter> ItemCounters = new();  // itemClassId = (count, countMax)
        public bool IsRequired { get; set; }
        public List<MissionIndicator> IndicatorList { get; set; }
    }
}
