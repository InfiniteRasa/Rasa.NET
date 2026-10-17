using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Crafting.Client;
    using Packets.Crafting.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Crafting stations (Kraftwerks, to the client) and the crafting requests made at them.
    ///
    /// The station is a usable dynamic object. Using it is what opens the crafting window: the
    /// client's Kraftwerks augmentation posts UI_CRAFTINGSTATION_ACTIVATE on Recv_Use for the actor
    /// who used it. Every request the window then makes names the station and is answered on it:
    /// CraftingStatus carries the player's jobs at that station, CraftingSuccess/Failure the outcome
    /// the window reports. The client resets its job list when it uses a station, so a status
    /// message follows every use.
    ///
    /// Fabrication (roadmap 3.2) is implemented here with the rules of the client's shared/crafting.py:
    /// a schematic in the player's inventory names a recipe (RecipeManager); the player must be at
    /// the recipe's level, able to pay its credit cost and holding every ingredient class in the
    /// stated quantity, standing within reach of a station with no job of theirs still running
    /// there. The ingredients and credits are taken, the schematic is kept, and a job with the
    /// recipe's time goes on the station for that player; the result is created when they take
    /// it. The window shows the job in its output slot from the start - CraftingStatus reports no
    /// time left, or the window would close itself - while the time left runs on the on-screen
    /// countdown, put up on Fabricate and again on every use of the station while the job runs.
    /// A Take before it is done is told how long is left, and "Crafting success!" says when it
    /// is. The window's other four pages - salvage, extraction, integration, upgrade - work on
    /// the modules an item carries, and are in KraftwerksManager.Modules.
    ///
    /// A job is a crafting_job row from the moment it is accepted until it is taken: the
    /// ingredients and credits went when it was made, so a restart, a station a GM moved or a map
    /// closed used to lose them and the product with them. Jobs are kept under the station's
    /// kraftwerks row id, which outlives the entity id a moved station gets, and loaded again at
    /// start with the time they had left (LoadJobs).
    /// </summary>
    public partial class KraftwerksManager
    {
        private static KraftwerksManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>The use arg the client sends for a station: usabledata[9595][0]. UseObject recovery is dispatched on it.</summary>
        public const uint UseObjectArgId = 5;

        /// <summary>Windup the client is told for a use; the window opens as soon as Recv_Use arrives, so this only paces the animation.</summary>
        public const int UseWindupMs = 100;

        /// <summary>shared/gameconstants.py MAX_INTERACTION_RANGE; a request from further away than this is not honoured.</summary>
        public const float InteractionRange = 5.0f;

        /// <summary>shared/crafting.py g_maxSimultaneousCraftItems: jobs a player may have waiting at one station.</summary>
        public const int MaxJobsPerStation = 301;

        /// <summary>The crafting window's fabrication page (generated.shared.crafting CRAFTACTION_FABRICATION); the page a job reports decides which page the window reopens on.</summary>
        public const uint FabricationPage = 1;

        /// <summary>The on-screen timer a running job's time left is shown on: generated.client.timertype "Countdown".</summary>
        public const TimerType CountdownTimer = TimerType.Countdown;

        /// <summary>Ids for the jobs no row holds - a station with no row, a write that failed - well clear of the row ids.</summary>
        private ulong _nextJobId = 1UL << 40;

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _currencyManager;

        /// <summary>Every station by database id, live object included.</summary>
        private readonly Dictionary<uint, Station> _stations = new Dictionary<uint, Station>();

        /// <summary>Jobs per (kraftwerks row, character), for CraftingStatus. What crafting_job holds, kept in step with it.</summary>
        private readonly Dictionary<(uint station, uint character), List<CraftingJob>> _jobs = new Dictionary<(uint, uint), List<CraftingJob>>();

        public class Station
        {
            public KraftwerksEntry Entry;
            public DynamicObject Object;
        }

        public static KraftwerksManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new KraftwerksManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal KraftwerksManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _currencyManager = new ManifestationManager(gameUnitOfWorkFactory);
        }

        public IEnumerable<Station> Stations => _stations.Values;

        public bool TryGet(uint id, out Station station) => _stations.TryGetValue(id, out station);

        #region Loading and placing

        /// <summary>Loads the kraftwerks table into the maps. Runs after MapChannelInit; the objects enter the world from the dynamic-object worker.</summary>
        public void KraftwerksInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entries = unitOfWork.Kraftwerks.GetKraftwerks();
            var placed = 0;

            foreach (var entry in entries)
                if (Place(entry) != null)
                    placed++;

            Logger.WriteLog(LogType.Initialize, $"Loaded {placed} crafting stations ({entries.Count} rows)");

            LoadJobs();
        }

        /// <summary>
        /// The jobs the stations were making or holding when the server last ran, with the time
        /// each had left: a job that finished meanwhile is finished, and its crafter is told the
        /// first time they are on the station's map (Worker). A row at a station that is not
        /// placed - its map not loaded, its row gone by hand - is left in the table and logged.
        /// </summary>
        internal void LoadJobs()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var rows = unitOfWork.CraftingJobs.GetAll();
            var now = DateTime.UtcNow;
            var tick = Environment.TickCount64;
            var loaded = 0;
            var orphaned = 0;

            foreach (var row in rows)
            {
                if (!_stations.ContainsKey(row.KraftwerksId))
                {
                    orphaned++;
                    continue;
                }

                var key = (row.KraftwerksId, row.CharacterId);

                if (!_jobs.TryGetValue(key, out var jobs))
                    _jobs[key] = jobs = new List<CraftingJob>();

                jobs.Add(new CraftingJob
                {
                    ResultItemId = row.Id,
                    ResultClassId = row.ResultClassId,
                    ResultItemTemplateId = row.ResultTemplateId,
                    Count = row.Count,
                    CraftingPage = row.CraftingPage,
                    QualityId = row.QualityId,
                    FinishTick = tick + (long)Math.Max(0, (row.FinishAt - now).TotalMilliseconds),
                    Persisted = true
                });
                loaded++;
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {loaded} crafting jobs ({rows.Count} rows)");

            if (orphaned > 0)
                Logger.WriteLog(LogType.Error, $"{orphaned} crafting job row(s) are at stations that are not placed; left in crafting_job");
        }

        /// <summary>
        /// Builds the live object for a row and puts it in its map's list. Null when the map is not
        /// loaded: that is logged, not raised as a map error, because it is not something a GM can
        /// put right in game - the seven seeded stations on the two wargame maps wait for those maps
        /// to be added to map_info.
        /// </summary>
        private Station Place(KraftwerksEntry entry)
        {
            if (!MapChannelManager.Instance.MapChannelArray.TryGetValue(entry.MapContextId, out var mapChannel))
            {
                Logger.WriteLog(LogType.Initialize, $"  kraftwerks {entry.Id} ({entry.Comment}) is on map {entry.MapContextId}, which is not loaded; skipped");
                return null;
            }

            var station = new Station
            {
                Entry = entry,
                Object = new DynamicObject
                {
                    Position = entry.Position,
                    Rotation = entry.Rotation,
                    MapContextId = entry.MapContextId,
                    EntityClassId = (EntityClasses)entry.ClassId,
                    DynamicObjectType = DynamicObjectType.Kraftwerks,
                    StateId = UseObjectState.CrafterState0,
                    WindupTime = UseWindupMs,
                    Comment = entry.Comment
                }
            };

            Register(station, mapChannel);

            return station;
        }

        /// <summary>Puts a station in the table and its map's list; its object carries the row id its jobs are kept under.</summary>
        internal void Register(Station station, MapChannel mapChannel)
        {
            station.Object.KraftwerksId = station.Entry.Id;
            _stations[station.Entry.Id] = station;
            mapChannel.Kraftwerks[station.Entry.Id] = station.Object;
        }

        /// <summary>Takes a station out of the world and its map's list; the row is untouched.</summary>
        private void Unplace(Station station)
        {
            if (MapChannelManager.Instance.MapChannelArray.TryGetValue(station.Entry.MapContextId, out var mapChannel))
            {
                if (station.Object.IsInWorld)
                    CellManager.Instance.RemoveFromWorld(mapChannel, station.Object);

                mapChannel.Kraftwerks.Remove(station.Entry.Id);
            }

            _stations.Remove(station.Entry.Id);
        }

        /// <summary>
        /// From DynamicObjectWorker, once a second: puts stations that are not in the world yet
        /// into it, and tells players whose jobs on this map's stations have just finished, so
        /// Take hands the item over without them having to use the station again. A fabrication
        /// that has just finished is also announced with the client's "Crafting success!": the
        /// window looks the same before and after, the item having been in its output slot all
        /// along.
        /// </summary>
        internal void Worker(MapChannel mapChannel)
        {
            foreach (var station in mapChannel.Kraftwerks.Values)
            {
                if (station.IsInWorld)
                    continue;

                CellManager.Instance.AddToWorld(mapChannel, station);
                station.IsInWorld = true;
            }

            if (_jobs.Count == 0)
                return;

            // The jobs are one list for every map, and every populated map's worker walks it.
            // A job is marked reported only once its status has gone to the crafter, on the map
            // its station stands on: the first map to tick used to mark it whether or not the
            // crafter was there, so a job finished while they were on another map was never
            // reported - Take did nothing and Fabricate stayed disabled until they used the
            // station again. A crafter away from the station's map is told when they are back
            // on it, or when they next use the station (SendStatus).
            foreach (var entry in _jobs)
            {
                var (stationId, characterId) = entry.Key;

                if (!entry.Value.Any(job => !job.FinishReported && job.IsFinished))
                    continue;

                if (!_stations.TryGetValue(stationId, out var station) || station.Entry.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                var client = mapChannel.ClientList.FirstOrDefault(c => c?.Player != null && c.Player.Id == characterId && c.State == ClientState.Ingame);

                if (client == null)
                    continue;

                var made = entry.Value.Any(job => !job.FinishReported && job.IsFinished && job.CraftingPage == FabricationPage);

                SendStatus(client, station.Object);

                if (made)
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmCraftingSuccess, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }
        }

        #endregion

        #region Using a station

        /// <summary>
        /// RequestUseObject on a station: the windup, the Use the client opens its window on, and
        /// the player's jobs there.
        ///
        /// The action is a use of this station - <see cref="ActionId.UseObject"/>, which
        /// DynamicObjectManager has established before this is reached. It used to be whatever
        /// action id the packet named, which made a station a way to have any action at all
        /// performed on the player. The arg id is still the client's, because the client matches
        /// the windup and recovery it gets against the ones it sent (every one of the 39 station
        /// classes carries <see cref="UseObjectArgId"/> as its own).
        /// </summary>
        internal void Use(Client client, DynamicObject station, uint actionArgId)
        {
            client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, ActionId.UseObject, actionArgId));
            client.CallMethod(station.EntityId, new UsePacket(client.Player.EntityId, station.StateId, UseWindupMs));
            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.UseObject, actionArgId, UseWindupMs));
            station.TriggeredByPlayers.Add(client);

            SendStatus(client, station);
            ShowCountdown(client, station);
        }

        /// <summary>PerformRecovery for UseObject arg 5: the use animation is over; leave the station as it was.</summary>
        internal void UseRecovery(MapChannel mapChannel, ActionData action)
        {
            foreach (var station in mapChannel.Kraftwerks.Values)
            {
                var user = station.TriggeredByPlayers.FirstOrDefault(c => c.Player == action.Actor);

                if (user == null)
                    continue;

                station.TriggeredByPlayers.Remove(user);

                if (!action.IsInrerrupted)
                {
                    CellManager.Instance.CellCallMethod(station, new UsableInfoPacket(station.IsEnabled, station.StateId, 0, station.WindupTime, station.ActivateMission));
                    MissionApplication.Instance.RecordProgress(
                        user,
                        MissionProgressEvent.Interaction(
                            (uint)station.EntityClassId));
                }

                return;
            }
        }

        private List<CraftingJob> JobsFor(Client client, DynamicObject station)
        {
            return _jobs.TryGetValue((station.KraftwerksId, client.Player.Id), out var jobs) ? jobs : new List<CraftingJob>();
        }

        /// <summary>Whether a job at any station is making an item of this template for the character.</summary>
        private bool Makes(uint characterId, uint itemTemplateId) =>
            _jobs.Any(entry => entry.Key.character == characterId && entry.Value.Any(job => job.ResultItemTemplateId == itemTemplateId && job.Count > 0));

        private List<CraftingJob> JobsForWriting(Client client, DynamicObject station)
        {
            var key = (station.KraftwerksId, client.Player.Id);

            if (!_jobs.TryGetValue(key, out var jobs))
                _jobs[key] = jobs = new List<CraftingJob>();

            return jobs;
        }

        /// <summary>
        /// A job accepted: its row, whose id is the job's from now on. A station with no row (one
        /// made by hand, outside the table) or a write that failed leaves the job in memory alone,
        /// under an id of its own, and the failure is logged - the ingredients have gone either way.
        /// </summary>
        private void Persist(Client client, DynamicObject station, CraftingJob job)
        {
            if (_gameUnitOfWorkFactory != null && station.KraftwerksId != 0)
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                var id = unitOfWork.CraftingJobs.Add(new Structures.Char.CraftingJobEntry
                {
                    CharacterId = client.Player.Id,
                    KraftwerksId = station.KraftwerksId,
                    ResultClassId = job.ResultClassId,
                    ResultTemplateId = job.ResultItemTemplateId,
                    Count = job.Count,
                    CraftingPage = job.CraftingPage,
                    QualityId = job.QualityId,
                    FinishAt = DateTime.UtcNow.AddMilliseconds(Math.Max(0, job.FinishTick - Environment.TickCount64))
                });

                if (id != 0)
                {
                    job.ResultItemId = id;
                    job.Persisted = true;
                    return;
                }

                Logger.WriteLog(LogType.Error, $"crafting job for {client.Player.FamilyName} at station {station.KraftwerksId} could not be saved; it is held in memory until the server stops");
            }

            job.ResultItemId = _nextJobId++;
        }

        /// <summary>A character deleted: their jobs go from memory. The rows went with the character (CharacterManager).</summary>
        internal void ForgetCharacter(uint characterId)
        {
            foreach (var key in _jobs.Keys.Where(key => key.character == characterId).ToList())
                _jobs.Remove(key);
        }

        /// <summary>
        /// The player's jobs at the station, to their client. A finished job's finish is reported
        /// by this, however it came to be sent - the worker, a use of the station, a job taken -
        /// so the worker does not send it again.
        /// </summary>
        private void SendStatus(Client client, DynamicObject station)
        {
            var jobs = JobsFor(client, station);

            client.CallMethod(station.EntityId, new CraftingStatusPacket(client.Player.EntityId, jobs));

            foreach (var job in jobs)
                if (job.IsFinished)
                    job.FinishReported = true;
        }

        /// <summary>
        /// Puts the on-screen countdown up for the player's job at the station that is still being
        /// made, from the time it has left; nothing when none is. The client takes the timer down
        /// itself when it reaches 0. It is the timer a timed mission objective would use, but this
        /// server sends no objective timers.
        /// </summary>
        private void ShowCountdown(Client client, DynamicObject station)
        {
            var running = JobsFor(client, station).Where(job => !job.IsFinished).ToList();

            if (running.Count == 0)
                return;

            NotificationManager.Instance.DisplayTimer(client, CountdownTimer, SecondsLeft(running), true);
        }

        /// <summary>Whole seconds until the last of the jobs is done, rounded up so a job is never shown as done before it is.</summary>
        private static int SecondsLeft(IEnumerable<CraftingJob> jobs) =>
            (int)Math.Ceiling(jobs.Max(job => job.TimeLeftSeconds));

        /// <summary>
        /// The station a request names, when it is one and the player is close enough to use it.
        /// Null otherwise; the caller has nothing sensible to answer on, so it answers nothing.
        /// </summary>
        private DynamicObject StationFor(Client client, ulong kraftwerksId, string request)
        {
            if (!EntityManager.Instance.TryGetObject(kraftwerksId, out var station) || station.DynamicObjectType != DynamicObjectType.Kraftwerks)
            {
                Logger.WriteLog(LogType.Debug, $"{request} from {client.Player.FamilyName} names {kraftwerksId}, which is not a crafting station");
                return null;
            }

            if (Vector3.Distance(client.Player.Position, station.Position) > InteractionRange + 2f)
            {
                Logger.WriteLog(LogType.Debug, $"{request} from {client.Player.FamilyName} at {Vector3.Distance(client.Player.Position, station.Position):0.#} m from station {kraftwerksId}");
                return null;
            }

            return station;
        }

        /// <summary>Tells the window the request failed (so it re-enables its buttons) and the player why.</summary>
        private void Fail(Client client, DynamicObject station, string why)
        {
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Failure(client.Player.EntityId));

            if (why != null)
                CommunicatorManager.Instance.SystemMessage(client, why);
        }

        private void Fail(Client client, DynamicObject station, PlayerMessage message)
        {
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Failure(client.Player.EntityId));
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        /// <summary>The 1.16.5 window: (station, the schematic item's entity id, page).</summary>
        internal void RequestCraftItemNew(Client client, RequestCraftItemNewPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestCraftItemNew");

            if (station == null)
                return;

            if (packet.CraftingPage != FabricationPage)
            {
                Fail(client, station, null);
                return;
            }

            if (!client.Player.Inventory.PersonalInventory.Contains(packet.RecipeItemId))
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} asked to craft from item {packet.RecipeItemId}, which is not in their inventory");
                Fail(client, station, null);
                return;
            }

            var schematic = EntityManager.Instance.GetItem(packet.RecipeItemId);

            if (schematic?.ItemTemplate == null)
            {
                Fail(client, station, null);
                return;
            }

            Fabricate(client, station, schematic.ItemTemplate.ItemTemplateId);
        }

        /// <summary>The older form: (station, schematic template id). The player still has to be holding one.</summary>
        internal void RequestCraftItem(Client client, RequestCraftItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestCraftItem");

            if (station == null)
                return;

            var holdsOne = client.Player.Inventory.PersonalInventory.Any(id => id != 0 && EntityManager.Instance.GetItem(id)?.ItemTemplate?.ItemTemplateId == packet.RecipeTemplateId);

            if (!holdsOne)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} asked to craft recipe {packet.RecipeTemplateId} without holding the schematic");
                Fail(client, station, null);
                return;
            }

            Fabricate(client, station, packet.RecipeTemplateId);
        }

        /// <summary>
        /// CanManifestationUseRecipeNow and CanManifestationUseKraftwerksNow from shared/crafting.py,
        /// then the job. A plain fabrication cannot fail once it has been accepted: the shared rules
        /// roll dice only for the module paths.
        /// </summary>
        private void Fabricate(Client client, DynamicObject station, uint schematicTemplateId)
        {
            var player = client.Player;

            if (!RecipeManager.Instance.TryGet(schematicTemplateId, out var recipe))
            {
                Logger.WriteLog(LogType.Debug, $"{player.FamilyName}: no recipe for schematic template {schematicTemplateId}");
                Fail(client, station, "That schematic has no recipe on this server.");
                return;
            }

            if (player.Level < recipe.MinLevel)
            {
                Fail(client, station, $"You need to be level {recipe.MinLevel} to fabricate that.");
                return;
            }

            if (player.Credits[CurencyType.Credits] < recipe.EnergyCost)
            {
                Fail(client, station, PlayerMessage.PmInsufficientFunds);
                return;
            }

            foreach (var input in recipe.Inputs)
            {
                var have = InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)input.ClassId);

                if (have < input.Quantity)
                {
                    Fail(client, station, $"You need {input.Quantity} {NameOfClass(input.ClassId)} and have {have}.");
                    return;
                }
            }

            var jobs = JobsForWriting(client, station);

            if (jobs.Count >= MaxJobsPerStation)
            {
                Fail(client, station, "This station is holding too many finished items for you; take some first.");
                return;
            }

            if (jobs.Any(j => !j.IsFinished))
            {
                Fail(client, station, "This station is still working on something for you.");
                return;
            }

            var result = ItemManager.Instance.GetItemTemplateById(recipe.ResultTemplateId);

            if (result == null)
            {
                Fail(client, station, null);
                return;
            }

            // Character Unique: not while one is held, or on a station waiting to be taken -
            // decided before the ingredients go.
            if (InventoryManager.IsCharacterUnique(result) &&
                (!InventoryManager.Instance.MayReceive(player, result) || Makes(player.Id, result.ItemTemplateId)))
            {
                Fail(client, station, PlayerMessage.PmItemCharacterUnique);
                return;
            }

            if (recipe.EnergyCost > 0 &&
                !_currencyManager.LossCredits(client, (int)recipe.EnergyCost))
            {
                Fail(client, station, PlayerMessage.PmInsufficientFunds);
                return;
            }

            if (recipe.EnergyCost > 0)
                EconomyAudit.Instance.Record(Structures.Char.EconomyLogKind.Crafting, recipe.TemplateId,
                    EconomyAudit.MoneyLine(client, CurencyType.Credits, -(long)recipe.EnergyCost));

            // Everything checked; now take the ingredients. The schematic stays.
            foreach (var input in recipe.Inputs)
            {
                var short_ = InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)input.ClassId, input.Quantity);

                if (short_ != 0)
                    Logger.WriteLog(LogType.Error, $"fabricate {recipe.TemplateId} for {player.FamilyName}: {short_} of class {input.ClassId} could not be removed after the count passed");
            }

            var job = new CraftingJob
            {
                ResultClassId = (uint)result.Class,
                ResultItemTemplateId = recipe.ResultTemplateId,
                Count = recipe.ResultAmount,
                CraftingPage = FabricationPage,
                QualityId = (uint)result.QualityId,
                FinishTick = Environment.TickCount64 + recipe.KraftwerksSeconds * 1000L
            };

            Persist(client, station, job);
            jobs.Add(job);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} fabricates {recipe.ResultAmount} x template {recipe.ResultTemplateId} from schematic {recipe.TemplateId} at station {station.EntityId}, ready in {recipe.KraftwerksSeconds} s");

            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(player.EntityId));
            ShowCountdown(client, station);
        }

        private static string NameOfClass(uint classId)
        {
            var entityClass = EntityClassManager.Instance.GetClassInfo((EntityClasses)classId);

            return entityClass?.ClassName ?? $"items of class {classId}";
        }

        /// <summary>
        /// Gives the player what of a finished job they have room for, in one transaction with the
        /// job's row: the items (onto their stacks, then into free slots of their category) and the
        /// row's delete, or its count brought down to what is left, are written together or not at
        /// all, so a crash or a failed write can neither lose the product nor hand it out twice.
        /// What is left stays on the station until the player makes room. Returns true when the
        /// whole job was handed over.
        ///
        /// What was fabricated carries its maker's name, and what does not fit the backpack goes
        /// to the inbox (the auction house's Pick Up Items). Mimeogel and modules do neither: they
        /// were salvaged, taken out or upgraded, not made, and wait at the station for room.
        /// </summary>
        private bool HandOver(Client client, DynamicObject station, CraftingJob job)
        {
            // Character Unique: the job stays on the station while one is held.
            if (!InventoryManager.Instance.MayReceive(client.Player, ItemManager.Instance.GetItemTemplateById(job.ResultItemTemplateId)))
            {
                InventoryManager.TellCharacterUnique(client);
                return false;
            }

            var made = job.CraftingPage == FabricationPage;

            return HandOver(client, station, job, made ? client.Player.FamilyName ?? "" : null, overflowToInbox: made);
        }

        /// <summary>
        /// Take: what of the jobs asked for is done is handed over. The window offers Take for a job
        /// still being made too (CraftingStatus reports no time left), so one that is not done
        /// stays, and the player is told how long it has left.
        /// </summary>
        private void Collect(Client client, DynamicObject station, Func<CraftingJob, bool> which)
        {
            var jobs = JobsForWriting(client, station);
            var running = jobs.Where(j => !j.IsFinished && which(j)).ToList();

            foreach (var job in jobs.Where(j => j.IsFinished && which(j)).ToList())
            {
                // The row went, or was brought down to what is left, with the items (HandOver).
                if (HandOver(client, station, job))
                {
                    jobs.Remove(job);
                    continue;
                }

                break;
            }

            if (jobs.Count == 0)
                _jobs.Remove((station.KraftwerksId, client.Player.Id));

            SendStatus(client, station);

            if (running.Count > 0)
                CommunicatorManager.Instance.SystemMessage(client, $"Still being made: ready in {SecondsLeft(running)} s.");
        }

        internal void RequestRetrieveFinishedCraftItem(Client client, RequestRetrieveFinishedCraftItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestRetrieveFinishedCraftItem");

            if (station != null)
                Collect(client, station, j => j.ResultItemId == packet.ItemId);
        }

        internal void RequestRetrieveAllFinishedItems(Client client, RequestRetrieveAllFinishedItemsPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestRetrieveAllFinishedItems");

            if (station != null)
                Collect(client, station, _ => true);
        }

        #endregion

        #region GM editing

        /// <summary>Creates a row and a live station at the position and facing given. Null when the insert failed or the map is not loaded.</summary>
        public Station Add(uint mapContextId, Vector3 position, double rotation, string comment)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = new KraftwerksEntry
            {
                ClassId = KraftwerksEntry.DefaultClassId,
                MapContextId = mapContextId,
                PosX = position.X,
                PosY = position.Y,
                PosZ = position.Z,
                Rotation = rotation,
                Comment = comment ?? string.Empty
            };

            if (unitOfWork.Kraftwerks.AddKraftwerks(entry) == 0)
                return null;

            return Place(entry);
        }

        /// <summary>Moves or turns a station: the row is updated, the object is rebuilt in place so clients see it move.</summary>
        public bool Move(Station station, Vector3 position, double rotation)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = station.Entry;
            entry.PosX = position.X;
            entry.PosY = position.Y;
            entry.PosZ = position.Z;
            entry.Rotation = rotation;

            if (!unitOfWork.Kraftwerks.UpdateKraftwerks(entry))
                return false;

            // A fresh object: RemoveFromWorld frees the entity id, so the old one cannot be re-added.
            Unplace(station);
            Place(entry);

            return true;
        }

        public bool SetComment(Station station, string comment)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            station.Entry.Comment = comment ?? string.Empty;
            station.Object.Comment = station.Entry.Comment;

            return unitOfWork.Kraftwerks.UpdateKraftwerks(station.Entry);
        }

        public bool Delete(Station station)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!unitOfWork.Kraftwerks.DeleteKraftwerks(station.Entry.Id))
                return false;

            Unplace(station);

            // The jobs it was making or holding go with it: there is no station to take them from.
            foreach (var key in _jobs.Keys.Where(key => key.station == station.Entry.Id).ToList())
                _jobs.Remove(key);

            using (var charUnitOfWork = _gameUnitOfWorkFactory.CreateChar())
            {
                var gone = charUnitOfWork.CraftingJobs.DeleteByKraftwerks(station.Entry.Id);

                if (gone > 0)
                    Logger.WriteLog(LogType.Debug, $"crafting station {station.Entry.Id} was deleted with {gone} job(s) on it; they went with it");
            }

            return true;
        }

        /// <summary>The stations on a map, nearest to the position first.</summary>
        public List<Station> OnMap(uint mapContextId, Vector3 position)
        {
            return _stations.Values.Where(s => s.Entry.MapContextId == mapContextId)
                                   .OrderBy(s => Vector3.Distance(s.Entry.Position, position))
                                   .ToList();
        }

        #endregion
    }
}
