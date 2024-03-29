﻿using System.Collections.Generic;

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
        public Dictionary<uint, MapWaypointInfoList> MapWaypointInfoList { get; set; }
        internal uint TempWormholes { get; set; }
        internal WaypointType WaypointTypeId { get; set; }
        internal uint CurrentWaypointId { get; set; }
        
        public EnteredWaypointPacket(uint currentMapId, uint gameContextId, Dictionary<uint, MapWaypointInfoList> mapWaypointInfoList, WaypointType waypointTypeId, uint currentWaypointId = 0)
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
                    foreach (var entry in MapWaypointInfoList)
                    {
                        var waypointInfo = entry.Value;
                        pw.WriteTuple(3);
                            pw.WriteUInt(waypointInfo.GameGontextId);
                            pw.WriteList(waypointInfo.MapInstanceList.Count);
                                foreach (var mapInstanceInfo in waypointInfo.MapInstanceList)
                                    pw.WriteStruct(mapInstanceInfo);

                            pw.WriteList(waypointInfo.Waypoints.Count);
                                foreach (var waypoint in waypointInfo.Waypoints)
                                {
                                    pw.WriteTuple(3);
                                        pw.WriteUInt(waypoint.WaypointId);
                                        pw.WriteTuple(3);
                                            pw.WriteDouble(waypoint.Position.X);
                                            pw.WriteDouble(waypoint.Position.Y);
                                            pw.WriteDouble(waypoint.Position.Z);
                                        pw.WriteBool(waypoint.Contested);
                                }
                    }
                pw.WriteNoneStruct();                       //tempwormhole'
                pw.WriteInt((int)WaypointTypeId);
                if (CurrentWaypointId != 0)
                    pw.WriteUInt(CurrentWaypointId);
                else
                    pw.WriteNoneStruct();
        }
    }
}
