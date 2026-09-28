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
    /// it. The Crafting v2 pages - salvage, extraction, integration, upgrade - need per-item
    /// modules the server does not have yet, and are still declined with a failure the window
    /// can show.
    /// </summary>
    public class KraftwerksManager
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

        private ulong _nextJobId = 1;

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _currencyManager;

        /// <summary>Every station by database id, live object included.</summary>
        private readonly Dictionary<uint, Station> _stations = new Dictionary<uint, Station>();

        /// <summary>Jobs per (station entity, character), for CraftingStatus.</summary>
        private readonly Dictionary<(ulong station, uint character), List<CraftingJob>> _jobs = new Dictionary<(ulong, uint), List<CraftingJob>>();

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

        private KraftwerksManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
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

            _stations[entry.Id] = station;
            mapChannel.Kraftwerks[entry.Id] = station.Object;

            return station;
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
        /// into it, and tells players whose jobs on this map's stations have just finished, so the
        /// window shows the Take button without them having to use the station again.
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

            foreach (var entry in _jobs)
            {
                var (stationId, characterId) = entry.Key;
                var finished = false;

                foreach (var job in entry.Value)
                    if (!job.FinishReported && job.IsFinished)
                    {
                        job.FinishReported = true;
                        finished = true;
                    }

                if (!finished)
                    continue;

                var client = mapChannel.ClientList.FirstOrDefault(c => c?.Player != null && c.Player.Id == characterId && c.State == ClientState.Ingame);

                if (client != null && EntityManager.Instance.TryGetObject(stationId, out var station) && station.MapContextId == mapChannel.MapInfo.MapContextId)
                    SendStatus(client, station);
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
            return _jobs.TryGetValue((station.EntityId, client.Player.Id), out var jobs) ? jobs : new List<CraftingJob>();
        }

        private List<CraftingJob> JobsForWriting(Client client, DynamicObject station)
        {
            var key = (station.EntityId, client.Player.Id);

            if (!_jobs.TryGetValue(key, out var jobs))
                _jobs[key] = jobs = new List<CraftingJob>();

            return jobs;
        }

        private void SendStatus(Client client, DynamicObject station)
        {
            client.CallMethod(station.EntityId, new CraftingStatusPacket(client.Player.EntityId, JobsFor(client, station)));
        }

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

        /// <summary>
        /// The Crafting v2 requests, until items can carry modules: the window is told the
        /// request failed so it re-enables its buttons, and the player is told why.
        /// </summary>
        private void Decline(Client client, ulong kraftwerksId, string request, string what)
        {
            var station = StationFor(client, kraftwerksId, request);

            if (station == null)
                return;

            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Failure(client.Player.EntityId));
            CommunicatorManager.Instance.SystemMessage(client, $"{what} is not available on this server yet.");
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
                Fail(client, station, "Only fabrication is available on this server yet.");
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

            if (recipe.EnergyCost > 0 &&
                !_currencyManager.LossCredits(client, (int)recipe.EnergyCost))
            {
                Fail(client, station, PlayerMessage.PmInsufficientFunds);
                return;
            }

            // Everything checked; now take the ingredients. The schematic stays.
            foreach (var input in recipe.Inputs)
            {
                var short_ = InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)input.ClassId, input.Quantity);

                if (short_ != 0)
                    Logger.WriteLog(LogType.Error, $"fabricate {recipe.TemplateId} for {player.FamilyName}: {short_} of class {input.ClassId} could not be removed after the count passed");
            }

            var job = new CraftingJob
            {
                ResultItemId = _nextJobId++,
                ResultClassId = (uint)result.Class,
                ResultItemTemplateId = recipe.ResultTemplateId,
                Count = recipe.ResultAmount,
                CraftingPage = FabricationPage,
                QualityId = (uint)result.QualityId,
                FinishTick = Environment.TickCount64 + recipe.KraftwerksSeconds * 1000L
            };

            jobs.Add(job);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} fabricates {recipe.ResultAmount} x template {recipe.ResultTemplateId} from schematic {recipe.TemplateId} at station {station.EntityId}, ready in {recipe.KraftwerksSeconds} s");

            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(player.EntityId));
        }

        private static string NameOfClass(uint classId)
        {
            var entityClass = EntityClassManager.Instance.GetClassInfo((EntityClasses)classId);

            return entityClass?.ClassName ?? $"items of class {classId}";
        }

        /// <summary>
        /// Creates the result of a finished job in the player's inventory, in stacks of the class's
        /// stack size. What does not fit stays on the job, so the player can make room and take it
        /// again. Returns true when the whole job was handed over.
        /// </summary>
        private bool HandOver(Client client, CraftingJob job)
        {
            var classInfo = EntityClassManager.Instance.GetClassInfo((EntityClasses)job.ResultClassId);
            var stackSize = Math.Max(1u, classInfo?.ItemClassInfo?.StackSize ?? 1u);

            while (job.Count > 0)
            {
                var amount = Math.Min(job.Count, stackSize);
                var item = ItemManager.Instance.CreateFromTemplateId(job.ResultItemTemplateId, amount, client.Player.FamilyName);

                if (item == null)
                    return false;

                item.Crafter = client.Player.FamilyName;

                if (InventoryManager.Instance.GrantItemToInventory(client, item) != null)
                {
                    job.Count -= amount;
                    continue;
                }

                // Some or none of this stack fitted; the remainder's row and entity go away and
                // the job keeps what the player has not got yet.
                var placed = amount - item.StackSize;
                EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);

                if (item.Id != 0)
                    using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                        unitOfWork.Items.DeleteItem(item.Id);

                job.Count -= placed;
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return false;
            }

            return true;
        }

        private void Collect(Client client, DynamicObject station, Func<CraftingJob, bool> which)
        {
            var jobs = JobsForWriting(client, station);

            foreach (var job in jobs.Where(j => j.IsFinished && which(j)).ToList())
                if (HandOver(client, job))
                    jobs.Remove(job);
                else
                    break;

            if (jobs.Count == 0)
                _jobs.Remove((station.EntityId, client.Player.Id));

            SendStatus(client, station);
        }
        internal void RequestSalvageItem(Client client, RequestSalvageItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestSalvageItem", "Salvage");
        internal void RequestExtractModule(Client client, RequestExtractModulePacket packet) => Decline(client, packet.KraftwerksId, "RequestExtractModule", "Module extraction");
        internal void RequestIntegrateItem(Client client, RequestIntegrateItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestIntegrateItem", "Module integration");
        internal void RequestUpgradeItem(Client client, RequestUpgradeItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestUpgradeItem", "Module upgrade");

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
