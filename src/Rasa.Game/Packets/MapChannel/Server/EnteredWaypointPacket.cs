using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class EnteredWaypointPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EnteredWaypoint;

        public uint CurrentMapId { get; set; }
        public uint GameContextId { get; set; }
        public List<MapWaypointInfoList> MapWaypointInfoList { get; set; }
        public uint TempWormholes { get; set; }
        public uint WaypointTypeId { get; set; }
        public uint CurrentWaypointId { get; set; }
        
        public EnteredWaypointPacket(uint currentMapId, uint gameContextId, List<MapWaypointInfoList> mapWaypointInfoList, uint waypointTypeId, uint currentWaypointId)
        {
            CurrentMapId = currentMapId;
            GameContextId = gameContextId;
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
                pw.WriteList(0);                                // mapInstanceList  (we wont use mapInstances)
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
            pw.WriteNoneStruct();
            pw.WriteUInt(WaypointTypeId);
            pw.WriteUInt(CurrentWaypointId);
        }
    }
}
