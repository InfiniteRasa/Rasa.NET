using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets;
    using Structures;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();

        public static DynamicObjectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new DynamicObjectManager();
                    }
                }

                return _instance;
            }
        }

        private DynamicObjectManager()
        {
        }

        public void InitDynamicObjects()
        {
            InitTeleporters();
        }

        public void InitTeleporters()
        {
            var teleporters = TeleporterTable.GetTeleporters();

            foreach (var teleporter in teleporters)
            {
                if (teleporter.MapContextId == 0)
                    continue;

                var mapChannel = MapChannelManager.Instance.FindByContextId(teleporter.MapContextId);

                var newTeleporter = new DynamicObject
                {
                    Position = new Vector3(teleporter.CoordX, teleporter.CoordY, teleporter.CoordZ),
                    Orientation = teleporter.Orientation,
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = (EntityClassId)teleporter.EntityClassId,
                    ObjectData = new WaypointInfo(teleporter.Id, false, teleporter.Type)
                };

                mapChannel.Teleporters.Add(teleporter.Id, newTeleporter);
            }
        }

        public void ForceState(Client client, uint entityId, uint state)
        {
            client.CallMethod(entityId, new ForceStatePacket(state, 200));
        }

        public void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            client.CallMethod((uint)packet.EntityId, new UsePacket(client.MapClient.Player.Actor.EntityId, 1, 0));
        }

        public void DynamicObjectWorker(MapChannel mapChannel, long delta)
        {
            // dynamicObjects
            // dropShips
            // footlockers
            // etc...

            // teleporters
            foreach (var entry  in mapChannel.Teleporters)
            {
                var teleporter = entry.Value;
                // spawn object
                if (!teleporter.IsInWorld)
                {
                    teleporter.RespawnTime -= delta;

                    if (teleporter.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, teleporter);
                        teleporter.IsInWorld = true;
                    }
                }

                // check for players neer object
                DynamicObjectProximityWorker(mapChannel, teleporter, delta);
            }
        }

        private void DynamicObjectProximityWorker(MapChannel mapChannel, DynamicObject obj, long delta)
        {
            switch (obj.EntityClassId)
            {
                // Human waypoint
                case EntityClassId.UsableTwoStateHumWaypointV01:
                    {
                        var cellSeed = CellManager.Instance.GetCellSeed(obj.Position);

                        // check for players that enter range
                        foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                        {
                            if (Vector3.Distance(obj.Position, client.MapClient.Player.Actor.Position) < 2.0f)
                            {
                                var alreadyInList = false;

                                foreach (var player in obj.TrigeredByPlayers)
                                    if (client == player)
                                    {
                                        alreadyInList = true;
                                        break;
                                    }

                                if (!alreadyInList)
                                {
                                    obj.TrigeredByPlayers.Add(client);

                                    var objectData = (WaypointInfo)obj.ObjectData;

                                    PlayerHasWaypoint(client, objectData);

                                    var waypointInfoList = CreateListOfWaypoints(client, mapChannel);

                                    client.CallMethod(SysEntity.ClientMethodId, new EnteredWaypointPacket(obj.MapContextId, obj.MapContextId, waypointInfoList, objectData.WaypointType, objectData.WaypointId));

                                    if (obj.StateId == 0 || obj.StateId == 55)
                                    {
                                        obj.StateId = 56;

                                        client.CellCallMethod(client, obj.EntityId, new ForceStatePacket(obj.StateId, 100));
                                    }
                                }
                            }
                        }

                        // check for players that leave range
                        if (obj.TrigeredByPlayers.Count > 0)
                        {
                            for (var i = obj.TrigeredByPlayers.Count - 1; i >= 0; i--)
                            {
                                var client = obj.TrigeredByPlayers[i];

                                if (Vector3.Distance(obj.Position, client.MapClient.Player.Actor.Position) > 2.0f)
                                {
                                    obj.TrigeredByPlayers.RemoveAt(i);

                                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());

                                    if (obj.TrigeredByPlayers.Count == 0)
                                    {
                                        obj.StateId = 55;

                                        client.CellCallMethod(client, obj.EntityId, new ForceStatePacket(obj.StateId, 100));
                                    }
                                }
                            }
                        }

                        break;
                    }
                default:
                    break;
            }
        }

        // 1 object to n client's
        public void CellIntroduceDynamicObjectToClients(DynamicObject dynamicObject, List<Client> listOfClients)
        {
            foreach (var client in listOfClients)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        // n creatures to 1 client
        public void CellIntroduceDynamicObjectsToClient(Client client, List<DynamicObject> listOfObjects)
        {
            foreach (var dynamicObject in listOfObjects)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        public void CreateDynamicObjectOnClient(Client client, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo((EntityClassId)EntityManager.Instance.GetEntityClassId(dynamicObject.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Orientation)
            };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
        }

        public void CellDiscardDynamicObjectsToClient(Client client, List<DynamicObject> discardObjects)
        {
            foreach (var dynamicObject in discardObjects)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(dynamicObject.EntityId));
        }

        #region Waypoint

        public void PlayerHasWaypoint(Client client, WaypointInfo objectData)
        {
            var hasWaypoint = false;
            var waypointInfoList = new List<MapWaypointInfoList>();

            // check if player have curent waypoint
            foreach (var waypointId in client.MapClient.Player.GainedWaypoints)
            {
                if (waypointId == objectData.WaypointId)
                {
                    hasWaypoint = true;
                    break;
                }
            }

            if (!hasWaypoint)
            {
                // add waypoint to player if he etered first time
                client.CallMethod(client.MapClient.Player.Actor.EntityId, new WaypointGainedPacket(objectData.WaypointId, objectData.WaypointType));
                client.MapClient.Player.GainedWaypoints.Add(objectData.WaypointId);

                // update Db
                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, objectData.WaypointId);

                hasWaypoint = true;
            }
        }

        public List<MapWaypointInfoList> CreateListOfWaypoints(Client client, MapChannel mapChannel)
        {
            var listOfWaypoints = new List<MapWaypointInfoList>();
            var waypointInfo = new List<WaypointInfo>();

            // create waypoint list for player
            foreach (var teleporter in mapChannel.Teleporters)
            {
                var teleporterData = teleporter.Value.ObjectData as WaypointInfo;

                foreach (var waypointId in client.MapClient.Player.GainedWaypoints)
                {
                    if (waypointId == teleporterData.WaypointId)
                    {
                        waypointInfo.Add(new WaypointInfo(teleporterData.WaypointId, teleporterData.Contested, teleporterData.WaypointType)
                        {
                            Position = teleporter.Value.Position
                        });

                        break;
                    }
                }
            }

            listOfWaypoints.Add(new MapWaypointInfoList(mapChannel.MapInfo.MapContextId, waypointInfo));

            return listOfWaypoints;
        }

        public void SelectWaypoint(Client client, SelectWaypointPacket packet)
        {
            var teleporter = client.MapClient.MapChannel.Teleporters[packet.WaypointId];
            var objData = teleporter.ObjectData as WaypointInfo;

            client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new PreTeleportPacket(TeleportType.Default));
            client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
            
            //client.CallMethod(client.MapClient.Player.Actor.EntityId, new TeleportPacket(teleporter.Position, client.MoveMessage.ViewX, TeleportType.Default, 4));
        }

        public void TeleportAcknowledge(Client client)
        {
            Logger.WriteLog(LogType.AI, "teleporting???");
            //
            //client.CallMethod(client.MapClient.Player.Actor.EntityId, new TeleportArrivalPacket());
        }

        #endregion
    }
}
