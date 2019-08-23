using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
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

        internal void InitDynamicObjects()
        {
            InitTeleporters();
        }

        internal void ForceState(DynamicObject obj, UseObjectState state, int delta)
        {
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(state, delta));
        }

        internal void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            var obj = EntityManager.Instance.GetObject(packet.EntityId);

            switch (obj.DynamicObjectType)
            {
                default:
                    Logger.WriteLog(LogType.Debug, $"ToDo: RequestUseObjectPacket: unsuported object type {obj.DynamicObjectType}");
                    break;
            }
        }

        internal void DynamicObjectWorker(MapChannel mapChannel, long delta)
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
                        teleporter.StateId = UseObjectState.TsState1;
                    }
                }

                // check for players neer object
                DynamicObjectProximityWorker(mapChannel, teleporter, delta);
            }
        }

        internal void DynamicObjectProximityWorker(MapChannel mapChannel, DynamicObject obj, long delta)
        {
            switch (obj.EntityClassId)
            {
                // Human waypoint
                case EntityClassId.UsableTwoStateHumWaypointV01:
                    {
                        // check for players that enter range
                        PlayerEnterWaypoint(obj);

                        // check for players that leave range
                        PlayerExitWaypoint(obj);

                        break;
                    }
                // Control point
                case (EntityClassId)3814:
                    break;
                default:
                    break;
            }
        }
        
        // 1 object to n client's
        internal void CellIntroduceDynamicObjectToClients(DynamicObject dynamicObject, List<Client> listOfClients)
        {
            foreach (var client in listOfClients)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        // n objects to 1 client
        internal void CellIntroduceDynamicObjectsToClient(Client client, List<DynamicObject> listOfObjects)
        {
            foreach (var dynamicObject in listOfObjects)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        internal void CreateDynamicObjectOnClient(Client client, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(EntityManager.Instance.GetEntityClassId(dynamicObject.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Orientation),
                // set state
                new UsableInfoPacket(dynamicObject.IsEnabled, dynamicObject.StateId, 0, dynamicObject.WindupTime, dynamicObject.ActivateMission)
        };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
        }

        internal void CellDiscardDynamicObjectsToClient(Client client, List<DynamicObject> discardObjects)
        {
            foreach (var dynamicObject in discardObjects)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(dynamicObject.EntityId));
        }

        /* Destroys an object on client and serverside
         * Frees the memory and informs clients about removal
         */
        internal void DynamicObjectDestroy(MapChannel mapChannel, DynamicObject dynObject)
        {
            // TODO, check timers
            // remove from world
            EntityManager.Instance.UnregisterEntity(dynObject.EntityId);
            CellManager.RemoveFromWorld(mapChannel, dynObject);
            
            // destroy callback
            Logger.WriteLog(LogType.Debug, "ToDO remove dynamic object from server");
        }

        #region Waypoint

        internal void InitTeleporters()
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

                switch(teleporter.Type)
                {
                    case 1:
                        newTeleporter.DynamicObjectType = DynamicObjectType.LocalTeleporter;
                        break;
                    case 2:
                        newTeleporter.DynamicObjectType = DynamicObjectType.Waypoint;
                        break;
                    case 3:
                        newTeleporter.DynamicObjectType = DynamicObjectType.Wormhole;
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"InitTeleporters: unsuported teleporter type {teleporter.Type}");
                        break;
                }
                mapChannel.Teleporters.Add(teleporter.Id, newTeleporter);
            }
        }

        internal void PlayerHasWaypoint(Client client, WaypointInfo objectData)
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

        internal List<MapWaypointInfoList> CreateListOfWaypoints(Client client, MapChannel mapChannel)
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

        internal void SelectWaypoint(Client client, SelectWaypointPacket packet)
        {
            var teleporter = client.MapClient.MapChannel.Teleporters[packet.WaypointId];
            var objData = teleporter.ObjectData as WaypointInfo;
            var movementData = new Memory.MovementData
                (
                    teleporter.Position.X,
                    teleporter.Position.Y + 1,
                    teleporter.Position.Z,
                    teleporter.Orientation
                );

            client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new PreTeleportPacket(TeleportType.Default));
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new TeleportPacket(teleporter.Position, teleporter.Orientation, TeleportType.Default, 5));
            client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
            client.CellMoveObject(client.MapClient, new MoveObjectMessage(client.MapClient.Player.Actor.EntityId, movementData), false);

            teleporter.TrigeredByPlayers.Remove(client);    // ToDO: maybe safely remove client
        }

        internal void TeleportAcknowledge(Client client)
        {
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new TeleportArrivalPacket());
        }

        internal void PlayerEnterWaypoint(DynamicObject obj)
        {
            var cellSeed = CellManager.Instance.GetCellSeed(obj.Position);
            var mapChannel = MapChannelManager.Instance.FindByContextId(obj.MapContextId);

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
                    }
                }
            }
        }

        internal void PlayerExitWaypoint(DynamicObject obj)
        {
            for (var i = obj.TrigeredByPlayers.Count - 1; i >= 0; i--)
            {
                var client = obj.TrigeredByPlayers[i];

                if (Vector3.Distance(obj.Position, client.MapClient.Player.Actor.Position) > 2.0f)
                {
                    obj.TrigeredByPlayers.RemoveAt(i);

                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
            }
        }

        #endregion
    }
}
