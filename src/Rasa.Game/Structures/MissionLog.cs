using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionLog
    {
        public int MissionId { get; set; }
        public short MissionState { get; set; } // the state the mission currently is at
        public List<int> MissionData { get; set; }  // 8 16-bit values of mission data (stores kill count, completed objectives..)
    }
}
