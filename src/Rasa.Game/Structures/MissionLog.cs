using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionLog
    {
        public int MissionIndex { get; set; }       // use index instead of id for faster access, but if we save to the DB we have to translate this back to id
        public int State { get; set; }              // the state the mission currently is at
        public List<int> MissionData { get; set; }  // 8 16-bit values of mission data (stores kill count, completed objectives..)
    }
}
