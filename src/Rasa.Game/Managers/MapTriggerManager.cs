using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Extensions;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    public class MapTriggerManager
    {
        private static MapTriggerManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly DynamicObjectManager _objects;
        private DynamicObjectManager Objects => _objects ?? DynamicObjectManager.Instance;
        public static MapTriggerManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapTriggerManager();
                    }
                }

                return _instance;
            }
        }

        public MapTriggerManager(DynamicObjectManager objects = null)
        {
            _objects = objects;
        }

        internal void MapTriggerInit()
        {
        }

        /// <summary>What a player standing in a one-way exit is told when it opens under them.</summary>
        internal const string ExitReadyMessage =
            "The extraction beam is ready. Step out of it and back in when you are ready to leave; you cannot come back.";

        /// <summary>
        /// Ordinary pads grant discovery and open travel. A ready one-way extraction
        /// starts its authored departure without joining the waypoint network.
        ///
        /// Only stepping into it boards: leaving is one-way, so a beam that opens under someone
        /// already standing in it - handing in the last objective there - does not take them. It
        /// did nothing at all, and the player had no way to know they had to step out and back
        /// in; they are now told so, once each time it opens under them.
        /// </summary>
        internal void PlayerEnterTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (client.State == ClientState.Ingame && client.PendingTransfer == null &&
                client.Player?.MapChannel?.MapInfo.MapContextId == mapTrigger.MapContextId &&
                client.Player.IsNear5m(mapTrigger))
            {
                if (Objects.IsOneWayExit(mapTrigger.MapContextId, mapTrigger.TriggerId))
                {
                    var entered = mapTrigger.ExitOccupants.Add(client);

                    if (!Objects.IsStationAvailable(client, mapTrigger.MapContextId, mapTrigger.TriggerId))
                        mapTrigger.ExitWaiting.Add(client);
                    else if (entered)
                        Objects.BeginOneWayDeparture(client);
                    else if (mapTrigger.ExitWaiting.Remove(client))
                        CommunicatorManager.Instance.SystemMessage(client, ExitReadyMessage);
                    return;
                }
                if (mapTrigger.TriggeredBy.Contains(client))
                    return;
                if (!Objects.IsStationAvailable(client, mapTrigger.MapContextId, mapTrigger.TriggerId))
                    return;

                if (!Objects.Teleporters.TryGetValue(mapTrigger.TriggerId, out var station) ||
                    station.ObjectData is not WaypointInfo waypoint || waypoint.WaypointType != WaypointType.Dropship)
                {
                    Logger.WriteLog(LogType.Error, $"Missing dropship definition for trigger {mapTrigger.TriggerId}.");
                    return;
                }
                Objects.CheckPlayerWaypoint(client, waypoint);
                mapTrigger.TriggeredBy.Add(client);

                var dropshipInfoList = Objects.CreateListOfDropships(client, mapTrigger.TriggerId);

                client.CallMethod(SysEntity.ClientMethodId,
                    new EnteredWaypointPacket(client.Player.MapChannel.MapInfo.MapContextId, mapTrigger.MapContextId,
                        dropshipInfoList, WaypointType.Dropship, mapTrigger.TriggerId));
            }
        }
        internal void PlayerExitTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (Objects.IsOneWayExit(mapTrigger.MapContextId, mapTrigger.TriggerId))
            {
                if (client.State != ClientState.Ingame || client.PendingTransfer != null ||
                    client.Player?.MapChannel?.MapInfo.MapContextId != mapTrigger.MapContextId ||
                    !client.Player.IsNear5m(mapTrigger))
                {
                    mapTrigger.ExitOccupants.Remove(client);
                    mapTrigger.ExitWaiting.Remove(client);
                }
                return;
            }
            if (client.State != ClientState.Ingame || client.PendingTransfer != null ||
                client.Player?.MapChannel?.MapInfo.MapContextId != mapTrigger.MapContextId ||
                !Objects.IsStationAvailable(client, mapTrigger.MapContextId, mapTrigger.TriggerId) ||
                !client.Player.IsNear5m(mapTrigger))
                if (mapTrigger.TriggeredBy.Contains(client))
                {
                    mapTrigger.TriggeredBy.Remove(client);
                    if (client.State != ClientState.Disconnected)
                        client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
        }

        internal void TriggersProximityWorker(MapChannel mapChannel)
        {
            var triggers = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.MapTriggers).Distinct().ToArray();
            foreach (var trigger in triggers)
            {
                foreach (var previous in trigger.TriggeredBy.Concat(trigger.ExitOccupants).ToArray())
                    PlayerExitTriggerRange(previous, trigger);
                foreach (var client in mapChannel.ClientList.ToArray())
                {
                    if (client?.Player != null && !client.Player.Disconected)
                        PlayerEnterTriggerRange(client, trigger);
                }
            }
        }
    }
}
