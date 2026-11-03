using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Packets.MapChannel.Server;
    using Rasa.Missions.Scenes;
    using Structures;
    using Structures.World;

    using C = Services.Preloader.CraterLakeContent;

    /// <summary>
    /// What Crater Lake Research Facility (1721) does that its rows do not: each copy of it - a
    /// squad's instance, or the map's own channel - runs its story for whoever is in it
    /// (CraterLakeContent has the people, the Bane and the missions). The world is the copy's,
    /// one for everyone in it, and an event is everyone's in the copy who has its objective open.
    ///
    ///  - We Be Jammin': the large transmitter in the clearing on the ridge, destroyed, is the
    ///    first objective; the GPS beacon's place by the radar dish takes the beacon from a member
    ///    on the second while the transmitter is down (refused before, as the client puts it
    ///    without the beacon: PmMissingReqItem).
    ///  - Destroying the Evidence: the three Corman terminals and the Bane equipment are object
    ///    conversations (MissionObjectConversations), each its objective.
    ///  - Centrifuge Fuel: a storage crate gives a member on it a crystal while they hold fewer
    ///    than five, and is empty <see cref="CrateEmptyMinMs"/> to <see cref="CrateEmptyMaxMs"/>.
    ///  - Reclaiming What's Ours: the weapons caches on the fortification's platform hand a member
    ///    up to <see cref="CacheDetonators"/> detonators while a generator stands. A detonator set
    ///    in the bomb at a generator (PlacedUsables) blows it when it goes off. The fortification's
    ///    generator down takes the force fields in the stronghold's west and north ways in down
    ///    for the copy, which is the generator's objective. The Overseer in the courtyard: his
    ///    shield up, he is immune to everything but Sonic and Lightning (Electrical); it fails when
    ///    they have taken <see cref="ShieldBreakFraction"/> of his health, or after
    ///    <see cref="ShieldUpMs"/>, for <see cref="ShieldDownMs"/>. While he fights, drop pods put
    ///    Hunters down in the courtyard's middle, and after he is dead they still come at whoever
    ///    is in the courtyard until the pen is open. His death is the next objective for the copy.
    ///    The pen's panel answers once he is dead and the courtyard's own generator is down; its
    ///    objective done by a member, the pen's field goes down for the copy.
    ///  - Daniel Corman in the pen: he follows the nearest member on Return to Sender or Bending
    ///    the Rules who comes within <see cref="EscortPickupRadius"/>, is put beside them when they
    ///    are more than <see cref="WarpDistance"/> away or where he cannot walk to them, and takes
    ///    no harm. Within <see cref="DeliverRadius"/> of Velns he is delivered, within
    ///    <see cref="ExitRadius"/> of the door out he is out, for everyone on that objective; and
    ///    he stays there.
    ///  - The Bane teleporters on the trail and on the crater floor beam a Hunter or a Predator
    ///    in every <see cref="TeleporterEveryMs"/> while a member is within
    ///    <see cref="TeleporterRange"/>, no more than <see cref="TeleporterLimit"/> of their own
    ///    about at once; destroyed, they send none.
    ///  - A member outside who stays within <see cref="LingerRadius"/> of one place for
    ///    <see cref="LingerMs"/> has a wave beamed in behind them, no more often than
    ///    <see cref="WaveCooldownMs"/> in the copy. The staging post's cave and the courtyard
    ///    are not outside.
    /// What is destroyed stays down while the copy stands. Members who come later are given what
    /// the copy has done. A copy that is closed and opened again starts over.
    /// </summary>
    public static class CraterLakeFacility
    {
        public const int UseWindupMs = 2_000;

        public const long CrateEmptyMinMs = 60_000, CrateEmptyMaxMs = 120_000;
        public const uint CacheDetonators = 2;

        public const long ShieldUpMs = 20_000, ShieldDownMs = 6_000;
        public const float ShieldBreakFraction = 0.05f;
        public const float OverseerShout = 80f;

        public const long DropEveryMs = 15_000, DropAfterEveryMs = 25_000;
        public const int DropCap = 6, DropAfterCap = 4, DropSize = 2;

        public const float TeleporterRange = 60f;
        public const long TeleporterEveryMs = 40_000;
        public const int TeleporterLimit = 2;

        public const float LingerRadius = 15f;
        public const long LingerMs = 60_000, WaveCooldownMs = 180_000;
        public const int WaveSize = 3;
        public const float WaveDistance = 20f;

        public const float EscortPickupRadius = 15f, DeliverRadius = 8f, ExitRadius = 12f, WarpDistance = 40f;

        public const long RetryMs = 10_000;

        /// <summary>The copy's people, whose missions they are: they take no harm.</summary>
        public static readonly uint[] StoryNpcs = { C.Velns, C.Daniel };

        /// <summary>The damage the Overseer's shield lets through.</summary>
        public static readonly DamageType[] ShieldPasses = { DamageType.Sonic, DamageType.Electrical };

        public enum Kind { BeaconSite, Cache, Crate }

        /// <summary>One of the copy's usables: what it is, and which.</summary>
        internal sealed class Usable
        {
            public Kind Kind;
            public int Index;
            public DynamicObject Object;
        }

        /// <summary>One copy of the facility, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public PlacedDestructibles.Live WestField, NorthField, PenField, OuterGenerator, InnerGenerator, Jammer;
            public readonly List<PlacedDestructibles.Live> Teleporters = new();
            public PlacedUsables.Live OuterBomb, InnerBomb;
            public readonly List<Usable> Usables = new();
            public readonly List<DynamicObject> Conversations = new();
            public DynamicObject Panel;

            public bool BaseOpen, PenOpen;
            public long[] CrateBackAt = new long[C.Crates.Length];

            public Creature Overseer;
            public bool OverseerDown, ShieldUp;
            public long ShieldChangeAt;
            public int ShieldHealth;

            public readonly List<Creature> Drops = new();
            public long NextDropAt;

            public readonly List<Creature>[] Beamed = { new List<Creature>(), new List<Creature>() };
            public readonly long[] NextBeamAt = new long[2];
            public readonly int[] BeamCount = new int[2];

            public readonly Dictionary<uint, (Vector3 At, long Since)> Lingering = new();
            public long NextWaveAt;

            public Creature Daniel;
            public ulong DanielLead;

            /// <summary>Events already given, by player, assignment and event: given once; one that changed nothing is tried again later.</summary>
            public readonly Dictionary<string, long> Given = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A random number from the first to the second, both in; a test's to replace.</summary>
        public static Func<long, long, long> Roll { get; set; } = (min, max) => Random.Shared.NextInt64(min, max + 1);

        /// <summary>Whether a map channel is a copy of the facility.</summary>
        public static bool IsFacility(MapChannel map) => map?.MapInfo?.MapContextId == C.CraterLake;

        internal static State StateOf(MapChannel map) => IsFacility(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsFacility(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            ProtectStoryNpcs(map);
            Bombs(map, state);
            KeepDown(state);
            Crates(map, state, now);

            var members = Members(map);

            Overseer(map, state, members, now);
            Panel(map, state);

            if (members.Count == 0)
                return;

            WeBeJammin(state, members, now);
            Reclaiming(map, state, members, now);
            Drops(map, state, members, now);
            Teleporters(map, state, members, now);
            Lingering(map, state, members, now);
            Daniel(map, state, members, now);
        }

        #region Set up

        /// <summary>The copy's fields, generators, transmitter, teleporters, bombs and usables, and the Overseer and Daniel, put down once.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            state.WestField = Field(map, C.WestGate, C.WestGateYaw);
            state.NorthField = Field(map, C.NorthGate, C.NorthGateYaw);
            state.PenField = Field(map, C.PenGate, C.PenGateYaw);

            state.OuterGenerator = PlacedDestructibles.PutDown(map, (EntityClasses)C.GeneratorClass, Ground(map, C.OuterGenerator), 0, C.GeneratorHitPoints);
            state.InnerGenerator = PlacedDestructibles.PutDown(map, (EntityClasses)C.GeneratorClass, Ground(map, C.InnerGenerator), 0, C.GeneratorHitPoints);
            state.Jammer = PlacedDestructibles.PutDown(map, (EntityClasses)C.JammerClass, Ground(map, C.Jammer), 0, C.JammerHitPoints);

            foreach (var spot in C.Teleporters)
                state.Teleporters.Add(PlacedDestructibles.PutDown(map, (EntityClasses)C.TeleporterClass, Ground(map, spot), 0, C.TeleporterHitPoints));

            state.OuterBomb = Bomb(map, C.OuterBomb, "Crater Lake: the bomb at the fortification's generator");
            state.InnerBomb = Bomb(map, C.InnerBomb, "Crater Lake: the bomb at the courtyard's generator");

            foreach (var (objective, _, _, classId, spot, _) in C.Terminals)
                state.Conversations.Add(Place(map, $"clrf:evidence-{objective}", classId, Ground(map, spot), 0, UseObjectState.TsState0,
                    obj => obj.MissionConversation = new SceneObjectConversation(C.DestroyingTheEvidence, objective, C.PackageOf(objective), objective)));

            state.Panel = Place(map, "clrf:pen-panel", C.KeyboardClass, Ground(map, C.Panel), 0, UseObjectState.TsState0,
                obj => obj.MissionConversation = new SceneObjectConversation(C.ReclaimingWhatsOurs, C.UsePanel, C.PanelPackage, C.UsePanel));
            SetEnabled(map, state.Panel, false);

            Add(state, Kind.BeaconSite, 0, Place(map, "clrf:beacon-site", C.BeaconSiteClass, Ground(map, C.BeaconSite), 0, UseObjectState.IaStateActive));
            Scenery(map, "clrf:radar-dish", C.RadarDishClass, Ground(map, C.RadarDish), 0.8f);

            for (var i = 0; i < C.Caches.Length; i++)
                Add(state, Kind.Cache, i, Place(map, $"clrf:cache-{i}", C.CacheClass, Ground(map, C.Caches[i]), 1.57, UseObjectState.TdStateClosed));

            for (var i = 0; i < C.Crates.Length; i++)
                Add(state, Kind.Crate, i, Place(map, $"clrf:crate-{i}", C.CrateClass, Ground(map, C.Crates[i]), i * 0.9, UseObjectState.TdStateClosed));

            state.Overseer = Spawn(map, C.Overseer, C.Courtyard, 0f, C.PenGate);
            RaiseShield(state, Now());

            state.Daniel = Spawn(map, C.Daniel, C.DanielInPen, 0f, C.PenGate);

            Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: three fields, two generators, the transmitter, " +
                $"{state.Teleporters.Count(live => live != null)} teleporters, {state.Usables.Count} usables, the Overseer and Daniel put down.");
        }

        /// <summary>A sealed field of the wall's minor entrance at a gate: it stops players and takes nothing from shots.</summary>
        private static PlacedDestructibles.Live Field(MapChannel map, Vector3 gate, float yaw)
        {
            var live = PlacedDestructibles.PutDown(map, (EntityClasses)C.BaseFieldClass, gate, yaw, side: WorldDestructibleEntry.SideBane, @sealed: true);

            if (live?.Field != null)
                live.Field.Sealed = true;

            return live;
        }

        private static PlacedUsables.Live Bomb(MapChannel map, Vector3 at, string comment)
        {
            var ground = Ground(map, at);
            var bomb = PlacedUsables.PutDown(map, new WorldUsableEntry
            {
                ClassId = C.BombClass,
                PosX = ground.X,
                PosY = ground.Y,
                PosZ = ground.Z,
                Usable = 1,
                ItemClassId = C.DetonatorClass,
                ItemCount = 1,
                DamageMin = 20,
                DamageMax = 40,
                Radius = 10,
                Comment = comment
            }, out var why);

            if (bomb == null)
                Logger.WriteLog(LogType.Error, $"Crater Lake {map.InstanceId}: no bomb at {at}: {why}.");

            return bomb;
        }

        private static void Add(State state, Kind kind, int index, DynamicObject obj) =>
            state.Usables.Add(new Usable { Kind = kind, Index = index, Object = obj });

        /// <summary>A usable of the copy, made as <paramref name="init"/> has it before anyone is shown it.</summary>
        private static DynamicObject Place(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial,
            Action<DynamicObject> init = null)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, position, rotation, key, true, UseWindupMs);
            obj.StateId = initial;
            init?.Invoke(obj);
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        /// <summary>A piece the client shows and nobody uses: the radar dish by the beacon's place.</summary>
        private static DynamicObject Scenery(MapChannel map, string key, uint classId, Vector3 position, float yaw)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = new DynamicObject
            {
                EntityClassId = (EntityClasses)classId,
                DynamicObjectType = DynamicObjectType.Scenery,
                Position = position,
                Rotation = yaw,
                MapContextId = C.CraterLake,
                RuntimeMapChannel = map,
                TargetCategory = TargetCategory.Object,
                ScenarioKey = key,
                Comment = "Crater Lake: the radar dish by the GPS beacon's place",
                IsInWorld = true
            };

            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            return obj;
        }

        private static void SetEnabled(MapChannel map, DynamicObject obj, bool enabled)
        {
            if (obj == null || obj.IsEnabled == enabled)
                return;

            DynamicObjectManager.Instance.SetScenarioInteractionEnabled(map, obj, enabled);
        }

        /// <summary>What is destroyed in the facility stays so: it is never due back.</summary>
        private static void KeepDown(State state)
        {
            foreach (var live in state.Teleporters.Concat(new[]
                     {
                         state.WestField, state.NorthField, state.PenField, state.OuterGenerator, state.InnerGenerator, state.Jammer
                     }))
                if (live != null && live.IsDown && live.RespawnAt != long.MaxValue)
                    live.RespawnAt = long.MaxValue;
        }

        /// <summary>The copy's people take no harm, each one as it comes into the world again.</summary>
        private static void ProtectStoryNpcs(MapChannel map)
        {
            foreach (var creature in Creatures(map))
                if (!creature.ImmuneToAllDamage && Array.IndexOf(StoryNpcs, creature.DbId) >= 0)
                    creature.ImmuneToAllDamage = true;
        }

        #endregion

        #region Use

        /// <summary>Whether a finished use is of one of the copy's usables (ActorActionManager.PerformRecovery).</summary>
        public static bool IsUseOf(MapChannel map, ActionData action) =>
            action != null && StateOf(map) is { } state && state.Usables.Any(usable => usable.Object.EntityId == action.SourceId);

        /// <summary>
        /// A use about to start (DynamicObjectManager.RequestUseObjectPacket): the message it is
        /// refused with, or null. The beacon's place wants the beacon and the transmitter down; an
        /// emptied crate is empty.
        /// </summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var map = client?.Player?.MapChannel;
            var state = StateOf(map);
            var usable = UsableOf(state, obj);

            if (usable == null)
                return null;

            switch (usable.Kind)
            {
                case Kind.BeaconSite when InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.BeaconClass, includeMissionItems: true) == 0:
                    return PlayerMessage.PmMissingReqItem;
                case Kind.BeaconSite when JammerStands(state):
                    CommunicatorManager.Instance.SystemMessage(client, "The Bane jammer drowns the beacon out. Bring the transmitter on the ridge down first.");
                    return PlayerMessage.PmUseObjectNotUsable;
                case Kind.Crate when state.CrateBackAt[usable.Index] != 0:
                    return PlayerMessage.PmUseObjectNotUsable;
                default:
                    return null;
            }
        }

        /// <summary>A finished use of one of the copy's usables: what it does.</summary>
        public static void UseRecovery(MapChannel map, ActionData action)
        {
            var state = StateOf(map);
            var usable = state?.Usables.FirstOrDefault(entry => entry.Object.EntityId == action.SourceId);

            if (usable == null)
                return;

            var obj = usable.Object;
            var client = obj.TriggeredByPlayers.FirstOrDefault(c => c.Player == action.Actor);

            if (client == null)
                return;

            obj.TriggeredByPlayers.Remove(client);

            if (action.IsInrerrupted)
                return;

            if (!MapInstanceScope.Share(map, client.Player, obj) ||
                Vector3.Distance(client.Player.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} was no longer at object {obj.EntityId} when the use finished; nothing done.");
                return;
            }

            var now = Now();

            switch (usable.Kind)
            {
                case Kind.BeaconSite:
                    PlaceBeacon(map, state, usable, client, now);
                    break;
                case Kind.Cache:
                    OpenCache(client, state);
                    break;
                case Kind.Crate:
                    OpenCrate(map, state, usable, client, now);
                    break;
            }
        }

        private static Usable UsableOf(State state, DynamicObject obj) =>
            state == null || obj == null ? null : state.Usables.FirstOrDefault(usable => ReferenceEquals(usable.Object, obj));

        private static void Show(MapChannel map, DynamicObject obj, UseObjectState next, ulong actor)
        {
            obj.StateId = next;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actor, next, 0));
        }

        private static void Give(Client client, uint template, uint count)
        {
            if (count == 0)
                return;

            var item = ItemManager.Instance.CreateFromTemplateId(template, count);

            if (item != null)
                InventoryManager.Instance.GrantItemToInventory(client, item);
        }

        #endregion

        #region We Be Jammin'

        /// <summary>Whether the copy's transmitter on the ridge still stands.</summary>
        public static bool JammerStands(MapChannel map) => JammerStands(StateOf(map));

        private static bool JammerStands(State state) => state?.Jammer != null && !state.Jammer.IsDown;

        private static void WeBeJammin(State state, List<Client> members, long now)
        {
            if (JammerStands(state))
                return;

            foreach (var client in members.Where(client => Wants(client.Player, C.WeBeJammin, C.DestroyJammers)))
                Record(state, client, C.WeBeJammin, C.JammerDown, now);
        }

        private static void PlaceBeacon(MapChannel map, State state, Usable usable, Client client, long now)
        {
            if (JammerStands(state) || !Wants(client.Player, C.WeBeJammin, C.PlaceBeacon) ||
                InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.BeaconClass, includeMissionItems: true) == 0)
            {
                CommunicatorManager.Instance.SystemMessage(client, "The beacon's place has no fix to take.");
                return;
            }

            Show(map, usable.Object, UseObjectState.IaStateAccepted, client.Player.EntityId);
            Record(state, client, C.WeBeJammin, C.BeaconPlaced, now);
            Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: {client.Player.FamilyName} placed the GPS beacon.");
        }

        #endregion

        #region Centrifuge Fuel

        private static uint Crystals(Client client) =>
            InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.CrystalClass, includeMissionItems: true);

        private static void OpenCrate(MapChannel map, State state, Usable usable, Client client, long now)
        {
            if (state.CrateBackAt[usable.Index] != 0)
                return;

            if (!Active(client.Player, C.CentrifugeFuel) || Crystals(client) >= C.CrystalsWanted)
            {
                CommunicatorManager.Instance.SystemMessage(client, "Old stores, nothing you need.");
                return;
            }

            Give(client, C.CrystalTemplate, 1);
            state.CrateBackAt[usable.Index] = now + Roll(CrateEmptyMinMs, CrateEmptyMaxMs);
            Show(map, usable.Object, UseObjectState.TdStateOpened, client.Player.EntityId);
            SetEnabled(map, usable.Object, false);
        }

        /// <summary>An emptied crate has its crystal again once its time is up.</summary>
        private static void Crates(MapChannel map, State state, long now)
        {
            foreach (var usable in state.Usables.Where(entry => entry.Kind == Kind.Crate))
            {
                var backAt = state.CrateBackAt[usable.Index];

                if (backAt == 0 || now < backAt)
                    continue;

                state.CrateBackAt[usable.Index] = 0;
                Show(map, usable.Object, UseObjectState.TdStateClosed, 0);
                SetEnabled(map, usable.Object, true);
            }
        }

        /// <summary>Whether a crate of the copy is empty now, for a test or a GM.</summary>
        public static bool CrateEmpty(MapChannel map, int crate) => StateOf(map)?.CrateBackAt[crate] != 0;

        #endregion

        #region Reclaiming What's Ours

        private static uint Detonators(Client client) =>
            InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)C.DetonatorClass);

        private static void OpenCache(Client client, State state)
        {
            var standing = new[] { state.OuterGenerator, state.InnerGenerator }.Count(live => live != null && !live.IsDown);
            var held = Detonators(client);

            if (standing == 0 || held >= CacheDetonators)
            {
                CommunicatorManager.Instance.SystemMessage(client, "There is nothing more in the cache you need.");
                return;
            }

            Give(client, C.DetonatorTemplate, CacheDetonators - held);
            CommunicatorManager.Instance.SystemMessage(client, "You take generator detonators from the Bane weapons cache.");
        }

        /// <summary>A bomb gone off blows the generator it was set at.</summary>
        private static void Bombs(MapChannel map, State state)
        {
            foreach (var (bomb, generator) in new[] { (state.OuterBomb, state.OuterGenerator), (state.InnerBomb, state.InnerGenerator) })
            {
                // Its generator gone, a bomb is no more use: nobody spends a detonator on it.
                if (bomb != null && generator is { IsDown: true } && bomb.Object.IsEnabled && bomb.State != UseObjectState.BombStateArmed)
                    DynamicObjectManager.Instance.SetEnabled(bomb.Object, false);

                if (bomb == null || generator == null || generator.IsDown || bomb.State != UseObjectState.BombStateDetonated ||
                    Vector3.Distance(bomb.Object.Position, generator.Object.Position) > bomb.Placement.Radius)
                    continue;

                var by = bomb.ArmedBy != 0 && EntityManager.Instance.Players.TryGetValue(bomb.ArmedBy, out var player) ? player : null;
                PlacedDestructibles.Destroy(generator, by);
            }

            if (!state.BaseOpen && state.OuterGenerator is { IsDown: true })
                OpenBase(map, state);
        }

        /// <summary>The fortification's generator down: the stronghold's ways in are open.</summary>
        private static void OpenBase(MapChannel map, State state)
        {
            state.BaseOpen = true;

            foreach (var field in new[] { state.WestField, state.NorthField })
                if (field != null && !field.IsDown)
                    PlacedDestructibles.Destroy(field, null);

            KeepDown(state);

            foreach (var member in Members(map))
                CommunicatorManager.Instance.SystemMessage(member, "The generator is down: the force fields of the Bane stronghold fail.");

            Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: the fortification's generator is down; the stronghold is open.");
        }

        /// <summary>Whether the copy's stronghold is open, for a test or a GM.</summary>
        public static bool BaseOpen(MapChannel map) => StateOf(map)?.BaseOpen == true;

        /// <summary>Whether the copy's supply pen is open, for a test or a GM.</summary>
        public static bool PenOpen(MapChannel map) => StateOf(map)?.PenOpen == true;

        /// <summary>The panel answers once the Overseer is dead and the courtyard's generator is down.</summary>
        private static void Panel(MapChannel map, State state) =>
            SetEnabled(map, state.Panel, state.OverseerDown && state.InnerGenerator is { IsDown: true });

        private static void Reclaiming(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.BaseOpen)
                foreach (var client in members.Where(client => Wants(client.Player, C.ReclaimingWhatsOurs, C.BlowGenerator)))
                    Record(state, client, C.ReclaimingWhatsOurs, C.BaseOpen, now);

            if (state.OverseerDown)
                foreach (var client in members.Where(client => Wants(client.Player, C.ReclaimingWhatsOurs, C.KillOverseer)))
                    Record(state, client, C.ReclaimingWhatsOurs, C.OverseerDown, now);

            // The panel used by one of them: the pen is open for the copy.
            if (state.PenOpen || !members.Any(client => Done(client.Player, C.ReclaimingWhatsOurs, C.UsePanel)))
                return;

            state.PenOpen = true;

            if (state.PenField != null && !state.PenField.IsDown)
                PlacedDestructibles.Destroy(state.PenField, null);

            KeepDown(state);
            Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: the supply pen's field is down.");
        }

        #endregion

        #region The Overseer

        /// <summary>The copy's Overseer, alive; null if he is not.</summary>
        public static Creature OverseerOf(MapChannel map) => StateOf(map)?.Overseer is { } overseer && Alive(overseer) ? overseer : null;

        /// <summary>Whether the copy's Overseer has his shield up.</summary>
        public static bool ShieldUp(MapChannel map) => StateOf(map)?.ShieldUp == true;

        private static void Overseer(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.OverseerDown)
                return;

            var overseer = state.Overseer;

            if (overseer == null)
                return;

            if (!Alive(overseer))
            {
                state.OverseerDown = true;
                state.ShieldUp = false;
                Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: the Overseer is dead.");
                return;
            }

            var health = overseer.Attributes[Attributes.Health].Current;
            var max = Math.Max(1, overseer.Attributes[Attributes.Health].NormalMax);

            if (state.ShieldUp)
            {
                var broken = state.ShieldHealth - health >= max * ShieldBreakFraction;

                if (!broken && now < state.ShieldChangeAt)
                    return;

                LowerShield(state, now);
                Shout(map, overseer, broken ? "The Overseer's shield buckles under the blast and fails!" : "The Overseer's shield flickers and fails!");
                return;
            }

            if (now < state.ShieldChangeAt)
                return;

            RaiseShield(state, now);
            Shout(map, overseer, "The Overseer's shield hums back to full strength.");
        }

        private static void RaiseShield(State state, long now)
        {
            var overseer = state.Overseer;

            if (overseer == null)
                return;

            state.ShieldUp = true;
            state.ShieldChangeAt = now + ShieldUpMs;
            state.ShieldHealth = overseer.Attributes[Attributes.Health].Current;

            foreach (var type in Enum.GetValues<DamageType>().Where(type => Array.IndexOf(ShieldPasses, type) < 0))
                overseer.DamageImmunities.Add(type);
        }

        private static void LowerShield(State state, long now)
        {
            state.ShieldUp = false;
            state.ShieldChangeAt = now + ShieldDownMs;
            state.Overseer?.DamageImmunities.Clear();
        }

        private static void Shout(MapChannel map, Creature overseer, string text)
        {
            foreach (var member in Members(map).Where(member => Vector3.Distance(member.Player.Position, overseer.Position) <= OverseerShout))
                CommunicatorManager.Instance.SystemMessage(member, text);
        }

        /// <summary>Whether a position is within the stronghold's walls.</summary>
        public static bool InCourtyard(Vector3 position) =>
            position.X is > -98f and < 8f && position.Z is > -69f and < 41f && position.Y < 85f;

        /// <summary>
        /// The drop pods: Hunters into the courtyard's middle while the Overseer fights, and after
        /// he is dead, at whoever is in the courtyard until the pen is open.
        /// </summary>
        private static void Drops(MapChannel map, State state, List<Client> members, long now)
        {
            state.Drops.RemoveAll(creature => !Alive(creature));

            var fighting = !state.OverseerDown && state.Overseer is { } overseer && Alive(overseer) && overseer.Hate.Ranked().Any();
            var inside = members.Where(client => InCourtyard(client.Player.Position)).ToList();

            if (!fighting && (!state.OverseerDown || state.PenOpen || inside.Count == 0))
                return;

            var cap = fighting ? DropCap : DropAfterCap;

            if (now < state.NextDropAt || state.Drops.Count >= cap)
                return;

            state.NextDropAt = now + (fighting ? DropEveryMs : DropAfterEveryMs);

            var targets = inside.Count > 0 ? inside : members;

            for (var i = 0; i < DropSize && state.Drops.Count < cap; i++)
            {
                var spot = C.DropSpots[(state.Drops.Count + i) % C.DropSpots.Length];

                if (Spawn(map, C.Hunter, spot, 3f, C.Courtyard) is not { } hunter)
                    continue;

                hunter.Hate.Ensure(Nearest(targets, hunter.Position).Player.EntityId, Threat.NoticedThreat);
                state.Drops.Add(hunter);
            }
        }

        #endregion

        #region Beamed in

        /// <summary>Each standing teleporter beams a Bane in at the nearest member within reach.</summary>
        private static void Teleporters(MapChannel map, State state, List<Client> members, long now)
        {
            for (var i = 0; i < state.Teleporters.Count && i < state.Beamed.Length; i++)
            {
                var live = state.Teleporters[i];

                if (live == null || live.IsDown)
                    continue;

                var at = live.Object.Position;
                var near = members.Where(client => Vector3.Distance(client.Player.Position, at) <= TeleporterRange).ToList();

                if (near.Count == 0)
                    continue;

                var sent = state.Beamed[i];
                sent.RemoveAll(creature => !Alive(creature));

                if (now < state.NextBeamAt[i] || sent.Count >= TeleporterLimit)
                    continue;

                state.NextBeamAt[i] = now + TeleporterEveryMs;

                // Every third a Predator, the rest Hunters.
                var row = ++state.BeamCount[i] % 3 == 0 ? C.Predator : C.Hunter;

                if (Spawn(map, row, at, 3f, Nearest(near, at).Player.Position) is not { } bane)
                    continue;

                bane.Hate.Ensure(Nearest(near, bane.Position).Player.EntityId, Threat.NoticedThreat);
                sent.Add(bane);
            }
        }

        /// <summary>Whether a position is in the staging post's cave, where no Bane are beamed in.</summary>
        public static bool InStagingPost(Vector3 position) =>
            position.Y > 130f && Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(30f, 180f)) <= 60f;

        /// <summary>A wave beamed in behind a member who has lingered in one place outside.</summary>
        private static void Lingering(MapChannel map, State state, List<Client> members, long now)
        {
            Client lingerer = null;

            foreach (var client in members)
            {
                var id = client.Player.Id;
                var at = client.Player.Position;

                if (InStagingPost(at) || InCourtyard(at))
                {
                    state.Lingering.Remove(id);
                    continue;
                }

                if (!state.Lingering.TryGetValue(id, out var since) || Vector3.Distance(since.At, at) > LingerRadius)
                {
                    state.Lingering[id] = (at, now);
                    continue;
                }

                if (now - since.Since >= LingerMs && lingerer == null)
                    lingerer = client;
            }

            if (lingerer == null || now < state.NextWaveAt)
                return;

            state.NextWaveAt = now + WaveCooldownMs;

            var player = lingerer.Player;
            var behind = player.Position + new Vector3((float)Math.Sin(player.Rotation), 0f, (float)Math.Cos(player.Rotation)) * WaveDistance;
            var spot = NavMeshManager.RandomPointAround(map, behind, 4f) is { } near && Vector3.Distance(near, behind) <= 6f
                ? near
                : NavMeshManager.RandomPointAround(map, player.Position, WaveDistance) ?? player.Position;
            var beamed = 0;

            for (var i = 0; i < WaveSize; i++)
                if (Spawn(map, i == WaveSize - 1 ? C.Predator : C.Hunter, spot, 3f, player.Position) is { } bane)
                {
                    bane.Hate.Ensure(player.EntityId, Threat.NoticedThreat);
                    beamed++;
                }

            // Moved on, they are not lingering; staying, they are again a while later.
            state.Lingering[player.Id] = (player.Position, now);
            CommunicatorManager.Instance.SystemMessage(lingerer, "Bane signatures beaming in behind you!");
            Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: {beamed} Bane beamed in behind {player.FamilyName}, who lingered at {player.Position}.");
        }

        #endregion

        #region Daniel

        /// <summary>The copy's Daniel, alive; null if he is not.</summary>
        public static Creature DanielOf(MapChannel map) => StateOf(map)?.Daniel is { } daniel && Alive(daniel) ? daniel : null;

        private static bool Escorting(Manifestation player) =>
            Wants(player, C.ReturnToSender, C.EscortToVelns) || Wants(player, C.BendingTheRules, C.EscortOut);

        private static void Daniel(MapChannel map, State state, List<Client> members, long now)
        {
            if (!Alive(state.Daniel))
            {
                state.Daniel = Spawn(map, C.Daniel, C.DanielInPen, 0f, C.PenGate);
                state.DanielLead = 0;
            }

            var daniel = state.Daniel;

            if (daniel == null)
                return;

            if (Vector3.Distance(daniel.Position, C.VelnsSpot) <= DeliverRadius)
                foreach (var client in members.Where(client => Wants(client.Player, C.ReturnToSender, C.EscortToVelns)))
                    Record(state, client, C.ReturnToSender, C.DanielDelivered, now);

            if (Vector3.Distance(daniel.Position, C.Arrival) <= ExitRadius)
                foreach (var client in members.Where(client => Wants(client.Player, C.BendingTheRules, C.EscortOut)))
                    Record(state, client, C.BendingTheRules, C.DanielOut, now);

            var lead = members.FirstOrDefault(client => client.Player.EntityId == state.DanielLead && Escorting(client.Player));

            if (lead == null)
            {
                lead = members.Where(client => Escorting(client.Player) &&
                        Vector3.Distance(client.Player.Position, daniel.Position) <= EscortPickupRadius)
                    .OrderBy(client => Vector3.Distance(client.Player.Position, daniel.Position)).FirstOrDefault();

                if (lead == null)
                {
                    if (state.DanielLead != 0)
                        Stop(map, daniel);

                    state.DanielLead = 0;
                    return;
                }

                state.DanielLead = lead.Player.EntityId;
                daniel.Controller.ScriptedMove = null;
                BehaviorManager.Instance.SetActionFollow(daniel, lead.Player.EntityId);
                CreatureManager.PublishEscortStatus(map, daniel, true);
                Logger.WriteLog(LogType.Debug, $"Crater Lake {map.InstanceId}: Daniel follows {lead.Player.FamilyName}.");
            }

            var to = lead.Player.Position;
            var gap = Vector3.Distance(daniel.Position, to);

            if (gap <= WarpDistance && map.NavMesh?.Connected(daniel.Position, to) != false)
                return;

            // Too far, or where he cannot walk: put beside them.
            var beside = NavMeshManager.RandomPointAround(map, to, 3f) is { } near && Vector3.Distance(near, to) <= 5f ? near : to;
            beside = NavMeshManager.SnapToGround(map, beside);
            var facing = Math.Atan2(to.X - beside.X, to.Z - beside.Z);

            if (BehaviorManager.Instance.RestoreScriptedPose(map, daniel, beside, facing))
            {
                daniel.Controller.ScriptedMove = null;
                BehaviorManager.Instance.SetActionFollow(daniel, lead.Player.EntityId);
            }
        }

        /// <summary>Daniel stands where he is: nobody's to follow.</summary>
        private static void Stop(MapChannel map, Creature daniel)
        {
            daniel.Controller.ActionFollow.FollowTargetId = 0;
            CreatureManager.PublishEscortStatus(map, daniel, false);

            if (BehaviorManager.Instance.RestoreScriptedPose(map, daniel, daniel.Position, daniel.Rotation))
            {
                daniel.Controller.ScriptedMove = null;
                daniel.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            }
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static Client Nearest(List<Client> clients, Vector3 to) =>
            clients.OrderBy(client => Vector3.Distance(client.Player.Position, to)).First();

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

        private static bool Active(Manifestation player, uint missionId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>Whether the player has the mission with the objective done.</summary>
        private static bool Done(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Completed;

        /// <summary>The event to the player's mission: once for an assignment, or again a while after one that changed nothing.</summary>
        private static void Record(State state, Client client, uint missionId, uint eventId, long now)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = $"{client.Player.Id}:{log.AssignmentId}:{missionId}:{eventId}";

            if (state.Given.TryGetValue(key, out var retryAt) && now < retryAt)
                return;

            state.Given[key] = MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, C.Scenario, eventId)
                ? long.MaxValue
                : now + RetryMs;
        }

        /// <summary>The navmesh under a spot, if it is near enough; the spot as it is if not.</summary>
        private static Vector3 Ground(MapChannel map, Vector3 spot) => NavMeshManager.SnapToGround(map, spot);

        /// <summary>A creature of the row, put on the ground near the spot, facing the other.</summary>
        private static Creature Spawn(MapChannel map, uint dbId, Vector3 spot, float spread, Vector3 facingTo)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
                return null;

            var at = spot;

            if (spread > 0 && NavMeshManager.RandomPointAround(map, spot, spread) is { } walkable &&
                Vector2.Distance(new Vector2(walkable.X, walkable.Z), new Vector2(spot.X, spot.Z)) <= spread + 1f)
                at = walkable;

            at = Ground(map, at);

            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        /// <summary>Alive and still in the world as itself.</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
