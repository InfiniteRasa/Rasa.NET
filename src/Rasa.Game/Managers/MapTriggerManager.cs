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

        /// <summary>
        /// Ordinary pads grant discovery and open travel. A ready one-way extraction
        /// starts its authored departure without joining the waypoint network.
        /// </summary>
        internal void PlayerEnterTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (client.State == ClientState.Ingame && client.PendingTransfer == null &&
                client.Player?.MapChannel?.MapInfo.MapContextId == mapTrigger.MapContextId &&
                client.Player.IsNear5m(mapTrigger))
            {
                if (Objects.IsOneWayExit(mapTrigger.MapContextId, mapTrigger.TriggerId))
                {
                    if (mapTrigger.ExitOccupants.Add(client) &&
                        Objects.IsStationAvailable(client, mapTrigger.MapContextId, mapTrigger.TriggerId))
                        Objects.BeginOneWayDeparture(client);
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
                    new EnteredWaypointPacket(client.Player.MapChannel.InstanceId, mapTrigger.MapContextId,
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
                    mapTrigger.ExitOccupants.Remove(client);
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
