namespace Rasa.Structures
{
    using Data;
    using Memory;
    public class MapInstanceInfo : IPythonDataStruct
    {
        internal uint MapInstanceId { get; set; }
        internal uint MapContextId { get; set; }
        internal MapInstanceStatus MapInstanceStatus { get; set; }

        public MapInstanceInfo(uint mapInstanceId, uint mapContextId, MapInstanceStatus mapInstanceStatus)
        {
            MapInstanceId = mapInstanceId;
            MapContextId = mapContextId;
            MapInstanceStatus = mapInstanceStatus;
        }

        public void Read(PythonReader pr)
        {

        }

        /// <summary>
        /// waypointwindow.py reads this as (ordinal, mapId, overloadedStatus): the ordinal is
        /// shown after the map's name, and mapId keys the row - it is what SelectWaypoint sends
        /// back as the map instance, and what EnteredWaypoint's currentMapId is compared with.
        /// The map's context id is that key: unique across the maps a dropship list shows, where
        /// every instance number is 1.
        /// </summary>
        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MapInstanceId);
            pw.WriteUInt(MapContextId);
            pw.WriteUInt((uint)MapInstanceStatus);
        }
    }
}
