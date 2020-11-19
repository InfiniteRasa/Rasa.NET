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
            InitControlPoints();
            InitFootlockers();
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
                case DynamicObjectType.ControlPoint:
                    {
                        client.CallMethod(client.MapClient.Player.Actor.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.MapClient.Player.Actor.EntityId, obj.StateId, 10000));
                        client.MapClient.MapChannel.PerformRecovery.Add(new ActionData(client.MapClient.Player.Actor, packet.ActionId, packet.ActionArgId, 10000));

                        obj.TrigeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Lockbox:
                    {
                        client.CallMethod(client.MapClient.Player.Actor.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.MapClient.Player.Actor.EntityId, obj.StateId, 100));
                        client.MapClient.MapChannel.PerformRecovery.Add(new ActionData(client.MapClient.Player.Actor, packet.ActionId, packet.ActionArgId, 100));

                        obj.TrigeredByPlayers.Add(client);
                        break;
                    }
                default:
                    Logger.WriteLog(LogType.Debug, $"ToDo: RequestUseObjectPacket: unsuported object type {obj.DynamicObjectType}");
                    break;
            }
        }

        internal void DynamicObjectWorker(MapChannel mapChannel, long delta)
        {
            // dynamicObjects
            // dropShips
            // etc...

            // controlPoints
            foreach (var entry in mapChannel.ControlPoints)
            {
                var controlPoint = entry.Value;
                // spawn object
                if (!controlPoint.IsInWorld)
                {
                    controlPoint.RespawnTime -= delta;

                    if (controlPoint.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, controlPoint);
                        controlPoint.IsInWorld = true;
                        controlPoint.StateId = UseObjectState.CpointStateUnclaimed;
                        controlPoint.WindupTime = 10000;
                    }
                }

                // check for players neer object
                DynamicObjectProximityWorker(mapChannel, controlPoint, delta);
            }

            // footlocker
            foreach (var entry in mapChannel.FootLockers)
            {
                var footlocker = entry.Value;
                // spawn object
                if (!footlocker.IsInWorld)
                {
                    footlocker.RespawnTime -= delta;

                    if (footlocker.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, footlocker);
                        footlocker.IsInWorld = true;
                        footlocker.StateId = UseObjectState.CpointStateUnclaimed;
                        footlocker.WindupTime = 10000;
                    }
                }
            }

            // teleporters
            foreach (var entry in mapChannel.Teleporters)
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

        #region ControlPoint

        internal void InitControlPoints()
        {
            //var contolPoints = ControlPointTable.GetControlPoints();
            var mapChannel = MapChannelManager.Instance.FindByContextId(1220);

            var newControlPoint = new DynamicObject
            {
                Position = new Vector3(197.66f, 162.27f, -54.08f),
                Orientation = 3.05f,
                MapContextId = 1220,
                EntityClassId = (EntityClassId)26486,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                ObjectData = new ControlPointStatus(215, 1, 1, 30000)
            };

            newControlPoint.DynamicObjectType = DynamicObjectType.ControlPoint;

            mapChannel.ControlPoints.Add(1, newControlPoint);
        }

        internal void CaptureControlPointRecovery(MapChannel mapChannel, ActionData action)
        {
            foreach (var entry in mapChannel.ControlPoints)
            {
                var controlpoint = entry.Value;

                foreach (var client in controlpoint.TrigeredByPlayers)
                    if (client.MapClient.Player.Actor == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            controlpoint.TrigeredByPlayers.Remove(client);
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        controlpoint.TrigeredByPlayers.Remove(client);
                        controlpoint.Faction = controlpoint.Faction == Factions.AFS ? Factions.Bane : Factions.AFS;
                        controlpoint.StateId = controlpoint.StateId == UseObjectState.CpointStateFactionAOwned ? UseObjectState.CpointStateFactionBOwned : UseObjectState.CpointStateFactionAOwned;

                        CellManager.Instance.CellCallMethod(controlpoint, new ForceStatePacket(controlpoint.StateId, 100));
                        CellManager.Instance.CellCallMethod(controlpoint, new UsableInfoPacket(true, controlpoint.StateId, 0, 10000, 0));
                        break;
                    }
            }
        }

        #endregion

        #region Footlocker

        internal void FootlockerRecovery(MapChannel mapChannel, ActionData action)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: FootlockerRecovery, ActionId = {action.ActionId} ActionArgId = {action.ActionArgId}");
        }

        internal void InitFootlockers()
        {
            var footlockers = FootlockersTable.LoadFootlockers();

            foreach (var footlocker in footlockers)
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(footlocker.MapContextId);

                var newFootlocker = new DynamicObject
                {
                    Position = new Vector3(footlocker.CoordX, footlocker.CoordY, footlocker.CoordZ),
                    Orientation = footlocker.Orientation,
                    MapContextId = footlocker.MapContextId,
                    EntityClassId = (EntityClassId)footlocker.EntityClassId,
                    DynamicObjectType = DynamicObjectType.Lockbox,
                    Comment = footlocker.Comment
                };

                mapChannel.FootLockers.Add(footlocker.Id, newFootlocker);
            }
        }

        #endregion

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
                    Comment = teleporter.Description,
                    ObjectData = new WaypointInfo(teleporter.Id, false, teleporter.Type)
                };

                switch (teleporter.Type)
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

        internal void CheckPlayerWaypoint(Client client, WaypointInfo objectData)
        {
            // check if player has requested waypoint
            if (client.MapClient.Player.GainedWaypoints.Contains(objectData.WaypointId))
            {
                return;
            }

            // add waypoint to player as he entered for the first time
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new WaypointGainedPacket(objectData.WaypointId, objectData.WaypointType));
            client.MapClient.Player.GainedWaypoints.Add(objectData.WaypointId);

            // update Db
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, objectData.WaypointId);
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
                // check if player is near waypoint
                if (!client.MapClient.Player.Actor.IsNear(obj))
                {
                    continue;
                }

                // check if already added
                if (obj.TrigeredByPlayers.Any(p => p == client))
                {
                    continue;
                }

                // if not add him and send enter packet
                obj.TrigeredByPlayers.Add(client);

                var objectData = (WaypointInfo)obj.ObjectData;

                CheckPlayerWaypoint(client, objectData);

                var waypointInfoList = CreateListOfWaypoints(client, mapChannel);

                client.CallMethod(SysEntity.ClientMethodId, new EnteredWaypointPacket(obj.MapContextId, obj.MapContextId, waypointInfoList, objectData.WaypointType, objectData.WaypointId));

                // check if we already added him to the waypoint
            }
        }

        internal void PlayerExitWaypoint(DynamicObject obj)
        {
            for (var i = obj.TrigeredByPlayers.Count - 1; i >= 0; i--)
            {
                var client = obj.TrigeredByPlayers[i];

                if (!client.MapClient.Player.Actor.IsNear(obj))
                {
                    obj.TrigeredByPlayers.RemoveAt(i);

                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
            }
        }

        #endregion
    }
}
