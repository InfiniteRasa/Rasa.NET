using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    public class MapWaypointInfoList
    {
        public uint GameGontextId { get; set; }
        public List<uint> MapInstances { get; set; }            // we wont use mapInstances
        public List<WaypointInfo> WaypointInfo { get; set; }


        public MapWaypointInfoList(uint gameContextId, List<WaypointInfo> waypointInfo)
        {
            GameGontextId = gameContextId;
            WaypointInfo = waypointInfo;
        }

    }

    public class WaypointInfo
    {
        public uint WaypointId { get; set; }
        public bool Contested { get; set; }
        public Vector3 Position { get; set; }
        public uint WaypointType { get; set; }

        public WaypointInfo(uint waypointId, bool contested, uint waypointType)
        {
            WaypointId = waypointId;
            Contested = contested;
            WaypointType = waypointType;
        }
    }
}
