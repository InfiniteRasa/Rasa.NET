using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    public class MapWaypointInfoList
    {
        internal uint GameGontextId { get; set; }
        internal List<MapInstanceInfo> MapInstanceList { get; set; }
        internal List<WaypointInfo> Waypoints { get; set; }

        public MapWaypointInfoList(uint gameContextId, List<MapInstanceInfo> mapInstncesList, List<WaypointInfo> waypoints)
        {
            GameGontextId = gameContextId;
            MapInstanceList = mapInstncesList;
            Waypoints = waypoints;
        }
    }

    public class WaypointInfo
    {
        internal uint WaypointId { get; set; }
        internal bool Contested { get; set; }
        internal Vector3 Position { get; set; }
        internal WaypointType WaypointType { get; set; }

        public WaypointInfo(uint waypointId, bool contested, WaypointType waypointType)
        {
            WaypointId = waypointId;
            Contested = contested;
            WaypointType = waypointType;
        }

        public WaypointInfo(uint waypointId, bool contested, Vector3 position, WaypointType waypointType)
        {
            WaypointId = waypointId;
            Contested = contested;
            Position = position;
            WaypointType = waypointType;
        }
    }
}
