using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureNpcData
    {
        public Dictionary<int, Mission> Missions = new Dictionary<int, Mission>();   // list of all missions that involve this NPC type
        public List<int> NpcPackageIds = new List<int>();
    }
}
