using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Extensions;
    using Game;
    using Packets;
    using Packets.ClientMethod.Server;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Structures;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public readonly Dictionary<ulong, Dropship> Dropships = new Dictionary<ulong, Dropship>();

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
                            _instance = new DynamicObjectManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private DynamicObjectManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
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
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 10000));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 10000));

                        obj.TrigeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Lockbox:
                    {
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 100));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 100));

                        obj.TrigeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Logos:
                    {
                        var actionData = new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 10000);
                        actionData.SourceId = obj.EntityId;

                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 10000));
                        client.Player.MapChannel.PerformRecovery.Add(actionData);

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

            // dynamicObjects
            foreach (var dynamicObject in mapChannel.DynamicObjects)
            {
                // spawn object
                if (!dynamicObject.IsInWorld)
                {
                    dynamicObject.RespawnTime -= delta;

                    if (dynamicObject.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, dynamicObject);
                        dynamicObject.IsInWorld = true;
                        dynamicObject.StateId = UseObjectState.IdStateActive;
                        dynamicObject.WindupTime = 10000;
                    }
                }
            }
        }

        internal void DynamicObjectProximityWorker(MapChannel mapChannel, DynamicObject obj, long delta)
        {
            switch (obj.EntityClassId)
            {
                // Human waypoint
                case EntityClasses.UsableTwoStateHumWaypointV01:
                    {
                        // check for players that enter range
                        PlayerEnterWaypoint(obj);

                        // check for players that leave range
                        PlayerExitWaypoint(obj);

                        break;
                    }
                // Control point
                case (EntityClasses)3814:
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

            if (dynamicObject.EntityClassId == 0)
                return;
            var classInfo = EntityClassManager.Instance.GetClassInfo(EntityManager.Instance.GetEntityClassId(dynamicObject.EntityId));

            if (classInfo == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(classInfo.TargetFlag),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Rotation),
                // set state
                new UsableInfoPacket(dynamicObject.IsEnabled, dynamicObject.StateId, 0, dynamicObject.WindupTime, dynamicObject.ActivateMission)
        };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
        }

        internal void CellDiscardDynamicObjectToClients(ulong entityId, List<Client> clients)
        {
            if (entityId == 0)
                return;

            foreach (var client in clients)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
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
            CellManager.Instance.RemoveFromWorld(mapChannel, dynObject);

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
                Rotation = 3.05f,
                MapContextId = 1220,
                EntityClassId = (EntityClasses)26486,
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
                    if (client.Player == action.Actor)
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

        #region Dropship
        public void DropshipsWorker(MapChannel mapChannel, long timePassed)
        {
            foreach (var entry in Dropships)
            {
                var dropship = entry.Value;

                if (dropship.DropshipType != DropshipType.Spawner && dropship.DropshipType != DropshipType.Teleporter)
                {
                    Logger.WriteLog(LogType.Debug, $"error dropshiptype {dropship.DropshipType}");
                    return;
                }

                dropship.PhaseTimeleft -= timePassed;

                if (dropship.PhaseTimeleft > 0)
                    continue;

                if (dropship.Phase == 0 || dropship.Phase == 1 || dropship.Phase == 4)
                    CellManager.Instance.CellCallMethod(dropship, new ForceStatePacket(dropship.StateId, 0));

                switch (dropship.Phase)
                {
                    case 0:
                        dropship.Phase = 1;
                        dropship.StateId = UseObjectState.CsStateSpawn;
                        break;
                    case 1:
                        dropship.Phase = 2;
                        dropship.PhaseTimeleft = 2000;
                        break;
                    case 2:
                        dropship.Phase = 3;

                        if (dropship.DropshipType == DropshipType.Teleporter)
                        {
                            if (dropship.Client.State == ClientState.Ingame)
                            {
                                CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new PreTeleportPacket(TeleportType.Default));
                                dropship.Client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
                            }
                        }

                        if (dropship.DropshipType == DropshipType.Spawner)
                        {
                            // spawn creatures
                            for (var i = 0; i < dropship.SpawnPool.QueuedCreatures; i++)
                            {
                                var creature = CreatureManager.Instance.CreateCreature(dropship.SpawnPool.DbId, dropship.SpawnPool);

                                if (creature == null)
                                    continue;

                                SpawnPoolManager.Instance.RandomizePosition(creature, dropship.SpawnPool.QueuedCreatures);

                                CellManager.Instance.AddToWorld(mapChannel, creature);
                            }
                            SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(dropship.SpawnPool, dropship.SpawnPool.QueuedCreatures);
                        }

                        break;
                    case 3:
                        dropship.PhaseTimeleft = 3000;
                        dropship.Phase = 4;
                        dropship.StateId = UseObjectState.CsStateEnd;
                        break;
                    case 4:
                        dropship.Phase = 5;
                        dropship.PhaseTimeleft = 5000;

                        if (dropship.DropshipType == DropshipType.Teleporter)
                            if (dropship.Client.State == ClientState.Teleporting)
                                dropship.Client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                        break;
                    case 5:
                        if (dropship.DropshipType == DropshipType.Teleporter)
                        {
                            switch (dropship.Client.State)
                            {
                                case ClientState.Ingame:
                                    CellManager.Instance.RemoveFromWorld(dropship.Client);
                                    dropship.Client.Player.MapChannel.ClientList.Remove(dropship.Client);
                                    dropship.Client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                                    dropship.Client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
                                    dropship.Client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket(dropship.DestinationMapId, 1, MapChannelManager.Instance.MapChannelArray[dropship.DestinationMapId].MapInfo.MapVersion, dropship.Destination, 0));
                                    dropship.Client.Player.Position = dropship.Destination;
                                    dropship.Client.State = ClientState.Teleporting;
                                    break;
                                case ClientState.Teleporting:
                                    dropship.Client.State = ClientState.Ingame;
                                    break;
                                default:
                                    Logger.WriteLog(LogType.Error, $"Unsupported CLientState {dropship.Client.State}");
                                    break;
                            }
                        }

                        if (dropship.DropshipType == DropshipType.Spawner)
                            SpawnPoolManager.Instance.DecreaseQueueCount(dropship.SpawnPool);

                        // remove object
                        CellManager.Instance.RemoveFromWorld(mapChannel, dropship);

                        Dropships.Remove(dropship.EntityId);
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"Unsupported phase {dropship.Phase}");
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
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var footlockers = unitOfWork.Footlockers.GetFootlockers();

            foreach (var footlocker in footlockers)
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(footlocker.MapContextId);

                var newFootlocker = new DynamicObject
                {
                    Position = footlocker.Position,
                    Rotation = footlocker.Rotation,
                    MapContextId = footlocker.MapContextId,
                    EntityClassId = (EntityClasses)footlocker.ClassId,
                    DynamicObjectType = DynamicObjectType.Lockbox,
                    Comment = footlocker.Comment
                };

                mapChannel.FootLockers.Add(footlocker.Id, newFootlocker);
            }
        }

        #endregion

        #region Logos
        internal void LogosRecovery(MapChannel mapChannel, ActionData action)
        {
            foreach (var obj in mapChannel.DynamicObjects)
            {
                foreach (var client in obj.TrigeredByPlayers)
                    if (client.Player == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            obj.TrigeredByPlayers.Remove(client);
                            //CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        obj.TrigeredByPlayers.Remove(client);
                        CellManager.Instance.CellCallMethod(obj, new UsableInfoPacket(true, obj.StateId, 0, 10000, 0));

                        var logosId = 0u;
                        foreach (var entry in mapChannel.DynamicObjects)
                        {
                            var logos = entry as Logos;
                            if (action.SourceId == logos.EntityId)
                            {
                                logosId = logos.Id;
                                break;
                            }
                        }

                        var haveLogos = false;
                        foreach (var logos in client.Player.Logos)
                        {
                            if (logos == logosId)
                            {
                                haveLogos = true;
                                break;
                            }
                        }

                        if (!haveLogos)
                            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Logos, logosId);

                        break;
                    }
            }
        }
        #endregion

        #region Waypoint

        internal void InitTeleporters()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var teleporters = unitOfWork.Teleporters.GetTeleporters();

            foreach (var teleporter in teleporters)
            {
                if (teleporter.MapContextId == 0)
                    continue;

                var mapChannel = MapChannelManager.Instance.FindByContextId(teleporter.MapContextId);

                var newTeleporter = new DynamicObject
                {
                    Position = teleporter.Position,
                    Rotation = teleporter.Rotation,
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = (EntityClasses)teleporter.ClassId,
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
            if (client.Player.GainedWaypoints.Contains(objectData.WaypointId))
            {
                return;
            }

            // add waypoint to player as he entered for the first time
            client.CallMethod(client.Player.EntityId, new WaypointGainedPacket(objectData.WaypointId, objectData.WaypointType));
            client.Player.GainedWaypoints.Add(objectData.WaypointId);

            // update Db
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, objectData.WaypointId);
        }

        internal List<MapWaypointInfoList> CreateListOfWaypoints(Client client, MapChannel mapChannel)
        {
            var listOfWaypoints = new List<MapWaypointInfoList>();
            var listOfMapInstances = new List<MapInstanceInfo>();
            var waypointInfo = new List<WaypointInfo>();

            // create waypoint list for player
            foreach (var teleporter in mapChannel.Teleporters)
            {
                var teleporterData = teleporter.Value.ObjectData as WaypointInfo;

                foreach (var waypointId in client.Player.GainedWaypoints)
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

            listOfMapInstances.Add(new MapInstanceInfo(1, mapChannel.MapInfo.MapContextId, MapInstanceStatus.Low)); // ToDo: send mapInstanceStatus based on map population
            listOfWaypoints.Add(new MapWaypointInfoList(mapChannel.MapInfo.MapContextId, listOfMapInstances, waypointInfo));

            return listOfWaypoints;
        }

        internal void SelectWaypoint(Client client, SelectWaypointPacket packet)
        {
            if (packet.MapInstanceId != client.Player.MapContextId)
            {
                var dropship = new Dropship(Factions.AFS, DropshipType.Teleporter, client, MapChannelManager.Instance.MapChannelArray[packet.MapInstanceId].Teleporters[packet.WaypointId].Position, packet.MapInstanceId);

                CellManager.Instance.AddToWorld(client.Player.MapChannel, dropship);
                Dropships.Add(dropship.EntityId, dropship);
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());

                client.LoadingMap = packet.MapInstanceId;
                return;
            }

            var teleporter = client.Player.MapChannel.Teleporters[packet.WaypointId];
            var objData = teleporter.ObjectData as WaypointInfo;
            var movementData = new Models.Movement
                (
                new Vector3(
                    teleporter.Position.X,
                    teleporter.Position.Y + 1,
                    teleporter.Position.Z),
                0f,
                0,
                new Vector2((float)teleporter.Rotation, 0f)
            );

            client.CellCallMethod(client, client.Player.EntityId, new PreTeleportPacket(TeleportType.Default));
            client.CallMethod(client.Player.EntityId, new TeleportPacket(teleporter.Position, teleporter.Rotation, TeleportType.Default, 5));
            client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
            client.CellMoveObject(client, new MoveObjectMessage(client.Player.EntityId, movementData), false);

            teleporter.TrigeredByPlayers.Remove(client);    // ToDO: maybe safely remove client
        }

        internal void TeleportAcknowledge(Client client)
        {
            client.CallMethod(client.Player.EntityId, new TeleportArrivalPacket());
        }

        internal void PlayerEnterWaypoint(DynamicObject obj)
        {
            var cellSeed = CellManager.Instance.GetCellSeed(obj.Position);
            var mapChannel = MapChannelManager.Instance.FindByContextId(obj.MapContextId);

            foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
            {
                // check if player is near waypoint
                if (!client.Player.IsNear2m(obj))
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

                if (!client.Player.IsNear2m(obj))
                {
                    obj.TrigeredByPlayers.RemoveAt(i);

                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
            }
        }
        #endregion
    }
}
