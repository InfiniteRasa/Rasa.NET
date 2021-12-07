using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Extensions;
    using Game;
    using Packets;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;

    public class MapTriggerManager
    {
        private static MapTriggerManager _instance;
        private static readonly object InstanceLock = new object();

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

        public MapTriggerManager()
        {
        }

        internal void MapTriggerInit()
        {
            var triggers = new List<MapTrigger>();

            triggers.Add(new MapTrigger(267, "DropshipPad Twin Pillars", new Vector3(-60f, 221.269f, -471f), 0, 1220));
            triggers.Add(new MapTrigger(99, "DropshipPad Bootcamp", new Vector3(-225.353f, 99.597f, -70.5246f), 0, 1985));
            triggers.Add(new MapTrigger(249, "DropshipPad Foreas Base", new Vector3(-81f, 119.5f, 640.5f), 0, 1148));

            foreach (var trigger in triggers)
                CellManager.Instance.AddToWorld(MapChannelManager.Instance.MapChannelArray[trigger.MapContextId], trigger);
        }

        internal void PlayerEnterTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (client.MapClient.Player.Actor.IsNear5m(mapTrigger))
            {
                if (mapTrigger.TrigeredBy.Contains(client))
                    return;

                mapTrigger.TrigeredBy.Add(client);

                var listOfDropships1 = new List<WaypointInfo>();
                var listOfDropships2 = new List<WaypointInfo>();
                var listOfDropships3 = new List<WaypointInfo>();
                var dropshipInfoList = new List<MapWaypointInfoList>();

                listOfDropships1.Add(new WaypointInfo(99, false, new Vector3(-225.353f, 99.597f, -70.5246f), 1));
                listOfDropships2.Add(new WaypointInfo(267, false, new Vector3(- 60f, 221.269f, -471f), 1));
                listOfDropships3.Add(new WaypointInfo(249, false, new Vector3(-81f, 119.5f, 640.5f), 1));

                dropshipInfoList.Add(new MapWaypointInfoList(1985, new List<MapInstanceInfo> { new MapInstanceInfo(1, 1985, MapInstanceStatus.Low) }, listOfDropships1));
                dropshipInfoList.Add(new MapWaypointInfoList(1220, new List<MapInstanceInfo> { new MapInstanceInfo(1, 1220, MapInstanceStatus.Low) }, listOfDropships2));
                dropshipInfoList.Add(new MapWaypointInfoList(1148, new List<MapInstanceInfo> { new MapInstanceInfo(1, 1148, MapInstanceStatus.Low) }, listOfDropships3));

                client.CallMethod(SysEntity.ClientMethodId, new EnteredWaypointPacket(mapTrigger.MapContextId, mapTrigger.MapContextId, dropshipInfoList, 1, mapTrigger.TriggerId));
            }
        }
        internal void PlayerExitTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (!client.MapClient.Player.Actor.IsNear5m(mapTrigger))
                if (mapTrigger.TrigeredBy.Contains(client))
                {
                    mapTrigger.TrigeredBy.Remove(client);
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
        }

        internal void TriggersProximityWorker(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList)
            {
                if (client.MapClient.Disconected || client.MapClient.Player == null || client.State == ClientState.Loading)
                    continue;

                var cell = mapChannel.MapCellInfo.Cells[client.MapClient.Player.Actor.Cells[2, 2]];

                foreach (var mapTrigger in cell.MapTriggers)
                {
                    // check for players that enter range
                    PlayerEnterTriggerRange(client, mapTrigger);
    
                    // check for players that leave range
                    PlayerExitTriggerRange(client, mapTrigger);
                }
            }
        }
    }
}
