using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class EnteredWaypointPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EnteredWaypoint;

        internal uint CurrentMapId { get; set; }
        internal uint GameContextId { get; set; }
        internal MapInstanceInfo MapInstanceInfo { get; set; }
        public List<MapWaypointInfoList> MapWaypointInfoList { get; set; }
        internal uint TempWormholes { get; set; }
        internal uint WaypointTypeId { get; set; }
        internal uint CurrentWaypointId { get; set; }
        
        public EnteredWaypointPacket(uint currentMapId, uint gameContextId, MapInstanceInfo mapInstanceInfo, List<MapWaypointInfoList> mapWaypointInfoList, uint waypointTypeId, uint currentWaypointId = 0)
        {
            CurrentMapId = currentMapId;
            GameContextId = gameContextId;
            MapInstanceInfo = mapInstanceInfo;
            MapWaypointInfoList = mapWaypointInfoList;
            //TempWormholes = tempWormholes;
            WaypointTypeId = waypointTypeId;
            CurrentWaypointId = currentWaypointId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(CurrentMapId);
            pw.WriteUInt(GameContextId);
            pw.WriteList(MapWaypointInfoList.Count);
            foreach (var waypointInfo in MapWaypointInfoList)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(waypointInfo.GameGontextId);       // gameContextId
                pw.WriteList(1);                                // mapInstances
                pw.WriteStruct(MapInstanceInfo);
                pw.WriteList(waypointInfo.WaypointInfo.Count);  // waypoints
                foreach(var waypoint in waypointInfo.WaypointInfo)
                {
                    pw.WriteTuple(3);
                    pw.WriteUInt(waypoint.WaypointId);          // waypointId
                    pw.WriteTuple(3);                           // pos
                        pw.WriteDouble(waypoint.Position.X);
                        pw.WriteDouble(waypoint.Position.Y);
                        pw.WriteDouble(waypoint.Position.Z);
                    pw.WriteBool(waypoint.Contested);           // contested
                }
            }
            pw.WriteNoneStruct();       // tempwormhole's
            pw.WriteUInt(WaypointTypeId);
            if (CurrentWaypointId != 0)
                pw.WriteUInt(CurrentWaypointId);
            else
                pw.WriteNoneStruct();
        }
    }
}
