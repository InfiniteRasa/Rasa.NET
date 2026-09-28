using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;
    using Timer;

    public class MapChannelManager
    {
        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly int MapChannel_PlayerQueue = 32;
        public readonly Dictionary<uint, MapChannel> MapChannelArray = new Dictionary<uint, MapChannel>();           // list of loaded maps
        public readonly Timer Timer = new();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Func<long> _clock;
        private readonly Action<Client, CharacterUpdate, object> _updateCharacter;
        private readonly Action<Client> _disconnect;
        private readonly Action<Client, bool> _refreshStats;
        private readonly Action<Client> _assignPlayer;
        private readonly Action<Client> _enterMapChannels;
        private readonly PrivateMapInstanceService _privateInstances;
        private readonly MissionDeadlineService _missionDeadlineService;
        private readonly IMissionSceneHost _missionScenarioService;
        public static MapChannelManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        // Nothing supplied a scenarioService here, ever - every test that exercises
                        // Rebuild/Release/Tick constructs its own fully-wired MapChannelManager, so
                        // this gap was invisible to all of them. In production _missionScenarioService
                        // stayed null and every `_missionScenarioService?.` call (Rebuild on building
                        // a private instance, Release on losing one, Tick every world tick) was a
                        // silent no-op - scheduled scenario steps never fired and a relog rebuilding a
                        // private instance never restored any scenario-spawned object, dispenser
                        // included. MissionApplication.Instance already builds and exposes the one real
                        // MissionSceneHost (ScenarioService); share that one instead of leaving
                        // this one unset, since PlanSpawnDynamicObject's runtime registry
                        // (_runtimeByMap) only means anything if Rebuild queries the same instance
                        // that created it.
                        if (_instance == null)
                            _instance = new MapChannelManager(
                                Server.GameUnitOfWorkFactory,
                                scenarioService: MissionApplication.Instance.ScenarioService);
                    }
                }

                return _instance;
            }
        }
        public MapChannelManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            Func<long> clock = null, Action<Client, CharacterUpdate, object> updateCharacter = null,
            Action<Client> disconnect = null, Action<Client, bool> refreshStats = null,
            Action<Client> assignPlayer = null, Action<Client> enterMapChannels = null,
            PrivateMapInstanceService privateInstances = null,
            MissionDeadlineService missionDeadlineService = null,
            IMissionSceneHost scenarioService = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _clock = clock ?? (() => Environment.TickCount64);
            _updateCharacter = updateCharacter ?? ((client, update, value) =>
                CharacterManager.Instance.UpdateCharacter(client, update, value));
            _disconnect = disconnect ?? (client => client.Close(false));
            _refreshStats = refreshStats ?? ((client, fullReset) =>
                ManifestationManager.Instance.UpdateStatsValues(client, fullReset));
            _assignPlayer = assignPlayer ?? ManifestationManager.Instance.AssignPlayer;
            _enterMapChannels = enterMapChannels ?? CommunicatorManager.Instance.PlayerEnterMap;
            _privateInstances = privateInstances ?? PrivateMapInstanceService.Instance;
            _missionDeadlineService = missionDeadlineService ?? MissionDeadlineService.Instance;
            _missionScenarioService = scenarioService;
        }

        /// <summary>
        /// Wait between RequestLogout and an honoured CharacterLogout. Sent to the client as
        /// LogoutTimeRemaining, which keeps the logout window's Logout button disabled until it
        /// has elapsed.
        /// </summary>
        public const int LogoutDelayMs = 5000;

        /// <summary>
        /// Allowance for clock-rate drift on the early-logout check. A normal client cannot be
        /// early: it starts its countdown when LogoutTimeRemaining arrives, which is after the
        /// server recorded the request.
        /// </summary>
        private const int LogoutDelayToleranceMs = 250;

        public void CharacterLogout(Client client)
        {
            // Nothing requested, or the request was cancelled.
            if (client.Player.LogoutActive == false)
                return;

            // The delay used to be advisory - enforced only by the client's disabled button - so a
            // client that skipped the countdown could leave instantly, mid-fight. Refuse it until
            // the countdown the server announced has actually run.
            var waited = Environment.TickCount64 - client.Player.LogoutRequestedTick;

            if (waited < LogoutDelayMs - LogoutDelayToleranceMs)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} sent CharacterLogout {waited} ms into a {LogoutDelayMs} ms logout countdown; ignored");
                return;
            }

            client.Player.RemoveFromMap = true;
            client.State = ClientState.LoggedIn;
        }

        /// <summary>
        /// The logout window's Cancel button (client/ui/logoutwindow.py:108). Withdraws a pending
        /// logout, so a CharacterLogout that follows is ignored until the player requests again.
        /// </summary>
        public void CancelLogoutRequest(Client client)
        {
            // Once CharacterLogout has flagged the player for removal the logout is under way;
            // a late cancel does not pull them back.
            if (!client.Player.LogoutActive || client.Player.RemoveFromMap)
                return;

            client.Player.LogoutActive = false;
        }

        /// <summary>
        /// The map channel with this context id, or null. A context id that names no channel is
        /// an ordinary thing to ask about - an actor whose map was torn down, or one that never
        /// had one - and callers already treat the answer as optional: ActorManager.Heal guards
        /// "if (mapChannel != null)" before broadcasting, which the throw this used to do made
        /// unreachable. The world loop drives those callers, so the exception took the process
        /// down rather than the one heal.
        /// </summary>
        public MapChannel FindByContextId(uint contextId)
        {
            return MapChannelArray.TryGetValue(contextId, out var mapChannel) ? mapChannel : null;
        }

        public MapChannel FindByContextAndInstance(uint contextId, uint instanceId)
        {
            if (instanceId <= 1)
                return FindByContextId(contextId);

            return _privateInstances.FindByContextAndInstance(contextId, instanceId);
        }

        public MapChannel FindOwnedPrivateInstance(uint contextId, uint ownerCharacterId)
        {
            return _privateInstances.FindOwnedInstance(contextId, ownerCharacterId);
        }

        public MapChannel GetOrCreatePrivateInstance(uint contextId, uint ownerCharacterId)
        {
            var map = !MapChannelArray.TryGetValue(contextId, out var template)
                ? null
                : _privateInstances.GetOrCreate(template, ownerCharacterId,
                    map => InitializePrivateMapChannel(template, map));
            if (map != null)
                _missionScenarioService?.Rebuild(ownerCharacterId, map);
            return map;
        }

        public void ReleaseOwnedPrivateInstances(uint ownerCharacterId)
        {
            foreach (var map in _privateInstances.ReleaseOwned(ownerCharacterId))
            {
                CleanupPrivateMapChannel(map);
                _missionScenarioService?.Release(ownerCharacterId, map);
            }
        }

        public Dictionary<int, AbilityDrawerData> GetPlayerAbilities(uint characterId)
        {
            var abilities = new Dictionary<int, AbilityDrawerData>();
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var abilitiesData = unitOfWork.CharacterAbilityDrawers.GetCharacterAbilities(characterId);
            if (abilitiesData == null)
                return abilities;

            foreach (var ability in abilitiesData)
            {
                if (ability.AbilityId == 0) continue;

                abilities.Add(ability.AbilitySlot, new AbilityDrawerData(ability.AbilitySlot, ability.AbilityId, ability.AbilityLevel));
            }

            return abilities;
        }

        public Dictionary<SkillId, SkillsData> GetPlayerSkills(uint characterId)
        {
            var skills = new Dictionary<SkillId, SkillsData>();
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var skillsData = unitOfWork.CharacterSkills.GetCharacterSkills(characterId);
            if (skillsData == null)
                return skills;

            foreach (var skill in skillsData)
                skills.Add((SkillId)skill.SkillId, new SkillsData((SkillId)skill.SkillId, skill.AbilityId, skill.SkillLevel));

            return skills;
        }

        public void MapChannelInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var loadedMaps = unitOfWork.MapInfos.Get();

            foreach (var mapInfo in loadedMaps)
            {
                // load all maps
                var newMapChannel = new MapChannel
                {
                    MapInfo = new MapInfo(mapInfo),
                    //TimerClientEffectUpdate = Environment.TickCount,
                    //TimerMissileUpdate = Environment.TickCount,
                    //TimerDynObjUpdate = Environment.TickCount,
                    //TimerGeneralTimer = Environment.TickCount,
                    //TimerController = Environment.TickCount,
                    //TimerPlayerUpdate = Environment.TickCount,
                    //PlayerCount = 0,
                    PlayerLimit = 128,
                    ClientList = new List<Client>()
                };
                SpawnPoolManager.Instance.InitializeMapChannel(newMapChannel);
                // register mapChannel
                MapChannelArray.Add(mapInfo.Id, newMapChannel);
            }
            Timer.Add("AutoFire", 100, true, null);
            Timer.Add("CheckForLogingClients", 1000, true, null);
            Timer.Add("CheckForObjects", 1000, true, null);
            Timer.Add("ClientEffectUpdate", 500, true, null);
            Timer.Add("CellUpdateVisibility", 1000, true, null);
            Timer.Add("CheckForCreatures", 1000, true, null);
            Timer.Add("CheckForMapTriggers", 1000, true, null);
            Timer.Add("MissionDeadlineUpdate", 1000, true, null);
            Timer.Add("Regenerate", 1000, true, null);
        }

        public void MapChannelWorker(long delta)
        {
            Timer.Update(delta);

            PartyManager.Instance.ExpireHeldMembers();

            // Server-wide lists, ticked once. These used to run inside the per-map loop below,
            // guarded by that map having players, so with N populated maps every auto-fire
            // timer and every dropship advanced N times per tick.
            if (Timer.IsTriggered("AutoFire"))
                ManifestationManager.Instance.AutoFireTimerDoWork(delta);

            foreach (var mapChannel in MapChannelArray.Values
                         .Concat(_privateInstances.Snapshot())
                         .Distinct()
                         .ToArray())
            {

                mapChannel.MapChannelElapsed += delta;
                DynamicObjectManager.Instance.DropshipsWorker(mapChannel, delta);

                if (Timer.IsTriggered("CheckForLogingClients"))
                    if (mapChannel.QueuedClients.Count > 0)
                    {
                        // create new mapClient
                        var dequedClient = mapChannel.QueuedClients.Dequeue();

                        // add it to list
                        mapChannel.ClientList.Add(dequedClient);
                    }

                if (mapChannel.ClientList.Count > 0)
                {
                    ActorActionManager.Instance.DoWork(mapChannel, delta);
                    MissileManager.Instance.DoWork(mapChannel, delta);
                    BehaviorManager.Instance.MapChannelThink(mapChannel, delta);

                    // despawn timers, and minions whose master has gone
                    MinionManager.Instance.Worker(mapChannel, delta);

                    // players whose combat timer has run out
                    ManifestationManager.Instance.CombatWorker(mapChannel);

                    // CellManager worker
                    if (Timer.IsTriggered("CellUpdateVisibility"))
                        CellManager.Instance.DoWork(mapChannel);

                    // check for objects
                    if (Timer.IsTriggered("CheckForObjects"))
                        DynamicObjectManager.Instance.DynamicObjectWorker(mapChannel, delta);

                    // check for creatures
                    if (Timer.IsTriggered("CheckForCreatures"))
                        SpawnPoolManager.Instance.SpawnPoolWorker(mapChannel, delta);

                    // check for mapTriggers
                    if (Timer.IsTriggered("CheckForMapTriggers"))
                    {
                        MapTriggerManager.Instance.TriggersProximityWorker(mapChannel);

                        // zone borders and instance doors: anyone standing in one leaves the map
                        MapLinkManager.Instance.Worker(mapChannel);

                        // ambient/music/sky/minimap regions: tell whoever changed region
                        RegionManager.Instance.Worker(mapChannel);
                    }

                    // check for effects (buffs)
                    if (Timer.IsTriggered("ClientEffectUpdate"))
                        GameEffectManager.Instance.DoWork(mapChannel, delta);

                    // a second's health, armour, power and chi for everyone here
                    if (Timer.IsTriggered("Regenerate"))
                        ActorManager.Instance.Regenerate(mapChannel);

                    _missionScenarioService?.TickMap(mapChannel);

                    // warn idle players and flag long-idle ones for removal below
                    ManifestationManager.Instance.CheckInactivity(mapChannel);

                    // check for players leaving the map: /logout, inactivity, and dropped
                    // connections flagged by Client.Close()
                    foreach (var client in mapChannel.ClientList)
                        if (client != null && client.Player.RemoveFromMap)
                        {
                            // The MainLoop thread has no handler of its own, so an exception
                            // escaping here stops the whole server ticking. Clear the flag
                            // first and drop the entry on failure so a bad removal is logged
                            // once instead of retried - and thrown - on every tick.
                            client.Player.RemoveFromMap = false;

                            try
                            {
                                RemovePlayer(client, true);
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(LogType.Error, $"Failed to remove {client.Player.FamilyName} from map {mapChannel.MapInfo.MapContextId}: {e}");
                                mapChannel.ClientList.Remove(client);
                            }

                            break;
                        }
                }
            }
        }

        public void MapLoaded(Client client)
        {
            lock (client.SyncRoot)
                InitializeLoadedMap(client);
        }

        private void InitializeLoadedMap(Client client)
        {
            // Only in answer to a Wonkavate, and once for each. Nothing was checked, and this is
            // one of the methods a connection may call from outside the world - the loading
            // screen is where it comes from - so it could be sent from anywhere, any number of
            // times, and every path below assumes a player who is arriving.
            //
            // From the character screen after a logout it put the character back in the world:
            // registered, in its old map's cells and introduced to everyone there, but on no map's
            // client list, which is the only place the removal that follows a dropped connection is
            // looked for. Once the connection closed, the character stayed where it was, frozen,
            // for as long as the server ran. A second one during a dropship arrival built a second
            // arrival dropship; the first landed the player, and the second found them already in
            // the world and took itself for a departure with nowhere to go - MapChannelArray[0], a
            // KeyNotFoundException at the top of the map channel worker, on every tick after,
            // because the dropship was only removed at the end of the phase that threw.
            //
            // The state has to agree as well as the flag: a pending load can be overtaken by
            // something else changing the state before the client answers.
            if (client.PendingTransfer != null && CheckTransferTimeout(client))
                return;

            if (!client.AwaitingMapLoaded ||
                (client.State != ClientState.Loading &&
                    !IsExpectedTransferMapLoad(client)))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry?.Id} sent MapLoaded in state {client.State} with {(client.AwaitingMapLoaded ? "a" : "no")} map load pending; ignored.");
                return;
            }

            if (client.State == ClientState.Loading &&
                (client.Player?.MapChannel == null ||
                    client.LoadingMap != client.Player.MapChannel.MapInfo.MapContextId))
            {
                Logger.WriteLog(LogType.Security, "Ignored MapLoaded for a mismatched map.");
                return;
            }

            client.AwaitingMapLoaded = false;
            RemoveQueuedClient(client.Player.MapChannel, client);

            if (client.State == ClientState.Teleporting)
            {
                if (client.PendingTransfer?.IsMapLink == true)
                {
                    CompleteMapLinkTransfer(client);
                    return;
                }

                var mapChannel = client.Player.MapChannel;
                if (!DynamicObjectManager.Instance.CompleteMapLoadTransfer(client))
                    return;

                var dropship = new Dropship(
                    Factions.AFS,
                    DropshipType.Teleporter,
                    client,
                    DropshipRole.Arrival);
                client.Player.MapChannel = mapChannel;
                client.Player.MapContextId = dropship.Client.LoadingMap;

                if (!mapChannel.ClientList.Contains(client))
                    mapChannel.ClientList.Add(client);

                CellManager.Instance.AddToWorld(client.Player.MapChannel, dropship);
                DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);
                CommunicatorManager.Instance.LoginOk(dropship.Client);
                ServerFlagManager.Instance.SendFlags(client);

                // The manifestation, its items and its entity registrations survive the map
                // change; the client's picture of them does not. Show it what the server
                // already has rather than loading and registering it all a second time, and
                // recompute the stats without the full reset that healed the player.
                InventoryManager.Instance.ResendForMap(client);
                _refreshStats(client, false);

                CellManager.Instance.AddToWorld(dropship.Client); // will introduce the player to all clients, including the current owner
                MapLinkManager.Instance.PlayerEnteredMap(client);
                CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new TeleportArrivalPacket());
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                _assignPlayer(client);
                CommunicatorManager.Instance.PlayerEnterMap(dropship.Client);

                return;
            }

            client.State = ClientState.Ingame;
            ManifestationManager.Instance.ResetInactivity(client);
            InventoryManager.Instance.InitForClient(client);
            ManifestationManager.Instance.UpdateStatsValues(client, true);
            client.Player.Attributes[Attributes.Chi].Current = 0;

            // register new Player
            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            EntityManager.Instance.RegisterActor(client.Player.EntityId, client.Player);
            CommunicatorManager.Instance.LoginOk(client);

            // Before anything the player can act on: the client asks its own flag set whether to
            // offer a feature, and an empty set means the feature is simply missing.
            ServerFlagManager.Instance.SendFlags(client);

            // Whatever is broken about the map they have just walked into, if they are someone
            // who can do anything about it. The dialog is modal and always visible, so a player
            // would be stuck reading about server data they cannot fix.
            if (client.AccountEntry != null && client.AccountEntry.Level >= (byte)GmLevel.Observer)
                MapErrorManager.Instance.SendTo(client);

            CellManager.Instance.AddToWorld(client); // will introduce the player to all clients, including the current owner

            // Before the first link check: a player who arrives through a pass is standing in
            // the gate on this side, and must walk out of it before it can send them back.
            MapLinkManager.Instance.PlayerEnteredMap(client);
            ManifestationManager.Instance.AssignPlayer(client);

            ClanManager.Instance.InitializePlayerClanData(client);
            InventoryManager.Instance.InitClanInventory(client);
            _enterMapChannels(client);
            PartyManager.Instance.PlayerEnteredWorld(client);
        }

        private static bool IsExpectedTransferMapLoad(Client client)
        {
            var transfer = client.PendingTransfer;
            return client.State == ClientState.Teleporting && transfer?.HasDeparted == true &&
                client.LoadingMap == transfer.DestinationMap.MapInfo.MapContextId &&
                client.Player?.MapChannel == transfer.DestinationMap;
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
                if (CellManager.Instance.IsInWorld(client))
                    CellManager.Instance.RemoveFromWorld(client);
                client.RestoreTransferOrigin();
                DynamicObjectManager.Instance.CleanupClientDropships(client);
                _disconnect(client);
                return true;
            }
        }

        private void CompleteMapLinkTransfer(Client client)
        {
            var transfer = client.PendingTransfer;
            if (transfer?.IsMapLink != true || !IsExpectedTransferMapLoad(client))
                return;

            var map = transfer.DestinationMap;
            try
            {
                _updateCharacter(client, CharacterUpdate.Position, null);
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error, $"Unable to persist player transfer: {error.Message}");
                CellManager.Instance.RemoveFromWorld(client);
                map.ClientList.RemoveAll(member => member == client);
                client.RestoreTransferOrigin();
                _disconnect(client);
                return;
            }

            if (!map.ClientList.Contains(client))
                map.ClientList.Add(client);

            InventoryManager.Instance.ResendForMap(client);
            _refreshStats(client, false);
            CellManager.Instance.AddToWorld(client);
            MapLinkManager.Instance.PlayerEnteredMap(client);
            _assignPlayer(client);

            client.PendingTransfer = null;
            client.State = ClientState.Ingame;
            ResumeMissionScenes(client);
            ManifestationManager.Instance.ResetInactivity(client);
            client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
            _enterMapChannels(client);
            if (transfer.ReleaseOwnedPrivateInstancesForCharacterId != 0)
                ReleaseOwnedPrivateInstances(transfer.ReleaseOwnedPrivateInstancesForCharacterId);
        }

        public void PassClientToCharacterSelection(Client client)
        {
            // ToDo
            /*if (ClientsGameMainCount >= MAX_GAMEMAIN_CLIENTS)
            {
                // force disconnect
                closesocket(cgm->socket);
                //free(cgm);
                return;
            }*/
            CharacterManager.Instance.StartCharacterSelection(client);
            //Increase count and return struct
            //ClientsGameMainCount++;
        }
        public void PassClientToMapInstance(Client client)
        {
            var mapInstance = client.Player.MapChannel;
            if (client.State == ClientState.Loading && mapInstance.QueuedClients.Contains(client))
            {
                Logger.WriteLog(LogType.Network, "Ignored duplicate map-load request.");
                return;
            }
            client.LoadingMap = mapInstance.MapInfo.MapContextId;
            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket
               (
                   mapInstance.MapInfo.MapContextId,
                   mapInstance.InstanceId,
                   mapInstance.MapInfo.MapVersion,
                    client.Player.Position,
                   (float)client.Player.Rotation
               ));

            client.State = ClientState.Loading;
            client.AwaitingMapLoaded = true;
            client.Player.MapChannel.QueuedClients.Enqueue(client);
        }

        internal static void RemoveQueuedClient(MapChannel map, Client client)
        {
            if (map == null)
                return;

            var count = map.QueuedClients.Count;
            for (var i = 0; i < count; i++)
            {
                var queued = map.QueuedClients.Dequeue();
                if (queued != client)
                    map.QueuedClients.Enqueue(queued);
            }
        }

        internal static void PruneQueuedClients(MapChannel map)
        {
            var count = map.QueuedClients.Count;
            var retained = new HashSet<Client>();
            for (var i = 0; i < count; i++)
            {
                var queued = map.QueuedClients.Dequeue();
                if (queued?.State == ClientState.Loading && queued.Player?.MapChannel == map && retained.Add(queued))
                    map.QueuedClients.Enqueue(queued);
            }
        }

        internal void CleanupDisconnected(Client client)
        {
            var player = client.Player;
            if (player == null)
                return;

            ManifestationManager.Instance.RemovePlayerCharacter(client);
            if (player.ClanId != 0)
                ClanManager.Instance.RemovePlayer(client);
            DynamicObjectManager.Instance.CleanupClientDropships(client);
            CommunicatorManager.Instance.LeaveMapChannels(client);

            foreach (var map in MapChannelArray.Values.Concat(_privateInstances.Snapshot()).Distinct())
            {
                CellManager.Instance.DetachClient(map, client);
                map.ClientList.RemoveAll(member => member == client);
                RemoveQueuedClient(map, client);
                DetachMissionScenes(client, map);
            }

            var inventoryIds = player.Inventory.PersonalInventory.Concat(player.Inventory.HomeInventory)
                .Concat(player.Inventory.EquippedInventory).Concat(player.Inventory.WeaponDrawer).Distinct();
            foreach (var id in inventoryIds.Where(id => id != 0))
                if (EntityManager.Instance.Items.ContainsKey(id))
                    EntityManager.Instance.ReleaseEntity(id, EntityType.Item);
            player.Inventory = new Inventory();

            if (EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered) && registered == player)
                EntityManager.Instance.ReleaseEntity(player.EntityId, EntityType.Character);

            player.Cells = new uint[5, 5];
            player.MapChannel = null;
            player.RuntimeMapChannel = null;
            player.RemoveFromMap = false;
            ReleaseOwnedPrivateInstances(player.Id);
            player.Disconected = true;
        }

        internal void DetachMissionScenes(Client client, MapChannel map) =>
            _missionScenarioService?.Detach(client, map);

        internal void ResumeMissionScenes(Client client) => _missionScenarioService?.Resume(client);

        /// <summary>
        /// Moves an ingame player to a position on any loaded map by way of the loading screen:
        /// out of the current map channel, then Wonkavate into the new one. Summon and .teleport
        /// each had a copy of this that forgot to point the player at the new map, so when the
        /// client answered with MapLoaded it was added to the OLD map's cells at its OLD position.
        /// Everyone there saw a frozen ghost, its broadcasts went to the wrong map, and the first
        /// cell crossing on the new map indexed the old map's cell table with new-map seeds and
        /// threw KeyNotFoundException on the main loop.
        /// </summary>
        /// <returns>false when the map is not loaded or the player is not in a state to move.</returns>
        public bool ChangeMap(Client client, uint mapContextId, Vector3 position, float orientation)
        {
            if (!MapChannelArray.TryGetValue(mapContextId, out var mapChannel))
                return false;
            return ChangeMap(client, mapChannel, position, orientation);
        }

        internal bool ChangeMap(
            Client client,
            MapChannel destinationMap,
            Vector3 position,
            float orientation,
            uint releaseOwnedPrivateInstancesForCharacterId = 0)
        {
            lock (client.SyncRoot)
            {
                if (client.Player == null || client.State != ClientState.Ingame ||
                    client.PendingTransfer != null || !CellManager.Instance.IsInWorld(client) ||
                    destinationMap == null ||
                    !CellManager.TryGetCellCoordinates(position, out _, out _))
                    return false;

                var timeout = client.Server?.Config.GameConfig.TransferTimeoutSeconds ??
                    Config.GameConfig.DefaultTransferTimeoutSeconds;
                if (timeout <= 0)
                    return false;

                var origin = client.Player.MapChannel;
                client.PendingTransfer = new PlayerTransfer
                {
                    OriginMap = origin,
                    OriginPosition = client.Player.Position,
                    OriginRotation = client.Player.Rotation,
                    DestinationMap = destinationMap,
                    DestinationPosition = position,
                    DestinationRotation = orientation,
                    Deadline = checked(_clock() + timeout * 1000L),
                    IsMapLink = true,
                    HasDeparted = true,
                    ReleaseOwnedPrivateInstancesForCharacterId = releaseOwnedPrivateInstancesForCharacterId
                };

                client.State = ClientState.Teleporting;
                client.Player.Target = 0;
                LootDispenserManager.Instance.RemoveForOwner(origin, client);
                ActorActionManager.Instance.RemoveActor(client.Player);
                GameEffectManager.Instance.ClearEffects(origin, client.Player);
                client.Player.WeaponReady = false;
                MinionManager.Instance.DismissAll(client);
                MapLinkManager.Instance.RemovePlayer(client);
                RegionManager.Instance.RemovePlayer(client);
                CommunicatorManager.Instance.LeaveMapChannels(client);
                CellManager.Instance.RemoveFromWorld(client);
                origin.ClientList.RemoveAll(member => member == client);
                DetachMissionScenes(client, origin);

                client.Player.MapChannel = destinationMap;
                client.Player.MapContextId = destinationMap.MapInfo.MapContextId;
                client.SetWorldPosition(position, orientation);
                client.LoadingMap = destinationMap.MapInfo.MapContextId;
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
                client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket(
                    destinationMap.MapInfo.MapContextId,
                    destinationMap.InstanceId,
                    destinationMap.MapInfo.MapVersion,
                    position,
                    orientation));
                client.AwaitingMapLoaded = true;
                return true;
            }
        }

        public void Ping(Client client, double ping)
        {
            client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(ping));
        }

        /// <summary>
        /// Destroys every item entity a slot list holds, and empties the list with them.
        ///
        /// Emptying it is the point. Destroying an item hands its entity id back to the
        /// EntityManager's free list, which gives that id to the next item created - somebody
        /// else's, moments later - while the slots here went on naming it. A connection that is
        /// no longer in the world still had its four lists: at the character screen after a
        /// logout, or between maps for as long as the client took to answer with MapLoaded. Every
        /// handler that resolves a slot to an entity id resolved those to another player's items,
        /// which was enough to auction, sell, bank or craft with them.
        /// </summary>
        private static void DestroyInventory(Client client, List<ulong> inventory)
        {
            foreach (var entityId in inventory)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            inventory.Clear();
        }

        public void RemovePlayer(Client client, bool logout)
        {
            DetachMissionScenes(client, client.Player.MapChannel);
            ManifestationManager.Instance.RemovePlayerCharacter(client);
            client.RestoreTransferOrigin();
            DynamicObjectManager.Instance.CleanupClientDropships(client);

            // A target is an entity on this map; the client does not always re-target after a
            // map change, and MissileLaunch refuses cross-map targets, so drop it here.
            client.Player.Target = 0;

            // unregister Communicator
            CommunicatorManager.Instance.PlayerExitMap(client);
            // unregister mapChannelClient
            EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
            EntityManager.Instance.UnregisterActor(client.Player.EntityId);

            // unregister character Inventory
            DestroyInventory(client, client.Player.Inventory.EquippedInventory);
            DestroyInventory(client, client.Player.Inventory.HomeInventory);
            DestroyInventory(client, client.Player.Inventory.PersonalInventory);
            DestroyInventory(client, client.Player.Inventory.WeaponDrawer);

            NpcManager.Instance.DiscardBuybackItems(client);
            ActorActionManager.Instance.RemoveActor(client.Player);

            // Effects are per map as far as the clients know - nobody on the next map was told
            // about them - and a sprint left running would keep draining adrenaline unseen.
            GameEffectManager.Instance.ClearEffects(client.Player.MapChannel, client.Player);

            // The weapon is put away with them. A manifestation arriving on a map starts with
            // nothing in its hands - the client transitions to _no_tool and is never told
            // otherwise, since nothing sends WeaponReady on map entry - while this flag lived on
            // the Manifestation, which survives the change. The two then disagreed for the rest
            // of the session: the server thought a weapon was out that the player could see was
            // not, which let a tool action through that the client refuses (basetoolaction.py
            // checks IsWeaponReady) and skipped the draw the fire path performs for itself.
            client.Player.WeaponReady = false;

            // Before the player leaves the cells, while their minions can still be told to go:
            // "Player-controlled subordinates will teleport with their masters, but not change
            // maps." Leaving the map is leaving them behind, so they are dismissed, not orphaned.
            MinionManager.Instance.DismissAll(client);

            CellManager.Instance.RemoveFromWorld(client);
            MapLinkManager.Instance.RemovePlayer(client);
            RegionManager.Instance.RemovePlayer(client);
            ClanManager.Instance.RemovePlayer(client);
            LookingForGroupManager.Instance.RemovePlayer(client);
            SummonManager.Instance.RemovePlayer(client);
            TradeManager.Instance.RemovePlayer(client);
            PartyManager.Instance.RemovePlayer(client);
            PetitionManager.Instance.RemovePlayer(client);
            client.Player.Inventory = new Inventory();

            if (logout)
                if (client.Player.Disconected == false)
                {
                    PassClientToCharacterSelection(client);
                    client.Player.Disconected = true;
                }

            // remove from list
            for (var i = 0; i < client.Player.MapChannel.ClientList.Count; i++)
            {
                if (client == client.Player.MapChannel.ClientList[i])
                {
                    client.Player.MapChannel.ClientList.RemoveAt(i);
                    //mapClient.MapChannel.PlayerCount--;
                    break;
                }
            }

        }

        /// <summary>
        /// Takes a disconnected player out of the world when no map channel is going to.
        ///
        /// Close() only flags a departing player; the map channel worker acts on the flag, and
        /// looks for it among the clients on its own map's list. A player on no map's list is
        /// never looked at. A dropship journey is exactly that: the departure takes the client
        /// off its map's list when it sends the Wonkavate, and only MapLoaded puts it on the
        /// arrival map's, keeping the manifestation and its items registered in between. A
        /// connection that dropped on that loading screen - a crash, Alt+F4 - left them all
        /// registered for as long as the server ran.
        ///
        /// Called on the main loop for each connection it drops. A player still registered and
        /// on no map's list or login queue is removed here, the way the worker would have; any
        /// other is left alone, so nobody is removed twice.
        /// </summary>
        public void RemoveStrandedPlayer(Client client)
        {
            var player = client.Player;

            if (player == null
                || !EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered)
                || registered != player)
                return;

            foreach (var mapChannel in MapChannelArray.Values)
                if (mapChannel.ClientList.Contains(client) || mapChannel.QueuedClients.Contains(client))
                    return;

            foreach (var mapChannel in _privateInstances.Snapshot())
                if (mapChannel.ClientList.Contains(client) || mapChannel.QueuedClients.Contains(client))
                    return;

            player.RemoveFromMap = false;

            try
            {
                RemovePlayer(client, true);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Failed to remove disconnected player {player.FamilyName}, who was on no map's client list: {e}");
            }
        }

        public void RequestLogout(Client client)
        {
            if (client.State != ClientState.Ingame || client.PendingTransfer != null)
            {
                Logger.WriteLog(LogType.Network, "Ignored logout outside the active world state.");
                return;
            }

            // A repeated request restarts the countdown, matching the fresh one the client shows.
            client.MissionConversation = null;
            client.Player.LogoutActive = true;
            client.Player.LogoutRequestedTick = Environment.TickCount64;

            client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket(LogoutDelayMs));
        }

        public MapInstance GetMapInstance(uint mapContextId)
        {
            // TODO support additional maps
            var map = new MapInstance(new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0));

            return map;
        }

        private static void CleanupPrivateMapChannel(MapChannel map)
        {
            if (map == null)
                return;

            foreach (var queued in map.QueuedClients.ToArray())
            {
                RemoveQueuedClient(map, queued);
                if (queued?.Player?.MapChannel == map)
                {
                    queued.Player.MapChannel = null;
                    queued.Player.RuntimeMapChannel = null;
                    queued.Player.Cells = new uint[5, 5];
                }
            }

            foreach (var client in map.ClientList.Distinct().ToArray())
            {
                CellManager.Instance.DetachClient(map, client);
                if (client?.Player?.MapChannel == map)
                {
                    client.Player.MapChannel = null;
                    client.Player.RuntimeMapChannel = null;
                    client.Player.Cells = new uint[5, 5];
                }
            }
            map.ClientList.Clear();

            foreach (var creature in map.MapCellInfo.Cells.Values
                         .SelectMany(cell => cell.CreatureList)
                         .Distinct()
                         .ToArray())
                CellManager.Instance.RemoveCreatureFromWorld(map, creature);

            DynamicObjectManager.Instance.CleanupMapDropships(map);

            var dynamicObjects = map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Concat(map.DynamicObjects)
                .Concat(map.ControlPoints.Values)
                .Concat(map.FootLockers.Values)
                .Concat(map.Teleporters.Values)
                .Concat(map.Kraftwerks.Values)
                .Distinct()
                .ToArray();
            foreach (var dynamicObject in dynamicObjects)
                CellManager.Instance.RemoveFromWorld(map, dynamicObject);

            foreach (var loot in map.LootDispensers.Values.ToArray())
            {
                foreach (var item in loot.LootItems)
                    if (item?.Item != null)
                        EntityManager.Instance.ReleaseEntity(item.Item.EntityId, EntityType.Item);
            }

            map.PerformRecovery.Clear();
            map.QueuedMissiles.Clear();
            map.SpawnPools.Clear();
            map.DynamicObjects.Clear();
            map.ControlPoints.Clear();
            map.FootLockers.Clear();
            map.Teleporters.Clear();
            map.Kraftwerks.Clear();
            map.LootDispensers.Clear();
            map.MapCellInfo.Cells.Clear();
        }

        private static void InitializePrivateMapChannel(MapChannel template, MapChannel map)
        {
            SpawnPoolManager.Instance.CloneTemplateMap(template, map);
            DynamicObjectManager.Instance.CloneTemplateMap(template, map);
        }
    }
}
