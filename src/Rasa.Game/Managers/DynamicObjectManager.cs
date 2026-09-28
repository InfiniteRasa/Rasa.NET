using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

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
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.World;
    using System;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly MapChannelManager _maps;
        private readonly Func<long> _clock;
        private readonly Action<Client, CharacterUpdate, object> _updateCharacter;
        private readonly Action<Client> _disconnect;
        private readonly MissionApplication _missionManager;
        private readonly CharacterManager _characterManager;
        private MapChannelManager Maps => _maps ?? MapChannelManager.Instance;
        private CharacterManager Characters => _characterManager ?? CharacterManager.Instance;

        public readonly Dictionary<ulong, Dropship> Dropships = new Dictionary<ulong, Dropship>();
        public readonly Dictionary<ulong, DynamicObject> Teleporters = new Dictionary<ulong, DynamicObject>();

        /// <summary>
        /// The UseObject arg id each kind of object is used with, which is what picks the recovery
        /// in ActorActionManager. The client reads it off the object's own usabledata row
        /// (client/augmentations/usable.py, defaulting to 1 for a class with no row), and it
        /// matches the object here: all 35 footlocker classes carry 1, all 39 station classes 5,
        /// 163 of the 166 logos classes 6, and the control point 7.
        /// </summary>
        public const uint FootlockerUseArgId = 1;

        /// <inheritdoc cref="FootlockerUseArgId"/>
        public const uint LogosUseArgId = 6;

        /// <inheritdoc cref="FootlockerUseArgId"/>
        public const uint ControlPointUseArgId = 7;
        internal const uint DefaultScenarioUseWindupMs = 10000;

        /// <summary>
        /// How far from an object a player may be and still use it.
        ///
        /// The client's own radius is the larger of the player's use range and the object class's
        /// own: an actor's use range is 3 and a manifestation doubles it
        /// (client/augmentations/manifestation.py GetUseRange), and of every usable class only one
        /// - 26232, which is none of the objects placed here - sets a range of its own. So an
        /// honest client asks from within 6 units, and measures them from itself to the object's
        /// DAMAGE1 connection point rather than to where the object stands, which is what the rest
        /// of this allowance is for: the server knows only where it stands, and the position it
        /// has for the player is a tick behind the one the client checked. Twenty units is
        /// generous about all of that and still refuses what is worth refusing, which is a request
        /// from across the map.
        /// </summary>
        public const float MaxUseDistance = 20f;

        /// <summary>Whether an actor is on the object's map and near enough to use it.</summary>
        private static bool IsInUseRange(Actor actor, DynamicObject obj)
        {
            var mapChannel = actor switch
            {
                Manifestation player => player.MapChannel,
                _ => actor?.RuntimeMapChannel
            };
            return MapInstanceScope.Share(mapChannel, actor, obj) &&
                   Vector3.Distance(actor.Position, obj.Position) <= MaxUseDistance;
        }

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

        internal DynamicObjectManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            MapChannelManager maps = null, Func<long> clock = null,
            Action<Client, CharacterUpdate, object> updateCharacter = null,
            Action<Client> disconnect = null,
            MissionApplication missionManager = null,
            CharacterManager characterManager = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _maps = maps;
            _clock = clock ?? (() => Environment.TickCount64);
            _updateCharacter = updateCharacter ?? ((client, update, value) =>
                Characters.UpdateCharacter(client, update, value));
            _disconnect = disconnect ?? (client => client.Close(false));
            _missionManager = missionManager;
            _characterManager = characterManager;
        }

        internal void InitDynamicObjects()
        {
            InitControlPoints();
            InitFootlockers();
            InitTeleporters();
            LogosManager.Instance.LogosInit();
            KraftwerksManager.Instance.KraftwerksInit();
        }

        internal void CloneTemplateMap(MapChannel template, MapChannel mapChannel)
        {
            if (template == null || mapChannel == null)
                return;

            mapChannel.ControlPoints.Clear();
            foreach (var entry in template.ControlPoints)
                AddClonedDynamicObject(mapChannel.ControlPoints, entry.Key, entry.Value, mapChannel);

            mapChannel.FootLockers.Clear();
            foreach (var entry in template.FootLockers)
                AddClonedDynamicObject(mapChannel.FootLockers, entry.Key, entry.Value, mapChannel);

            mapChannel.Teleporters.Clear();
            foreach (var entry in template.Teleporters)
                AddClonedDynamicObject(mapChannel.Teleporters, entry.Key, entry.Value, mapChannel);

            mapChannel.DynamicObjects.Clear();
            foreach (var dynamicObject in template.DynamicObjects)
                mapChannel.DynamicObjects.Add(CloneDynamicObject(dynamicObject, mapChannel));

            mapChannel.Kraftwerks.Clear();
            foreach (var entry in template.Kraftwerks)
                AddClonedDynamicObject(mapChannel.Kraftwerks, entry.Key, entry.Value, mapChannel);

            foreach (var trigger in template.MapCellInfo.Cells.Values
                         .SelectMany(cell => cell.MapTriggers)
                         .Distinct()
                         .ToArray())
                CellManager.Instance.AddToWorld(mapChannel, CloneTrigger(trigger, mapChannel));

            foreach (var link in template.MapCellInfo.Cells.Values
                         .SelectMany(cell => cell.MapLinks)
                         .Distinct()
                         .ToArray())
                CellManager.Instance.AddToWorld(mapChannel, CloneMapLink(link, mapChannel));

        }

        internal void ForceState(DynamicObject obj, UseObjectState state, int delta)
        {
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(state, delta));
        }

        internal void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            // Teleporting counts as being in the world for the packet gate - a dropship ride keeps
            // the manifestation and everything registered with it - but the rider is between maps,
            // standing where they left, with no cells. There is nothing there for them to use.
            if (client.State != ClientState.Ingame)
                return;

            // The id names an object, or it names nothing this can answer. GetObject is the
            // throwing indexer, so any item, creature or player id - or an object id that is no
            // longer registered - closed the connection of whoever sent it.
            if (!EntityManager.Instance.TryGetObject(packet.EntityId, out var obj))
            {
                Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} asked to use {packet.EntityId}, which is not an object.");
                return;
            }

            if (!obj.IsEnabled)
            {
                Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} asked to use {packet.EntityId}, which is not enabled right now.");
                return;
            }

            // Where the player is standing decides what they can reach. Object ids are handed out
            // in order and are the same for every client, so a client could walk the id space and
            // use every footlocker, station and control point on the map without leaving the spot
            // it was standing on - and collect every logos tablet on it, since the recovery asks
            // only that the player be in the object's TriggeredByPlayers list. An object on
            // another map left them in that list for good, which holds their connection open in
            // the server's memory long after they have gone.
            if (!MapInstanceScope.Contains(client.Player.MapChannel, obj))
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} asked to use object {packet.EntityId}, which is not on the current map instance. Ignored.");
                return;
            }

            var distance = Vector3.Distance(client.Player.Position, obj.Position);

            if (distance > MaxUseDistance)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} asked to use object {packet.EntityId} from {distance:F0} units away. Ignored.");
                return;
            }

            // Using an object is the only thing a use request may ask for. The queued action
            // carried the packet's own action id to ActorActionManager.PerformRecovery, which
            // performs whatever that id names, on the player, as soon as the windup has run - so
            // any footlocker or crafting station was a hundred milliseconds of free choice over
            // the whole action table. WeaponReload filled the clip and cleared a jam with no
            // reload asked for and none of its time served, and anything AbilityManager can
            // resolve landed as an ability: its recovery is the half that takes the skill, the
            // cost, the cooldown and the range as read, because the request half checked them.
            //
            // The arg id is left as it arrived. It only picks which of the four use-object
            // recoveries runs, and each of those acts on an object that holds this player in its
            // TriggeredByPlayers - the list this request adds them to - so one that does not
            // match the object it was sent to finds nothing to do. It also has to go back
            // unchanged: the client files its pending action under (actionId, actionArgId) and
            // does not recognise its own windup or recovery under any other arg.
            if (packet.ActionId != ActionId.UseObject)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} sent {packet.ActionId}/{packet.ActionArgId} to use object {packet.EntityId}; an object is used with {ActionId.UseObject}. Ignored.");
                return;
            }
            if (obj.MissionConversation != null)
            {
                (_missionManager ?? MissionApplication.Instance).ObjectConversations.Open(client, obj.EntityId);
                return;
            }

            if (obj.MissionLootSource != null || obj.LootDispenserEntityId != 0)
            {
                if (obj.LootDispenserEntityId == 0)
                    LootDispenserManager.Instance.AttachRewardLoot(client, client.Player.MapChannel, obj);
                if (obj.LootDispenserEntityId == 0 || !obj.IsEnabled)
                    return;
            }

            switch (obj.DynamicObjectType)
            {
                case DynamicObjectType.ControlPoint:
                    {
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 10000));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 10000));

                        obj.TriggeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Lockbox:
                    {
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 100));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 100));

                        obj.TriggeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Logos:
                    {
                        var windupTime = obj.WindupTime == 0
                            ? DefaultScenarioUseWindupMs
                            : obj.WindupTime;
                        var actionData = new ActionData(client.Player, packet.ActionId, packet.ActionArgId, windupTime);
                        actionData.SourceId = obj.EntityId;

                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, (int)windupTime));
                        client.Player.MapChannel.PerformRecovery.Add(actionData);

                        obj.TriggeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Kraftwerks:
                    KraftwerksManager.Instance.Use(client, obj, packet.ActionArgId);
                    break;
                case DynamicObjectType.DropshipPad:
                    // The hovering ship is a two-state switch to the client, so it offers a use;
                    // there is nothing to do with one - the pad works by walking into the beam.
                    break;
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

            // crafting stations
            KraftwerksManager.Instance.Worker(mapChannel);

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

            // dynamicObjects: logos shrines and dropship pads
            foreach (var dynamicObject in mapChannel.DynamicObjects)
            {
                // spawn object
                if (!dynamicObject.IsInWorld)
                {
                    dynamicObject.RespawnTime -= delta;

                    if (dynamicObject.RespawnTime <= 0)
                    {
                        // A shrine is built with no state and gets the usable default here; a
                        // pad's dropship is built hovering (TsState1) and keeps it.
                        if (dynamicObject.StateId == 0)
                        {
                            dynamicObject.StateId = UseObjectState.IdStateActive;
                            dynamicObject.WindupTime = 10000;
                        }

                        CellManager.Instance.AddToWorld(mapChannel, dynamicObject);
                        dynamicObject.IsInWorld = true;
                    }
                }
            }
        }

        internal void DynamicObjectProximityWorker(MapChannel mapChannel, DynamicObject obj, long delta)
        {
            switch (obj.DynamicObjectType)
            {
                // teleporters
                case DynamicObjectType.Waypoint:
                case DynamicObjectType.Wormhole:
                case DynamicObjectType.DropshipTeleporter:
                    {
                        // check for players that enter range
                        PlayerEnterWaypoint(obj);

                        // check for players that leave range
                        PlayerExitWaypoint(obj);

                        break;
                    }
                // Control point
                case DynamicObjectType.ControlPoint:
                default:
                    break;
            }
        }

        internal DynamicObject CreateScenarioDynamicObject(
            MapChannel mapChannel,
            EntityClasses entityClassId,
            Vector3 position,
            double rotation,
            string scenarioKey,
            bool enabled,
            uint? windupTime = null)
        {
            var dynamicObject = new DynamicObject
            {
                EntityClassId = entityClassId,
                Position = position,
                Rotation = rotation,
                MapContextId = mapChannel?.MapInfo?.MapContextId ?? 0,
                RuntimeMapChannel = mapChannel,
                DynamicObjectType = DynamicObjectType.Logos,
                StateId = UseObjectState.IdStateActive,
                IsEnabled = enabled,
                WindupTime = windupTime.GetValueOrDefault(DefaultScenarioUseWindupMs),
                ScenarioKey = scenarioKey
            };
            return dynamicObject;
        }

        internal void SetScenarioInteractionEnabled(
            MapChannel mapChannel,
            DynamicObject dynamicObject,
            bool enabled)
        {
            if (mapChannel == null || dynamicObject == null)
                return;

            dynamicObject.IsEnabled = enabled;
            if (dynamicObject.SceneRunId != null)
            {
                var classInfo = EntityClassManager.Instance.GetClassInfo(dynamicObject.EntityClassId)
                    ?? throw new GameplayRejectionException($"Missing scene object class {dynamicObject.EntityClassId}.");
                CellManager.Instance.CellCallMethod(mapChannel, dynamicObject,
                    new IsTargetablePacket(classInfo.TargetFlag || enabled));
            }
            if (dynamicObject.MissionConversation != null)
            {
                if (CellManager.TryGetCellCoordinates(dynamicObject.Position, out var x, out var z))
                    foreach (var recipient in CellManager.Instance.GetClientsInCells(mapChannel,
                        CellManager.Instance.CreateCellMatrix(mapChannel, x, z)))
                        recipient.CallMethod(dynamicObject.EntityId,
                            (_missionManager ?? MissionApplication.Instance).ObjectConversations.Status(recipient, dynamicObject));
                return;
            }
            CellManager.Instance.CellCallMethod(
                mapChannel,
                dynamicObject,
                new UsableInfoPacket(
                    dynamicObject.IsEnabled,
                    dynamicObject.StateId,
                    0,
                    dynamicObject.WindupTime,
                    dynamicObject.ActivateMission));

            if (enabled && dynamicObject.MissionLootSource != null &&
                CellManager.TryGetCellCoordinates(dynamicObject.Position, out var cellX, out var cellZ))
            {
                var cells = CellManager.Instance.CreateCellMatrix(mapChannel, cellX, cellZ);
                foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, cells))
                    PublishRewardLoot(client, dynamicObject);
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
                new IsTargetablePacket(classInfo.TargetFlag ||
                    dynamicObject.SceneRunId != null && dynamicObject.IsEnabled),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Rotation)
            };
            if (dynamicObject.MissionConversation is { } conversation)
            {
                entityData.Add(new NPCInfoPacket(conversation.NpcPackageId));
                entityData.Add((_missionManager ?? MissionApplication.Instance).ObjectConversations.Status(client, dynamicObject));
            }
            else
                entityData.Add(new UsableInfoPacket(dynamicObject.IsEnabled, dynamicObject.StateId, 0,
                    dynamicObject.WindupTime, dynamicObject.ActivateMission));

            // Only for an object that actually has a lock. An unlocked usable is the default the
            // client already assumes, and sending a lock of zeroes would tell it the same thing
            // at the cost of a packet per object per client.
            if (dynamicObject.Lock != null)
                entityData.Add(new LockInfoPacket(dynamicObject.Lock));

            if (dynamicObject.DynamicObjectType == DynamicObjectType.PracticeDummy)
            {
                entityData.Add(new TargetCategoryPacket(TargetCategory.Object));
                entityData.Add(new DamageInfoPacket(
                    true, false, PracticeTargetManager.HitPoints, PracticeTargetManager.HitPoints));
            }

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
            PublishRewardLoot(client, dynamicObject);
        }

        private static void PublishRewardLoot(Client client, DynamicObject dynamicObject)
        {
            var source = dynamicObject.MissionLootSource;
            if (source != null &&
                client.Player?.Missions.TryGetValue(source.MissionId, out var mission) == true &&
                mission.Objectives.TryGetValue(source.ObjectiveId, out var objective) &&
                objective.State is MissionObjectiveState.Incomplete or MissionObjectiveState.Completed &&
                MapInstanceScope.Contains(client.Player?.MapChannel, dynamicObject))
            {
                var lootManager = LootDispenserManager.Instance;
                var map = client.Player.MapChannel;
                if (dynamicObject.LootDispenserEntityId == 0)
                    lootManager.AttachRewardLoot(client, map, dynamicObject);
                else if (map.LootDispensers.TryGetValue(dynamicObject.LootDispenserEntityId, out var loot) &&
                         ReferenceEquals(loot.OwnerClient, client))
                {
                    lootManager.AttachInfo(client, loot);
                    lootManager.LootInfo(client, loot);
                    lootManager.CanLootItems(client, loot);
                }
            }
        }

        internal void CellDiscardDynamicObjectToClients(ulong entityId, List<Client> clients)
        {
            if (entityId == 0)
                return;

            foreach (var client in clients)
                EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Object);
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
                EntityClassId = (EntityClasses)3814,
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

                foreach (var client in controlpoint.TriggeredByPlayers)
                    if (client.Player == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            controlpoint.TriggeredByPlayers.Remove(client);
                            break;
                        }

                        // As with a logos: a capture belongs to whoever is still standing at the
                        // point when its ten seconds are up.
                        if (!IsInUseRange(action.Actor, controlpoint))
                        {
                            Logger.WriteLog(LogType.Security,
                                $"{client.Player.FamilyName} was no longer at control point {controlpoint.EntityId} when the use finished; not captured.");
                            controlpoint.TriggeredByPlayers.Remove(client);
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        controlpoint.TriggeredByPlayers.Remove(client);
                        controlpoint.Faction = controlpoint.Faction == Factions.AFS ? Factions.Bane : Factions.AFS;
                        controlpoint.StateId = controlpoint.StateId == UseObjectState.CpointStateFactionAOwned ? UseObjectState.CpointStateFactionBOwned : UseObjectState.CpointStateFactionAOwned;

                        CellManager.Instance.CellCallMethod(
                            mapChannel,
                            controlpoint,
                            new ForceStatePacket(controlpoint.StateId, 100));
                        CellManager.Instance.CellCallMethod(
                            mapChannel,
                            controlpoint,
                            new UsableInfoPacket(
                                true,
                                controlpoint.StateId,
                                0,
                                10000,
                                0));
                        (_missionManager ?? MissionApplication.Instance).RecordProgress(
                            client,
                            MissionProgressEvent.Interaction(
                                (uint)controlpoint.EntityClassId));
                        break;
                    }
            }
        }

        #endregion

        #region Dropship
        /// <summary>
        /// Ticks the dropships owned by one map through their phases. A spawner lands, drops its
        /// creatures and leaves. A teleporter is either the departure or arrival end of a
        /// player's flight.
        /// </summary>
        public void DropshipsWorker(MapChannel mapChannel, long timePassed)
        {
            foreach (var entry in Dropships.ToArray())
            {
                var dropship = entry.Value;
                if (!ReferenceEquals(dropship.RuntimeMapChannel, mapChannel))
                    continue;

                if (dropship.DropshipType != DropshipType.Spawner && dropship.DropshipType != DropshipType.Teleporter)
                {
                    Logger.WriteLog(LogType.Debug, $"error dropshiptype {dropship.DropshipType}");
                    Dropships.Remove(entry.Key);
                    continue;
                }

                // A teleporter dropship whose passenger has gone (a dropped connection) has
                // nothing left to do; take it out of the world rather than fly it into a null.
                if (dropship.DropshipType == DropshipType.Teleporter && (dropship.Client?.Player == null || dropship.Client.Player.Disconected))
                {
                    var lostMap = dropship.RuntimeMapChannel ?? Maps.FindByContextId(dropship.MapContextId);
                    if (lostMap != null)
                        CellManager.Instance.RemoveFromWorld(lostMap, dropship);

                    Dropships.Remove(entry.Key);
                    continue;
                }

                dropship.PhaseTimeleft -= timePassed;

                if (dropship.PhaseTimeleft > 0)
                    continue;

                if (dropship.Phase == 0 || dropship.Phase == 1 || dropship.Phase == 4)
                    CellManager.Instance.CellCallMethod(mapChannel, dropship, new ForceStatePacket(dropship.StateId, 0));

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

                        if (dropship.DropshipType == DropshipType.Teleporter && dropship.Role == DropshipRole.Departure)
                        {
                            // Aboard: everyone sees the player fade, the player sees the teleport begin.
                            CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new PreTeleportPacket(TeleportType.Default));
                            dropship.Client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
                        }

                        if (dropship.DropshipType == DropshipType.Spawner)
                        {
                            // create list of creatures to spawn
                            var creatureList = SpawnPoolManager.Instance.CreateListOfCreatures(dropship.SpawnPool);

                            // spawn creatures
                            try
                            {
                                SpawnPoolManager.Instance.SpawnCreatures(dropship.SpawnPool, creatureList);
                            }
                            finally
                            {
                                SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(dropship.SpawnPool, creatureList.Count);
                            }
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

                        if (dropship.DropshipType == DropshipType.Teleporter && dropship.Role == DropshipRole.Arrival)
                            dropship.Client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                        break;
                    case 5:
                        if (dropship.DropshipType == DropshipType.Teleporter)
                        {
                            if (dropship.Role == DropshipRole.Departure)
                                DepartDropship(dropship.Client, dropship);
                            else
                            {
                                if (dropship.Client.PendingTransfer == null &&
                                    dropship.Client.Player.MapContextId == dropship.MapContextId)
                                {
                                    dropship.Client.State = ClientState.Ingame;
                                    Maps.ResumeMissionScenes(dropship.Client);
                                    ManifestationManager.Instance.ResetInactivity(dropship.Client);
                                }
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
            // The client defaults usable props without usabledata rows to argument 1.
            // Scenario props are DynamicObjects, not Logos, so they recover here.
            // Reward props open their dispenser; other props emit InteractionUsed.
            foreach (var obj in mapChannel.DynamicObjects)
            {
                if (action.SourceId != obj.EntityId)
                    continue;

                foreach (var client in obj.TriggeredByPlayers)
                {
                    if (client.Player != action.Actor)
                        continue;

                    obj.TriggeredByPlayers.Remove(client);

                    if (action.IsInrerrupted)
                        return;

                    if (!IsInUseRange(action.Actor, obj))
                    {
                        Logger.WriteLog(LogType.Security,
                            $"{client.Player.FamilyName} was no longer at object {obj.EntityId} when the use finished; nothing given.");
                        return;
                    }

                    // Usable supplies mouse targeting; its completion opens loot, never claims it.
                    if (obj.MissionLootSource != null || obj.LootDispenserEntityId != 0)
                    {
                        if (obj.LootDispenserEntityId != 0)
                            LootDispenserManager.Instance.RequestCorpseLooting(
                                client,
                                new Packets.LootDispenser.Client.RequestCorpseLootingPacket
                                {
                                    EntityId = obj.LootDispenserEntityId
                                });
                        return;
                    }

                    CellManager.Instance.CellCallMethod(
                        obj,
                        new UsableInfoPacket(
                            true,
                            obj.StateId,
                            0,
                            obj.WindupTime == 0 ? DefaultScenarioUseWindupMs : obj.WindupTime,
                            0));

                    (_missionManager ?? MissionApplication.Instance).RecordProgress(
                        client,
                        MissionProgressEvent.Interaction((uint)obj.EntityClassId));
                    return;
                }
            }

            Logger.WriteLog(LogType.Debug, $"ToDo: FootlockerRecovery (real Footlocker loot generation), ActionId = {action.ActionId} ActionArgId = {action.ActionArgId}");
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
                foreach (var client in obj.TriggeredByPlayers)
                    if (client.Player == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            obj.TriggeredByPlayers.Remove(client);
                            //CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                            break;
                        }

                        // Still at it when the ten seconds are up, not only when they started.
                        // The client interrupts a use the moment the player moves (useobject.py
                        // sets moveInterrupts), so the only client this refuses is one that did
                        // not - and a tablet is a permanent thing to be given for a use that was
                        // walked away from.
                        if (!IsInUseRange(action.Actor, obj))
                        {
                            Logger.WriteLog(LogType.Security,
                                $"{client.Player.FamilyName} was no longer at logos object {obj.EntityId} when the use finished; nothing given.");
                            obj.TriggeredByPlayers.Remove(client);
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        obj.TriggeredByPlayers.Remove(client);
                        CellManager.Instance.CellCallMethod(
                            obj,
                            new UsableInfoPacket(
                                true,
                                obj.StateId,
                                0,
                                obj.WindupTime == 0 ? DefaultScenarioUseWindupMs : obj.WindupTime,
                                0));

                        var logosId = 0u;
                        foreach (var entry in mapChannel.DynamicObjects)
                        {
                            if (entry is not Logos logos ||
                                action.SourceId != logos.EntityId)
                                continue;

                            logosId = logos.Id;
                            break;
                        }

                        var haveLogos = logosId != 0 &&
                                        client.Player.Logos.Any(logos => logos == logosId);
                        if (logosId != 0 && !haveLogos)
                            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Logos, logosId);
                        (_missionManager ?? MissionApplication.Instance).RecordProgress(
                            client,
                            MissionProgressEvent.Interaction((uint)obj.EntityClassId));

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
                if (mapChannel == null)
                    continue;

                var newTeleporter = new DynamicObject
                {
                    Position = teleporter.Position,
                    Rotation = teleporter.Rotation,
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = (EntityClasses)teleporter.ClassId,
                    Comment = teleporter.Description,
                    ObjectData = new WaypointInfo(teleporter.Id, false, (WaypointType)teleporter.Type)
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
                    case 4:
                        // A dropship pad is two things. The trigger is the 5 m circle on the pad
                        // that opens the travel window and gains the pad for whoever walks into
                        // it. The hovering dropship with its transporter beam is what the client
                        // shows there - UsableTwoStateHumDropshipBeam in TsState1 is the ship
                        // hovering with the beam on (TsState0 is an empty pad), which is how the
                        // help text describes gaining one: "walking across the pad when a Dropship
                        // is hovering with its transporter beam activated". The client's map has
                        // the landing pad geometry itself; the ship was always the server's.
                        CellManager.Instance.AddToWorld(mapChannel, new MapTrigger(teleporter.Id, teleporter.Description, teleporter.Position, teleporter.Rotation, teleporter.MapContextId));

                        if (Characters.StartingExperience.IsExitPad(teleporter.MapContextId, teleporter.Id))
                            break;
                        mapChannel.DynamicObjects.Add(new DynamicObject
                        {
                            EntityId = EntityManager.Instance.GetEntityId,
                            EntityClassId = EntityClasses.UsableTwoStateHumDropshipBeam,
                            DynamicObjectType = DynamicObjectType.DropshipPad,
                            Position = teleporter.Position,
                            Rotation = teleporter.Rotation,
                            MapContextId = teleporter.MapContextId,
                            Faction = Factions.AFS,
                            StateId = UseObjectState.TsState1,
                            Comment = teleporter.Description
                        });
                        break;
                    case 5:
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"InitTeleporters: unsuported teleporter type {teleporter.Type}");

                        MapErrorManager.Instance.Record(teleporter.MapContextId,
                            $"Teleporter {teleporter.Id} ({teleporter.Description}) is type {teleporter.Type}, which nothing handles.");

                        break;
                }

                mapChannel.Teleporters.Add(teleporter.Id, newTeleporter);
                Teleporters.Add(teleporter.Id, newTeleporter);
            }
        }

        internal void CheckPlayerWaypoint(Client client, WaypointInfo objectData)
        {
            if (Characters.StartingExperience.IsExitWaypoint(objectData.WaypointId))
                return;
            lock (client.SyncRoot)
            {
                foreach (var waypoint in client.Player.GainedWaypoints)
                    if (waypoint.WaypointId == objectData.WaypointId)
                        return;

                var newWaypoint = new CharacterTeleporterEntry(
                    client.Player.Id,
                    objectData.WaypointId,
                    (byte)objectData.WaypointType);
                _updateCharacter(client, CharacterUpdate.Teleporter, newWaypoint);
                ConvergeWaypointGrant(
                    client,
                    newWaypoint,
                    recordProgress: true,
                    missionManager: _missionManager);
            }
        }

        internal static bool ConvergeWaypointGrant(
            Client client,
            CharacterTeleporterEntry waypoint,
            bool recordProgress = false,
            MissionApplication missionManager = null)
        {
            if (client?.Player == null || waypoint == null)
                return false;

            lock (client.SyncRoot)
            {
                if (client.Player.GainedWaypoints.Any(known => known.WaypointId == waypoint.WaypointId))
                    return false;

                client.Player.GainedWaypoints.Add(waypoint);
                PublishWaypointGrant(client, waypoint, recordProgress, missionManager);
                return true;
            }
        }

        private static void PublishWaypointGrant(
            Client client,
            CharacterTeleporterEntry waypoint,
            bool recordProgress,
            MissionApplication missionManager)
        {
            var waypointType = (WaypointType)waypoint.WaypointType;
            client.CallMethod(
                client.Player.EntityId,
                new WaypointGainedPacket(
                    waypoint.WaypointId,
                    waypointType));
            MapMarkerManager.Instance.WaypointDiscovered(
                client,
                waypoint.WaypointId);
            if (recordProgress)
            {
                (missionManager ?? MissionApplication.Instance).RecordProgress(
                    client,
                    MissionProgressEvent.Waypoint(waypoint.WaypointId));
            }
        }

        internal Dictionary<uint, MapWaypointInfoList> CreateListOfWaypoints(Client client, WaypointType waypointType)
        {
            var listOfWaypoints = new Dictionary<uint, MapWaypointInfoList>();
            var listOfMapInstances = new List<MapInstanceInfo>();
            var waypointInfo = new List<WaypointInfo>();
            var mapChannel = client.Player.MapChannel;

            // create waypoint list for player
            foreach (var waypoint in client.Player.GainedWaypoints)
            {
                if ((WaypointType)waypoint.WaypointType != waypointType)
                    continue;

                if (!Teleporters.TryGetValue(waypoint.WaypointId, out var teleporter))
                {
                    Logger.WriteLog(LogType.Error, $"Discovered waypoint {waypoint.WaypointId} has no world definition.");
                    continue;
                }
                var teleporterData = teleporter.ObjectData as WaypointInfo;
                if (teleporterData == null || teleporterData.Contested)
                    continue;

                if (teleporter.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                if (teleporterData.WaypointType != waypointType)
                    continue;

                if (waypoint.WaypointId == teleporterData.WaypointId)
                {
                    waypointInfo.Add(new WaypointInfo(teleporterData.WaypointId, teleporterData.Contested, teleporterData.WaypointType)
                    {
                        Position = teleporter.Position
                    });

                }
            }
            listOfMapInstances.Add(new MapInstanceInfo(mapChannel.InstanceId, mapChannel.MapInfo.MapContextId, MapInstanceStatus.Low));

            listOfWaypoints.Add(mapChannel.MapInfo.MapContextId, new MapWaypointInfoList(mapChannel.MapInfo.MapContextId, listOfMapInstances, waypointInfo));

            return listOfWaypoints;
        }

        internal void SelectWaypoint(Client client, SelectWaypointPacket packet)
        {
            lock (client.SyncRoot)
            {
                if (client.PendingTransfer != null)
                {
                    Logger.WriteLog(LogType.Network, "Ignored a duplicate waypoint selection during transfer.");
                    return;
                }
                if (client.State != ClientState.Ingame || client.Player?.MapChannel == null || client.Player.Id == 0 ||
                    client.Player.Disconected || client.Player.RemoveFromMap || client.Player.LogoutActive ||
                    !CellManager.Instance.IsInWorld(client) ||
                    !Teleporters.TryGetValue(packet.WaypointId, out var teleporter) ||
                    teleporter.ObjectData is not WaypointInfo info)
                {
                    RejectTravel(client, "Invalid waypoint or player state.");
                    return;
                }

                var destinationMap = teleporter.RuntimeMapChannel ??
                    (packet.MapInstanceId > 1
                        ? Maps.FindByContextAndInstance(teleporter.MapContextId, packet.MapInstanceId)
                        : Maps.FindByContextId(teleporter.MapContextId));
                if (destinationMap == null)
                {
                    RejectTravel(client, "Invalid waypoint or player state.");
                    return;
                }

                var origin = client.Player.MapChannel;
                var isDropship = info.WaypointType == WaypointType.Dropship;
                var isStartingExperienceExit =
                    Characters.StartingExperience.IsExitPad(client.Player.MapContextId, packet.WaypointId);
                if (Characters.StartingExperience.IsExitWaypoint(packet.WaypointId) && !isStartingExperienceExit)
                {
                    RejectTravel(client, "One-way extraction is not a travel destination.");
                    return;
                }
                if ((packet.MapInstanceId != 0 && packet.MapInstanceId != destinationMap.InstanceId) ||
                    info.Contested ||
                    (!isStartingExperienceExit &&
                        !client.Player.GainedWaypoints.Any(waypoint => waypoint.WaypointId == packet.WaypointId &&
                            waypoint.WaypointType == (byte)info.WaypointType)) ||
                    (!isDropship && info.WaypointType != WaypointType.Waypoint && info.WaypointType != WaypointType.LocalTeleporter) ||
                    (!isDropship && destinationMap != origin))
                {
                    RejectTravel(client, "Waypoint is not discovered, available, or in the selected instance.");
                    return;
                }

                if (isDropship && !isStartingExperienceExit)
                {
                    if (Dropships.Values.Any(ship =>
                            ship.DropshipType == DropshipType.Teleporter &&
                            ship.Client == client))
                    {
                        RejectTravel(client, "A dropship flight is already active.");
                        return;
                    }
                    if (destinationMap.MapInfo.Planet != origin.MapInfo.Planet)
                    {
                        RejectTravel(client, "Dropship destinations must be on the current planet.");
                        return;
                    }
                }

                var nearbySource = origin.Teleporters.Values.Any(source =>
                    source.ObjectData is WaypointInfo sourceInfo && sourceInfo.WaypointType == info.WaypointType &&
                    (isStartingExperienceExit || !Characters.StartingExperience.IsExitWaypoint(sourceInfo.WaypointId)) &&
                    MapInstanceScope.Contains(origin, source) &&
                    (isDropship ? client.Player.IsNear5m(source) : client.Player.IsNear2m(source)));
                var destination = isDropship ? teleporter.Position : teleporter.Position + new Vector3(0, 1, 0);
                if (!nearbySource || !CellManager.TryGetCellCoordinates(destination, out _, out _) ||
                    !double.IsFinite(teleporter.Rotation) || !float.IsFinite((float)teleporter.Rotation))
                {
                    RejectTravel(client, "No nearby departure station or invalid destination position.");
                    return;
                }

                if (isStartingExperienceExit)
                {
                    if (!Characters.StartingExperience.TryDepart(client, this))
                        RejectTravel(client, "Starting-experience departure is not available.");
                    return;
                }
                if (isDropship)
                {
                    if (!TryBeginDropshipTravel(client, destinationMap, destination, teleporter.Rotation))
                        RejectTravel(client, "Dropship departure is not available.");
                    return;
                }

                var timeout = client.Server?.Config.GameConfig.TransferTimeoutSeconds ??
                    Config.GameConfig.DefaultTransferTimeoutSeconds;
                if (timeout <= 0)
                {
                    Logger.WriteLog(LogType.Error, "TransferTimeoutSeconds must be positive.");
                    RejectTravel(client, "Travel timeout configuration is invalid.");
                    return;
                }

                var transfer = new PlayerTransfer
                {
                    OriginMap = origin,
                    OriginPosition = client.Player.Position,
                    OriginRotation = client.Player.Rotation,
                    DestinationMap = destinationMap,
                    DestinationPosition = destination,
                    DestinationRotation = teleporter.Rotation,
                    Deadline = checked(_clock() + timeout * 1000L),
                    IsDropship = false
                };
                client.PendingTransfer = transfer;
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());

                client.CellCallMethod(client, client.Player.EntityId, new PreTeleportPacket(TeleportType.Default));
                client.State = ClientState.Teleporting;
                client.SetWorldPosition(destination, teleporter.Rotation);
                CellManager.Instance.UpdateVisibility(client);
                client.CallMethod(client.Player.EntityId,
                    new TeleportPacket(destination, teleporter.Rotation, TeleportType.Default, 5));
                client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
                client.CellMoveObject(client, new MoveObjectMessage(client.Player.EntityId, client.Movement), false);
            }
        }

        internal bool CanBeginDropshipTravel(Client client, MapChannel destination, Vector3 position, double rotation)
        {
            var timeout = client?.Server?.Config.GameConfig.TransferTimeoutSeconds ??
                Config.GameConfig.DefaultTransferTimeoutSeconds;
            return client?.Player?.MapChannel != null && client.State == ClientState.Ingame &&
                client.Player.State != CharacterState.Dead && client.PendingTransfer == null &&
                CellManager.Instance.IsInWorld(client) && destination != null &&
                CellManager.TryGetCellCoordinates(position, out _, out _) && double.IsFinite(rotation) &&
                timeout > 0 && !Dropships.Values.Any(ship => ship.Client == client && ship.DropshipType == DropshipType.Teleporter);
        }

        internal bool IsOneWayExit(uint mapContextId, uint waypointId) =>
            Characters.StartingExperience.IsExitPad(mapContextId, waypointId);

        internal void BeginOneWayDeparture(Client client)
        {
            lock (client.SyncRoot)
                if (!Characters.StartingExperience.TryDepart(client, this))
                    RejectTravel(client, "Starting-experience departure is not available.");
        }

        internal bool IsStationAvailable(Client client, uint mapContextId, uint waypointId) =>
            !IsOneWayExit(mapContextId, waypointId) ||
            Characters.StartingExperience.IsDepartureReady(client);

        internal bool TryBeginDropshipTravel(Client client, MapChannel destination, Vector3 position, double rotation,
            Vector3? departurePosition = null, double? departureRotation = null,
            uint releaseOwnedPrivateInstancesForCharacterId = 0)
        {
            lock (client.SyncRoot)
            {
                if (!CanBeginDropshipTravel(client, destination, position, rotation) ||
                    departurePosition.HasValue && !CellManager.TryGetCellCoordinates(departurePosition.Value, out _, out _) ||
                    departureRotation.HasValue && !double.IsFinite(departureRotation.Value))
                    return false;
                var origin = client.Player.MapChannel;
                var timeout = client.Server?.Config.GameConfig.TransferTimeoutSeconds ??
                    Config.GameConfig.DefaultTransferTimeoutSeconds;
                var transfer = new PlayerTransfer
                {
                    OriginMap = origin, OriginPosition = client.Player.Position, OriginRotation = client.Player.Rotation,
                    DestinationMap = destination, DestinationPosition = position, DestinationRotation = rotation,
                    Deadline = checked(_clock() + timeout * 1000L), IsDropship = true,
                    ReleaseOwnedPrivateInstancesForCharacterId = releaseOwnedPrivateInstancesForCharacterId
                };
                var ship = new Dropship(Factions.AFS, DropshipType.Teleporter, client, DropshipRole.Departure,
                    position, destination.MapInfo.MapContextId) { DestinationRotation = rotation };
                if (departurePosition.HasValue)
                    ship.Position = departurePosition.Value;
                if (departureRotation.HasValue)
                    ship.Rotation = departureRotation.Value;
                transfer.DropshipId = ship.EntityId;
                client.PendingTransfer = transfer;
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                CellManager.Instance.AddToWorld(origin, ship);
                Dropships.Add(ship.EntityId, ship);
                if (destination != origin)
                    client.LoadingMap = destination.MapInfo.MapContextId;
                return true;
            }
        }

        /// <summary>
        /// Whether the player currently has a waypoint window open on the server's side: the
        /// proximity workers add a client to a teleporter's TriggeredByPlayers or a dropship
        /// pad's TriggeredBy while it is within range, and take it out again when it leaves.
        /// </summary>
        private static bool IsAtWaypoint(Client client)
        {
            var mapChannel = client.Player.MapChannel;

            if (mapChannel == null)
                return false;

            foreach (var teleporter in mapChannel.Teleporters.Values)
                if (teleporter.TriggeredByPlayers.Contains(client))
                    return true;

            // A pad's trigger lives in the cell its centre is in, which need not be the cell the
            // player is in when they are within its 5 m of it.
            return PadUnder(client) != null;
        }

        internal void TeleportAcknowledge(Client client)
        {
            lock (client.SyncRoot)
            {
                if (client.State != ClientState.Teleporting || client.PendingTransfer == null ||
                    client.PendingTransfer.IsDropship)
                {
                    Logger.WriteLog(LogType.Network, "Ignored an unexpected teleport acknowledgement.");
                    return;
                }
                if (CheckTransferTimeout(client) || !PersistTransfer(client))
                    return;

                client.PendingTransfer = null;
                client.State = ClientState.Ingame;
                Maps.ResumeMissionScenes(client);
                client.CallMethod(client.Player.EntityId, new TeleportArrivalPacket());
                client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
            }
        }

        private static void RejectTravel(Client client, string reason)
        {
            Logger.WriteLog(LogType.Network, $"Rejected travel: {reason}");
            if (client.Player != null && client.State != ClientState.Disconnected)
                client.CallMethod(client.Player.EntityId, new TeleportFailedPacket());
        }

        internal bool CheckTransferTimeout(Client client)
        {
            lock (client.SyncRoot)
            {
                var transfer = client.PendingTransfer;
                if (transfer == null || _clock() < transfer.Deadline)
                    return false;

                Logger.WriteLog(LogType.Network,
                    $"Transfer timed out for entity {client.Player.EntityId}; restoring its origin.");
                if (transfer.HasDeparted && CellManager.Instance.IsInWorld(client))
                    CellManager.Instance.RemoveFromWorld(client);
                client.RestoreTransferOrigin();
                CleanupClientDropships(client);
                _disconnect(client);
                return true;
            }
        }

        private void DepartDropship(Client client, Dropship dropship)
        {
            lock (client.SyncRoot)
            {
                var transfer = client.PendingTransfer;
                if (transfer == null || !transfer.IsDropship || transfer.HasDeparted ||
                    transfer.DropshipId != dropship.EntityId || CheckTransferTimeout(client))
                    return;

                if (ReferenceEquals(transfer.OriginMap, transfer.DestinationMap))
                {
                    transfer.HasDeparted = true;
                    client.Player.Target = 0;
                    client.SetWorldPosition(
                        transfer.DestinationPosition,
                        transfer.DestinationRotation);
                    if (!PersistTransfer(client))
                        return;

                    client.PendingTransfer = null;
                    CellManager.Instance.UpdateVisibility(client);
                    client.CallMethod(
                        client.Player.EntityId,
                        new TeleportPacket(
                            transfer.DestinationPosition,
                            transfer.DestinationRotation,
                            TeleportType.Default,
                            5));
                    client.CellMoveObject(
                        client,
                        new MoveObjectMessage(
                            client.Player.EntityId,
                            client.Movement),
                        false);

                    var arrival = new Dropship(
                        Factions.AFS,
                        DropshipType.Teleporter,
                        client,
                        DropshipRole.Arrival);
                    CellManager.Instance.AddToWorld(transfer.DestinationMap, arrival);
                    Dropships.Add(arrival.EntityId, arrival);
                    return;
                }

                CommunicatorManager.Instance.LeaveMapChannels(client);
                LootDispenserManager.Instance.RemoveForOwner(transfer.OriginMap, client);
                CellManager.Instance.RemoveFromWorld(client);
                transfer.OriginMap.ClientList.RemoveAll(member => member == client);
                Maps.DetachMissionScenes(client, transfer.OriginMap);
                transfer.HasDeparted = true;
                client.Player.MapChannel = transfer.DestinationMap;
                client.Player.MapContextId = transfer.DestinationMap.MapInfo.MapContextId;
                client.SetWorldPosition(transfer.DestinationPosition, transfer.DestinationRotation);
                client.LoadingMap = transfer.DestinationMap.MapInfo.MapContextId;
                client.State = ClientState.Teleporting;
                client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
                client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket(
                    transfer.DestinationMap.MapInfo.MapContextId, transfer.DestinationMap.InstanceId,
                    transfer.DestinationMap.MapInfo.MapVersion, transfer.DestinationPosition,
                    (float)transfer.DestinationRotation));
                client.AwaitingMapLoaded = true;
            }
        }

        internal bool IsExpectedMapLoad(Client client)
        {
            var transfer = client.PendingTransfer;
            return client.State == ClientState.Teleporting &&
                transfer?.HasDeparted == true && client.LoadingMap == transfer.DestinationMap.MapInfo.MapContextId &&
                client.Player.MapChannel == transfer.DestinationMap;
        }

        internal bool CompleteMapLoadTransfer(Client client)
        {
            lock (client.SyncRoot)
            {
                if (!IsExpectedMapLoad(client))
                    throw new InvalidOperationException("No matching map transfer to complete.");
                if (!PersistTransfer(client))
                    return false;
                var releaseOwner = client.PendingTransfer.ReleaseOwnedPrivateInstancesForCharacterId;
                client.PendingTransfer = null;
                if (releaseOwner != 0)
                    Maps.ReleaseOwnedPrivateInstances(releaseOwner);
                return true;
            }
        }

        private bool PersistTransfer(Client client)
        {
            try
            {
                _updateCharacter(client, CharacterUpdate.Position, null);
                return true;
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error, $"Unable to persist player transfer: {error.Message}");
                client.RestoreTransferOrigin();
                CleanupClientDropships(client);
                _disconnect(client);
                return false;
            }
        }

        internal void CleanupClientDropships(Client client)
        {
            foreach (var dropship in Dropships.Values.Where(ship => ship.Client == client).ToArray())
            {
                var map = dropship.RuntimeMapChannel ?? Maps.FindByContextId(dropship.MapContextId);
                if (map != null)
                    CellManager.Instance.RemoveFromWorld(map, dropship);
                Dropships.Remove(dropship.EntityId);
            }
        }

        internal void CleanupMapDropships(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            foreach (var dropship in Dropships.Values
                         .Where(ship => ReferenceEquals(ship.RuntimeMapChannel, mapChannel))
                         .ToArray())
            {
                CellManager.Instance.RemoveFromWorld(mapChannel, dropship);
                Dropships.Remove(dropship.EntityId);
            }
        }

        internal void PlayerEnterWaypoint(DynamicObject obj)
        {
            var mapChannel = obj.RuntimeMapChannel;
            if (mapChannel == null)
                return;
            if (!CellManager.TryGetCellCoordinates(obj.Position, out var x, out var z))
            {
                Logger.WriteLog(LogType.Error, $"Invalid waypoint position for {obj.EntityId}.");
                return;
            }
            var cells = CellManager.Instance.CreateCellMatrix(mapChannel, x, z);

            foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, cells))
            {
                if (client.State != ClientState.Ingame || client.PendingTransfer != null)
                    continue;
                // check if player is near waypoint
                if (!client.Player.IsNear2m(obj))
                {
                    continue;
                }

                // check if already added
                if (obj.TriggeredByPlayers.Any(p => p == client))
                {
                    continue;
                }

                // if not add him and send enter packet
                obj.TriggeredByPlayers.Add(client);

                var objectData = (WaypointInfo)obj.ObjectData;

                CheckPlayerWaypoint(client, objectData);

                var waypointInfoList = CreateListOfWaypoints(client, objectData.WaypointType);

                client.CallMethod(SysEntity.ClientMethodId,
                    new EnteredWaypointPacket(mapChannel.InstanceId, obj.MapContextId,
                        waypointInfoList, objectData.WaypointType, objectData.WaypointId));

                // check if we already added him to the waypoint
            }
        }

        internal void PlayerExitWaypoint(DynamicObject obj)
        {
            for (var i = obj.TriggeredByPlayers.Count - 1; i >= 0; i--)
            {
                var client = obj.TriggeredByPlayers[i];

                if (client.State != ClientState.Ingame ||
                    client.Player?.MapChannel != obj.RuntimeMapChannel ||
                    !client.Player.IsNear2m(obj))
                {
                    obj.TriggeredByPlayers.RemoveAt(i);

                    if (client.State != ClientState.Disconnected)
                        client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
            }
        }

        /// <summary>
        /// The dropship travel window for a player standing on a pad: every pad they have gained
        /// on the same planet, plus the one they are standing on, grouped by map and placed where
        /// it really is - the window plots each entry on that map's picture. Gaining is by
        /// walking into a pad's beam (see <see cref="MapTriggerManager.PlayerEnterTriggerRange"/>),
        /// as the client's help text says: "you must first travel to another map and gain access
        /// to a Dropship Transport there before you can use this method of travel". Every pad on
        /// both planets used to be offered, the first of each map at a made-up position.
        /// </summary>
        internal Dictionary<uint, MapWaypointInfoList> CreateListOfDropships(
            Client client,
            uint currentPadId = 0)
        {
            var dropships = new Dictionary<uint, MapWaypointInfoList>();
            var player = client?.Player;

            if (player?.MapChannel == null)
                return dropships;

            var planet = player.MapChannel.MapInfo.Planet;
            var gained = new HashSet<uint>(
                player.GainedWaypoints.Select(waypoint => waypoint.WaypointId));
            if (currentPadId != 0)
                gained.Add(currentPadId);

            foreach (var teleporter in Teleporters.Values)
            {
                if (teleporter.ObjectData is not WaypointInfo info ||
                    info.WaypointType != WaypointType.Dropship ||
                    info.Contested || Characters.StartingExperience.IsExitWaypoint(info.WaypointId))
                    continue;

                if (!gained.Contains(info.WaypointId))
                    continue;

                var map = Maps.FindByContextId(teleporter.MapContextId);

                if (map == null || map.MapInfo.Planet != planet)
                    continue;

                if (!dropships.TryGetValue(teleporter.MapContextId, out var list))
                {
                    list = new MapWaypointInfoList(
                        teleporter.MapContextId,
                        new List<MapInstanceInfo>
                        {
                            new MapInstanceInfo(
                                map.InstanceId,
                                teleporter.MapContextId,
                                MapInstanceStatus.Low)
                        },
                        new List<WaypointInfo>());

                    dropships.Add(teleporter.MapContextId, list);
                }

                list.Waypoints.Add(new WaypointInfo(info.WaypointId, info.Contested, teleporter.Position, info.WaypointType));
            }

            return dropships;
        }

        /// <summary>The dropship pad trigger the player is standing in, or null.</summary>
        internal static MapTrigger PadUnder(Client client)
        {
            var mapChannel = client?.Player?.MapChannel;

            if (mapChannel == null)
                return null;

            foreach (var cell in CellManager.CellsIn(mapChannel, client.Player.Cells))
                foreach (var trigger in cell.MapTriggers)
                    if (trigger.TriggeredBy.Contains(client))
                        return trigger;

            return null;
        }

        /// <summary>
        /// Gives the player every dropship pad in the world, the way walking into each beam
        /// would. For testing a network that spans maps with nothing else on them yet.
        /// </summary>
        internal int GainAllDropshipPads(Client client)
        {
            var given = 0;

            foreach (var teleporter in Teleporters.Values)
            {
                if (!(teleporter.ObjectData is WaypointInfo info) || info.WaypointType != WaypointType.Dropship ||
                    Characters.StartingExperience.IsExitWaypoint(info.WaypointId))
                    continue;

                if (client.Player.GainedWaypoints.Any(w => w.WaypointId == info.WaypointId))
                    continue;

                CheckPlayerWaypoint(client, info);
                given++;
            }

            return given;
        }
        #endregion

        private static void AddClonedDynamicObject(
            IDictionary<uint, DynamicObject> destination,
            uint key,
            DynamicObject source,
            MapChannel mapChannel)
        {
            if (source == null)
                return;

            destination[key] = CloneDynamicObject(source, mapChannel);
        }

        private static DynamicObject CloneDynamicObject(DynamicObject source, MapChannel mapChannel)
        {
            DynamicObject clone = source switch
            {
                Logos logos => new Logos(new LogosEntry
                {
                    Id = logos.Id,
                    Name = logos.Name,
                    MapContextId = mapChannel.MapInfo.MapContextId,
                    ClassId = (uint)logos.EntityClassId,
                    PosX = logos.Position.X,
                    PosY = logos.Position.Y,
                    PosZ = logos.Position.Z
                }),
                _ => new DynamicObject()
            };

            clone.EntityClassId = source.EntityClassId;
            clone.Position = source.Position;
            clone.Rotation = source.Rotation;
            clone.MapContextId = mapChannel.MapInfo.MapContextId;
            clone.Faction = source.Faction;
            clone.RespawnTime = source.RespawnTime;
            clone.DynamicObjectType = source.DynamicObjectType;
            clone.Comment = source.Comment;
            clone.ScenarioKey = source.ScenarioKey;
            clone.Lock = CloneLock(source.Lock);
            clone.IsEnabled = source.IsEnabled;
            clone.StateId = source.StateId;
            clone.WindupTime = source.WindupTime;
            clone.ActivateMission = source.ActivateMission;
            clone.ObjectData = CloneObjectData(source.ObjectData);

            if (source.IsInWorld)
            {
                CellManager.Instance.AddToWorld(mapChannel, clone);
                clone.IsInWorld = true;
            }

            return clone;
        }

        private static object CloneObjectData(object objectData)
        {
            return objectData switch
            {
                null => null,
                ControlPointStatus status => new ControlPointStatus(
                    status.ControlPointId,
                    status.OwnerId,
                    status.StateId,
                    status.EndTime),
                WaypointInfo waypoint => new WaypointInfo(
                    waypoint.WaypointId,
                    waypoint.Contested,
                    waypoint.Position,
                    waypoint.WaypointType),
                _ => objectData
            };
        }

        private static UsableLock CloneLock(UsableLock source)
        {
            if (source == null)
                return null;

            var clone = new UsableLock
            {
                Unlocked = source.Unlocked,
                LockStateId = source.LockStateId,
                UnlockedStateId = source.UnlockedStateId,
                MissionId = source.MissionId,
                KeyItemTemplateId = source.KeyItemTemplateId,
                CipherLevel = source.CipherLevel,
                CipherAttemptsLeft = source.CipherAttemptsLeft
            };
            clone.LogosIds.AddRange(source.LogosIds);
            clone.PlayerFlagReqs.AddRange(source.PlayerFlagReqs);
            clone.PlayerFlagExs.AddRange(source.PlayerFlagExs);
            return clone;
        }

        private static MapTrigger CloneTrigger(MapTrigger source, MapChannel mapChannel)
        {
            return new MapTrigger(
                source.TriggerId,
                source.TriggerName,
                source.Position,
                source.Rotation,
                mapChannel.MapInfo.MapContextId);
        }

        private static MapLink CloneMapLink(MapLink source, MapChannel mapChannel)
        {
            return new MapLink
            {
                Id = source.Id,
                MapContextId = mapChannel.MapInfo.MapContextId,
                Position = source.Position,
                Radius = source.Radius,
                DestMapContextId = source.DestMapContextId,
                DestPosition = source.DestPosition,
                DestRotation = source.DestRotation,
                Kind = source.Kind,
                Enabled = source.Enabled,
                Comment = source.Comment
            };
        }
    }
}
