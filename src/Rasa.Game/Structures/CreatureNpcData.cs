using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureNpcData
    {
        public List<NpcDataRelatedMission> RelatedMissions { get; set; } = new List<NpcDataRelatedMission>();   // list of all missions that involve this NPC type
        public int NpcPackageId { get; set; }
    }
}
