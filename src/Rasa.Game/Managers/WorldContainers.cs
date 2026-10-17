using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Loot;
    using Packets.LootDispenser.Client;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Lootable containers the server puts on maps: rows of world_container, each a class of the
    /// client's with the TREASUREDISPENSER augmentation (64) - a crate, a locker, a console, a
    /// corpse, a cocooned body, a reliquary. The client's treasure dispenser is a usable and a
    /// lootable at once (treasuredispenser.py): used while closed (USE_TD_STATE_CLOSED, 200), it
    /// opens (USE_TD_STATE_OPENED, 201) and what is in it is looted through the corpse window
    /// (lootable.py, a dispenser attached to it). It can be ciphered only while closed. No .map
    /// places one, so each is a row.
    ///
    /// What is in it is drawn when it is opened: its fixed items (world_container_item), always,
    /// as many stacks as their quantity takes, and a roll of its loot profile on top (a
    /// loot_profile of the ordinary loot tables, the same rolls creature loot makes), if it names
    /// one.
    ///
    /// Each player has their own copy of every container, and a squad shares one: the first use
    /// by any of them opens it for those the squad's loot method shares a corpse with
    /// (PartyManager.LootersFor: the whole squad in range under Free For All, the next in turn
    /// under Rotation, the opener alone under Individual), and they loot it as they would a
    /// corpse - the items at or over the squad's threshold rolled for (LootRolls). To everyone it
    /// was opened for it shows opened, and stays so until <see cref="Placement.ResetMs"/> after the
    /// opening, when it is closed and full for them again; to everyone else it is closed. A player
    /// it was opened for cannot open it again before then, in another squad or alone.
    ///
    /// A row with a cipher level is locked while closed, to each player until they or a squad
    /// mate on the map decode it with a cipher tool (ToolActionManager); each player's lock takes
    /// <see cref="Cipher.AttemptsPerLock"/> attempts, and is locked again with the reset.
    ///
    /// Nothing of this is kept over a restart: every container is closed and full when the
    /// server comes up.
    /// </summary>
    public static class WorldContainers
    {
        /// <summary>How long a container stays opened to those who opened it, when its row says 0: half an hour.</summary>
        public const uint DefaultResetSeconds = 1800;

        /// <summary>The windup of a use: the client's own default for the family, which has no usabledata row.</summary>
        public const int UseWindupMs = 100;

        public sealed class Placement
        {
            public uint Id { get; init; }
            public uint MapContextId { get; init; }
            public EntityClasses ClassId { get; init; }
            public Vector3 Position { get; init; }
            public double Rotation { get; init; }
            public string LootProfileKey { get; init; }
            public int CipherLevel { get; init; }
            public long ResetMs { get; init; }
            public IReadOnlyList<(uint TemplateId, uint Quantity)> Items { get; init; } = Array.Empty<(uint, uint)>();
            public string Comment { get; init; }

            /// <summary>Put down by a GM (.worldcontainer put): no row, and gone with its channel.</summary>
            public bool IsPutDown { get; init; }

            public override string ToString() => IsPutDown ? $"container put down ({(uint)ClassId})" : $"world_container {Id} ({(uint)ClassId})";
        }

        /// <summary>One opening of a container: who it was opened for, what they found, and when it is closed to them again.</summary>
        public sealed class Opening
        {
            public HashSet<uint> Characters { get; } = new HashSet<uint>();
            public LootDispenser Loot { get; internal set; }
            public long ResetAt { get; internal set; }
        }

        public sealed class Live
        {
            internal Live(Placement placement, MapChannel map)
            {
                Placement = placement;
                Map = map;
            }

            public Placement Placement { get; }
            public MapChannel Map { get; }
            public DynamicObject Object { get; internal set; }

            /// <summary>The opening each character is part of, until its reset.</summary>
            public Dictionary<uint, Opening> Openings { get; } = new Dictionary<uint, Opening>();

            /// <summary>Each character's lock, for a container with a cipher level; made on first need.</summary>
            public Dictionary<uint, UsableLock> Locks { get; } = new Dictionary<uint, UsableLock>();
        }

        private static List<Placement> _placements = new List<Placement>();
        private static readonly ConditionalWeakTable<MapChannel, List<Live>> Channels = new();

        public static IReadOnlyList<Placement> All => _placements;

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A roll in [min, max), as the loot rolls take it; a test's to replace.</summary>
        internal static Func<int, int, int> Roll { get; set; } = Random.Shared.Next;

        /// <summary>The loot profile of a key, or null; a test's to replace.</summary>
        internal static Func<string, OrdinaryLootProfile> ProfileOf { get; set; } = key => OrdinaryLootCatalog.Current.Profile(key);

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Roll = Random.Shared.Next;
            ProfileOf = key => OrdinaryLootCatalog.Current.Profile(key);
        }

        #region Loading and placing

        /// <summary>Loads world_container and world_container_item and puts each container on its map. Runs after MapChannelInit and the loot catalog.</summary>
        public static void Init(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();

            var rows = unitOfWork.WorldUsables.GetContainers();
            var kept = Load(rows, unitOfWork.WorldUsables.GetContainerItems());
            var placed = 0;

            foreach (var mapContextId in _placements.Select(p => p.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += Place(mapChannel);
                else
                    Logger.WriteLog(LogType.Initialize, $"  world containers: map {mapContextId} is not loaded");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {kept} of {rows.Count} world containers, {placed} in the world");
        }

        /// <summary>
        /// Takes the rows as what is placed from here on. One whose class is not loaded or is not a
        /// treasure dispenser, whose loot profile is not in the loot tables, or one of whose items
        /// is no item, is logged and left out. Returns how many were kept.
        /// </summary>
        public static int Load(IEnumerable<WorldContainerEntry> rows, IEnumerable<WorldContainerItemEntry> items)
        {
            var itemsOf = (items ?? Enumerable.Empty<WorldContainerItemEntry>()).ToLookup(item => item.ContainerId);
            var placements = new List<Placement>();

            foreach (var row in rows ?? Enumerable.Empty<WorldContainerEntry>())
            {
                var placement = PlacementOf(row, itemsOf[row.Id], out var why);

                if (placement == null)
                {
                    Logger.WriteLog(LogType.Error, $"world_container {row.Id}: {why}; left out");
                    continue;
                }

                placements.Add(placement);
            }

            _placements = placements;

            return placements.Count;
        }

        /// <summary>What a row places, or null, with why, if it cannot be.</summary>
        public static Placement PlacementOf(WorldContainerEntry row, IEnumerable<WorldContainerItemEntry> items, out string why, bool isPutDown = false)
        {
            why = null;

            if (row == null)
            {
                why = "no row";
                return null;
            }

            why = WhyNot((EntityClasses)row.ClassId);

            if (why != null)
                return null;

            var profileKey = string.IsNullOrWhiteSpace(row.LootProfileKey) ? null : row.LootProfileKey;

            if (profileKey != null && ProfileOf(profileKey) == null)
            {
                why = $"loot profile {profileKey} is not in the loot tables";
                return null;
            }

            var fixedItems = new List<(uint, uint)>();

            foreach (var item in items ?? Enumerable.Empty<WorldContainerItemEntry>())
            {
                if (item.Quantity == 0 || StackSizeOf(item.ItemTemplateId) == 0)
                {
                    why = $"item {item.ItemTemplateId} x{item.Quantity} is no item it can hold";
                    return null;
                }

                fixedItems.Add((item.ItemTemplateId, item.Quantity));
            }

            if (!float.IsFinite(row.Position.X) || !float.IsFinite(row.Position.Y) || !float.IsFinite(row.Position.Z) ||
                !CellManager.TryGetCellCoordinates(row.Position, out _, out _))
            {
                why = "its position is off any map";
                return null;
            }

            if (row.CipherLevel > 50)
            {
                why = $"cipher level {row.CipherLevel} is over 50, the highest Specialist Tools goes";
                return null;
            }

            return new Placement
            {
                Id = row.Id,
                MapContextId = row.MapContextId,
                ClassId = (EntityClasses)row.ClassId,
                Position = row.Position,
                Rotation = double.IsFinite(row.Rotation) ? row.Rotation : 0,
                LootProfileKey = profileKey,
                CipherLevel = (int)row.CipherLevel,
                ResetMs = (row.ResetSeconds == 0 ? DefaultResetSeconds : row.ResetSeconds) * 1000L,
                Items = fixedItems,
                Comment = row.Comment,
                IsPutDown = isPutDown
            };
        }

        /// <summary>Why objects of the class cannot be placed as containers, or null if they can.</summary>
        public static string WhyNot(EntityClasses classId)
        {
            var classInfo = EntityClassManager.Instance.GetClassInfo(classId);

            if (classInfo == null)
                return $"class {(uint)classId} is not loaded";

            if (classInfo.Augmentations == null || !classInfo.Augmentations.Contains(AugmentationType.TreasureDispenser))
                return $"class {(uint)classId} ({classInfo.ClassName}) is no treasure dispenser";

            return null;
        }

        /// <summary>How many of an item template make a stack, or 0 if it is no item.</summary>
        public static uint StackSizeOf(uint itemTemplateId)
        {
            var template = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            var itemClass = template == null ? null : EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo;

            return itemClass == null ? 0 : (uint)Math.Max(1, itemClass.StackSize);
        }

        /// <summary>
        /// Puts this map's containers on one channel of it - the open world's or a private copy -
        /// and tells whoever is in range. One the channel already has is left alone. Returns how
        /// many were added.
        /// </summary>
        public static int Place(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null)
                return 0;

            var lives = Channels.GetOrCreateValue(mapChannel);
            var placed = 0;

            foreach (var placement in _placements)
            {
                if (placement.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                lock (lives)
                    if (lives.Any(live => live.Placement == placement))
                        continue;

                Build(mapChannel, placement);
                placed++;
            }

            return placed;
        }

        /// <summary>Puts one down on a map channel that is no row and is gone when the channel is (.worldcontainer put). Null, with why, if it cannot be.</summary>
        public static Live PutDown(MapChannel mapChannel, WorldContainerEntry row, IEnumerable<WorldContainerItemEntry> items, out string why)
        {
            why = "there is no map here";

            if (mapChannel?.MapInfo == null || row == null)
                return null;

            row.MapContextId = mapChannel.MapInfo.MapContextId;
            row.Comment = "put down by a GM";

            var placement = PlacementOf(row, items, out why, isPutDown: true);

            return placement == null ? null : Build(mapChannel, placement);
        }

        /// <summary>Takes away what GMs put down on a map channel; the rows' stay. Returns how many went.</summary>
        public static int TakeAwayPutDown(MapChannel mapChannel)
        {
            var putDown = On(mapChannel).Where(live => live.Placement.IsPutDown).ToList();

            foreach (var live in putDown)
                TakeAway(live);

            return putDown.Count;
        }

        private static Live Build(MapChannel map, Placement placement)
        {
            var live = new Live(placement, map);

            live.Object = new DynamicObject
            {
                EntityClassId = placement.ClassId,
                DynamicObjectType = DynamicObjectType.Container,
                Position = placement.Position,
                Rotation = placement.Rotation,
                MapContextId = placement.MapContextId,
                TargetCategory = TargetCategory.Object,
                StateId = UseObjectState.TdStateClosed,
                IsEnabled = true,
                IsInWorld = true,
                ObjectData = live,
                WindupTime = UseWindupMs,
                Comment = placement.ToString()
            };

            CellManager.Instance.AddToWorld(map, live.Object);

            var lives = Channels.GetOrCreateValue(map);

            lock (lives)
                lives.Add(live);

            return live;
        }

        private static void TakeAway(Live live)
        {
            if (Channels.TryGetValue(live.Map, out var lives))
                lock (lives)
                    lives.Remove(live);

            foreach (var opening in OpeningsOf(live))
                if (opening.Loot != null)
                    LootDispenserManager.Instance.RemoveContainerLoot(live.Map, opening.Loot);

            lock (live)
            {
                live.Openings.Clear();
                live.Locks.Clear();
            }

            live.Object.IsInWorld = false;
            CellManager.Instance.RemoveFromWorld(live.Map, live.Object);
        }

        /// <summary>Everything placed on a map channel.</summary>
        public static List<Live> On(MapChannel mapChannel)
        {
            if (mapChannel == null || !Channels.TryGetValue(mapChannel, out var lives))
                return new List<Live>();

            lock (lives)
                return lives.ToList();
        }

        /// <summary>What stands for a row on a map channel, or null.</summary>
        public static Live LiveOf(MapChannel mapChannel, uint rowId) =>
            On(mapChannel).FirstOrDefault(live => !live.Placement.IsPutDown && live.Placement.Id == rowId);

        /// <summary>The container an object stands for, or null.</summary>
        public static Live LiveOf(DynamicObject obj) => obj?.DynamicObjectType == DynamicObjectType.Container ? obj.ObjectData as Live : null;

        /// <summary>A map channel going away (MapChannelManager.CleanupPrivateMapChannel): everything placed on it taken away. Returns how many went.</summary>
        public static int Forget(MapChannel mapChannel)
        {
            var all = On(mapChannel);

            foreach (var live in all)
                TakeAway(live);

            if (mapChannel != null)
                Channels.Remove(mapChannel);

            return all.Count;
        }

        private static List<Opening> OpeningsOf(Live live)
        {
            lock (live)
                return live.Openings.Values.Distinct().ToList();
        }

        #endregion

        #region What each player sees

        /// <summary>Whether it is opened to this character: they were among those it was opened for, and it has not reset.</summary>
        public static bool IsOpenedTo(Live live, uint characterId)
        {
            lock (live)
                return live.Openings.ContainsKey(characterId);
        }

        /// <summary>The state it shows this character.</summary>
        public static UseObjectState StateFor(Live live, uint characterId) =>
            IsOpenedTo(live, characterId) ? UseObjectState.TdStateOpened : UseObjectState.TdStateClosed;

        /// <summary>This character's lock on a container with a cipher level, made on first need; null for one without.</summary>
        public static UsableLock LockFor(Live live, uint characterId)
        {
            if (live?.Placement.CipherLevel is not > 0)
                return null;

            lock (live)
            {
                if (!live.Locks.TryGetValue(characterId, out var usableLock))
                    live.Locks[characterId] = usableLock = new UsableLock
                    {
                        LockStateId = UseObjectState.TdStateClosed,
                        CipherLevel = live.Placement.CipherLevel
                    };

                return usableLock;
            }
        }

        /// <summary>The lock a client is to be told of for an object: its own, for a container; the object's for anything else.</summary>
        public static UsableLock LockFor(Client client, DynamicObject obj)
        {
            var live = LiveOf(obj);

            return live == null ? obj?.Lock : LockFor(live, client?.Player?.Id ?? 0);
        }

        /// <summary>
        /// A container coming into a client's view (DynamicObjectManager.CreateDynamicObjectOnClient):
        /// the state it is in for them, their lock on it, and - if they are still to loot what it
        /// was opened with - its dispenser.
        /// </summary>
        public static void Introduce(Client client, DynamicObject obj)
        {
            var live = LiveOf(obj);
            var player = client?.Player;

            if (live == null || player == null)
                return;

            Opening opening;

            lock (live)
                live.Openings.TryGetValue(player.Id, out opening);

            if (opening != null)
                client.CallMethod(obj.EntityId, new ForceStatePacket(UseObjectState.TdStateOpened, 0));

            if (LockFor(live, player.Id) is { } usableLock)
                client.CallMethod(obj.EntityId, new LockInfoPacket(usableLock));

            var loot = opening?.Loot;

            if (loot != null && loot.HasLoot && !loot.FullyLooted &&
                (loot.Owner == player.EntityId || loot.Looters.Contains(player.EntityId)) &&
                ReferenceEquals(live.Map, player.MapChannel) &&
                live.Map.LootDispensers.TryGetValue(loot.EntityId, out var current) && ReferenceEquals(current, loot))
                LootDispenserManager.Instance.ShowContainerLoot(client, loot);
        }

        /// <summary>The clients that can see the container, of these characters.</summary>
        private static List<Client> Watching(Live live, ICollection<uint> characterIds)
        {
            if (!CellManager.TryGetCellCoordinates(live.Object.Position, out var x, out var z))
                return new List<Client>();

            return CellManager.Instance.GetClientsInCells(live.Map, CellManager.Instance.CreateCellMatrix(live.Map, x, z))
                .Where(client => client?.Player != null && characterIds.Contains(client.Player.Id))
                .ToList();
        }

        #endregion

        #region Opening

        /// <summary>
        /// RequestUseObject for one of these (DynamicObjectManager): refused as the client would put
        /// it, or wound up, the container to open at the recovery (<see cref="UseRecovery"/>). One
        /// already opened to the player shows its window again, if they still have something in it
        /// to take. Returns whether it was taken.
        /// </summary>
        public static bool RequestUse(Client client, DynamicObject obj, RequestUseObjectPacket packet)
        {
            var player = client?.Player;
            var map = player?.MapChannel;
            var live = LiveOf(obj);

            if (live == null || player == null || map == null)
                return false;

            void Refuse(PlayerMessage? message) => ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, message);

            if (!ReferenceEquals(live.Map, map))
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} asked to use {live.Placement}, which is not on their map channel. Ignored.");
                return false;
            }

            if (packet.ActionId != ActionId.UseObject)
            {
                Refuse(PlayerMessage.PmUseObjectNotUsable);
                return false;
            }

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
            {
                Refuse(PlayerMessage.PmCannotPerformActionNow);
                return false;
            }

            if (Vector3.Distance(player.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
            {
                Refuse(PlayerMessage.PmTargetOutOfRange);
                return false;
            }

            Opening opening;

            lock (live)
                live.Openings.TryGetValue(player.Id, out opening);

            // Opened to them already: the window again, if there is anything in it for them; the
            // request itself is closed without a word.
            if (opening != null)
            {
                Refuse(null);

                if (opening.Loot != null)
                    LootDispenserManager.Instance.RequestCorpseLooting(client, new RequestCorpseLootingPacket { EntityId = opening.Loot.EntityId });

                return false;
            }

            var refusal = RefusalOf(live, player.Id);

            if (refusal != null)
            {
                Refuse(refusal.Value);
                return false;
            }

            if (map.PerformRecovery.Any(action => action.ActionId == ActionId.UseObject && action.Actor == player))
            {
                Refuse(PlayerMessage.PmCannotPerformActionNow);
                return false;
            }

            client.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
            map.PerformRecovery.Add(new ActionData(player, packet.ActionId, packet.ActionArgId, UseWindupMs) { SourceId = obj.EntityId });

            return true;
        }

        /// <summary>Why this character cannot open it, or null if they can.</summary>
        private static PlayerMessage? RefusalOf(Live live, uint characterId)
        {
            if (IsOpenedTo(live, characterId))
                return PlayerMessage.PmUseObjectNotUsable;

            if (LockFor(live, characterId) is { } usableLock && usableLock.IsLockedIn(UseObjectState.TdStateClosed))
                return PlayerMessage.PmUseObjectUsableLocked;

            return null;
        }

        /// <summary>Whether a use-object action is a use of one of these.</summary>
        public static bool IsUseOf(ActionData action) =>
            action != null && action.ActionId == ActionId.UseObject &&
            EntityManager.Instance.TryGetObject(action.SourceId, out var obj) && LiveOf(obj) != null;

        /// <summary>
        /// The recovery of a use: unless it was interrupted, or its user died or left the map in the
        /// meantime, or it can no longer be opened by them, it opens (<see cref="Open"/>). Returns
        /// the opening, or null.
        /// </summary>
        public static Opening UseRecovery(MapChannel map, ActionData action)
        {
            if (action == null || action.IsInrerrupted || !EntityManager.Instance.TryGetObject(action.SourceId, out var obj) ||
                LiveOf(obj) is not { } live || !ReferenceEquals(live.Map, map))
                return null;

            if (action.Actor is not Manifestation user || user.State == CharacterState.Dead || user.MapChannel != map)
                return null;

            var client = map.ClientList.FirstOrDefault(candidate => ReferenceEquals(candidate?.Player, user));

            if (client == null || RefusalOf(live, user.Id) != null ||
                Vector3.Distance(user.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
                return null;

            return Open(client, live);
        }

        /// <summary>
        /// The container opened by this player: for those their squad's loot method shares it with
        /// (less any it is opened to already), with its fixed items and a roll of its profile;
        /// opened to all of them, and its window open for the opener. Null if its loot could not
        /// be made, in which case it is not opened.
        /// </summary>
        public static Opening Open(Client client, Live live)
        {
            var (looters, partyId, party, eligible) = PartyManager.Instance.LootersFor(client, live.Object.Position);

            lock (live)
            {
                bool Free(Client member) => member == client || !live.Openings.ContainsKey(member.Player.Id);

                looters = looters.Where(Free).ToList();
                eligible = eligible.Where(Free).ToList();
            }

            // Rotation can hand it to a squad mate; the opener is among those it is opened for in
            // any case, and shares it if it was handed to them.
            if (looters.Count == 0)
                looters.Add(client);

            var loot = LootDispenserManager.Instance.LootContainer(client, live.Object, Drops(live.Placement), looters, partyId, party, eligible);

            if (loot == null)
                return null;

            var opening = new Opening { Loot = loot, ResetAt = Now() + live.Placement.ResetMs };

            opening.Characters.Add(client.Player.Id);

            foreach (var member in looters.Concat(eligible))
                opening.Characters.Add(member.Player.Id);

            foreach (var looterId in loot.Looters)
                if (EntityManager.Instance.Players.TryGetValue(looterId, out var looter))
                    opening.Characters.Add(looter.Id);

            lock (live)
                foreach (var characterId in opening.Characters)
                    live.Openings[characterId] = opening;

            foreach (var watcher in Watching(live, opening.Characters))
                watcher.CallMethod(live.Object.EntityId, new ForceStatePacket(UseObjectState.TdStateOpened, 0));

            if (loot.Owner == client.Player.EntityId || loot.Looters.Contains(client.Player.EntityId))
                LootDispenserManager.Instance.RequestCorpseLooting(client, new RequestCorpseLootingPacket { EntityId = loot.EntityId });

            return opening;
        }

        /// <summary>
        /// What one opening of it holds: each fixed item, as stacks of as many as its stack takes,
        /// and a roll of its loot profile.
        /// </summary>
        public static List<(uint TemplateId, uint Quantity)> Drops(Placement placement)
        {
            var drops = new List<(uint, uint)>();

            foreach (var (templateId, quantity) in placement.Items)
            {
                var stack = StackSizeOf(templateId);

                if (stack == 0)
                    continue;

                for (var left = quantity; left > 0; left -= Math.Min(left, stack))
                    drops.Add((templateId, Math.Min(left, stack)));
            }

            if (placement.LootProfileKey != null && ProfileOf(placement.LootProfileKey) is { } profile)
                drops.AddRange(profile.Roll(Roll));

            return drops;
        }

        #endregion

        #region Locks

        /// <summary>
        /// A cipher has decoded a container's lock for this player (ToolActionManager): it is
        /// unlocked to them and to their squad mates on the map, each of whom is told.
        /// </summary>
        public static void Unlocked(Client client, Live live)
        {
            var map = live.Map;
            var party = PartyManager.Instance.PartyOf(client);
            var members = new List<Client> { client };

            if (party != null)
                members.AddRange(map.ClientList.Where(other => other != client && other?.Player != null &&
                    ReferenceEquals(PartyManager.Instance.PartyOf(other), party)));

            var unlocked = new HashSet<uint>();

            foreach (var member in members)
                if (LockFor(live, member.Player.Id) is { } usableLock)
                {
                    usableLock.Unlocked = true;
                    unlocked.Add(member.Player.Id);
                }

            foreach (var watcher in Watching(live, unlocked))
                watcher.CallMethod(live.Object.EntityId, new LockInfoPacket(LockFor(live, watcher.Player.Id)));
        }

        #endregion

        #region Resets

        /// <summary>Every tick: the openings that are due to reset, closed and full again for those they were opened for.</summary>
        public static void Worker(MapChannel map)
        {
            var lives = On(map);

            if (lives.Count == 0)
                return;

            var now = Now();

            foreach (var live in lives)
                foreach (var opening in OpeningsOf(live))
                    if (now >= opening.ResetAt)
                        Close(live, opening);
        }

        /// <summary>Closes it again for everyone, now (.worldcontainer reset). Returns how many openings there were.</summary>
        public static int CloseAll(Live live)
        {
            var openings = OpeningsOf(live);

            foreach (var opening in openings)
                Close(live, opening);

            return openings.Count;
        }

        /// <summary>One opening reset: its dispenser and whatever nobody took gone, and the container closed, full and locked again to those it was opened for.</summary>
        private static void Close(Live live, Opening opening)
        {
            lock (live)
                foreach (var characterId in opening.Characters)
                {
                    if (live.Openings.TryGetValue(characterId, out var current) && current == opening)
                        live.Openings.Remove(characterId);

                    live.Locks.Remove(characterId);
                }

            if (opening.Loot != null)
                LootDispenserManager.Instance.RemoveContainerLoot(live.Map, opening.Loot);

            foreach (var watcher in Watching(live, opening.Characters))
            {
                watcher.CallMethod(live.Object.EntityId, new ForceStatePacket(UseObjectState.TdStateClosed, 0));

                if (LockFor(live, watcher.Player.Id) is { } usableLock)
                    watcher.CallMethod(live.Object.EntityId, new LockInfoPacket(usableLock));
            }
        }

        #endregion
    }
}
