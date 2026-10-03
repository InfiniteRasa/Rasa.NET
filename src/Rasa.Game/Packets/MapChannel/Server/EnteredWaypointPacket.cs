using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class EnteredWaypointPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EnteredWaypoint;

        /// <summary>
        /// The map the player is on, by the id its row in the list is keyed by - its context id
        /// (MapInstanceInfo's mapId). The window marks that row "(current)" and starts with it
        /// selected; sent as the instance number, it matched no row.
        /// </summary>
        internal uint CurrentMapId { get; set; }
        internal uint GameContextId { get; set; }
        public Dictionary<uint, MapWaypointInfoList> MapWaypointInfoList { get; set; }

        /// <summary>
        /// The Personal Waypoints on the map the player is on that they may return to
        /// (PersonalWaypoints): the window lists each under that map as "Temp Wormhole:" and its
        /// owner's name, and picking one is ReturnToWormhole. Null is none.
        /// </summary>
        internal IReadOnlyList<TempWormhole> TempWormholes { get; set; }

        internal WaypointType WaypointTypeId { get; set; }
        internal uint CurrentWaypointId { get; set; }
        
        public EnteredWaypointPacket(uint currentMapId, uint gameContextId, Dictionary<uint, MapWaypointInfoList> mapWaypointInfoList, WaypointType waypointTypeId, uint currentWaypointId = 0,
            IReadOnlyList<TempWormhole> tempWormholes = null)
        {
            CurrentMapId = currentMapId;
            GameContextId = gameContextId;
            MapWaypointInfoList = mapWaypointInfoList;
            TempWormholes = tempWormholes;
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
                if (TempWormholes == null || TempWormholes.Count == 0)
                    pw.WriteNoneStruct();
                else
                {
                    pw.WriteList(TempWormholes.Count);
                        foreach (var wormhole in TempWormholes)
                        {
                            pw.WriteTuple(3);
                                pw.WriteULong(wormhole.Id);
                                pw.WriteTuple(3);
                                    pw.WriteDouble(wormhole.Position.X);
                                    pw.WriteDouble(wormhole.Position.Y);
                                    pw.WriteDouble(wormhole.Position.Z);
                                pw.WriteUnicodeString(wormhole.OwnerName ?? "");
                        }
                }
                pw.WriteInt((int)WaypointTypeId.ToClient());
                if (CurrentWaypointId != 0)
                    pw.WriteUInt(CurrentWaypointId);
                else
                    pw.WriteNoneStruct();
        }
    }
}
