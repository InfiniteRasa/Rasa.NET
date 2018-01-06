using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureNpcData
    {
        public List<NpcDataRelatedMission> RelatedMissions { get; set; } // list of all missions that involve this NPC type
        public int RelatedMissionCount { get; set; }
        public int NpcPackageId { get; set; }
    }
}
