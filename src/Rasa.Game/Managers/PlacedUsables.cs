using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Usable world objects the server puts on maps: rows of world_usable, put down on their map
    /// when it comes up and on each private copy of it as that is made (as PlacedDestructibles
    /// puts down world_destructible's). The client has hundreds of usable classes with states,
    /// animations and effects that no .map places - switches, doors, item dispensers and
    /// acceptors, random explosions, bombs - which the live servers put down, and whose placements
    /// were not kept. Later migrations put them in (WorldUsablePreloader); a GM can put one down
    /// to look at it (.worldusable).
    ///
    /// What one is comes from its class's usable augmentation, and its states are that
    /// augmentation's (usableaugmentationstate); every change below is a Use, as the client has a
    /// transition for each (usableaugmentationstatetransition), which is what plays a change's
    /// animation and effect:
    ///  - TWOSTATESWITCH (9) and TRIGGEREDTWOSTATE (65): USE_TS_STATE_0 and _1; a use toggles.
    ///  - THREESTATESWITCH (71) and FIVESTATESWITCH (75): a use steps to the next state, the last
    ///    back to the first (the five-state switch has those ways and no others).
    ///  - DOOR (10): USE_DOOR_STATE_CLOSED and _OPEN, the client's collision following (door.py);
    ///    a use opens or shuts it, and a row's reset closes it again that long after it opened.
    ///  - ITEMDISPENSER (34): USE_ID_STATE_ACTIVE, the only state the client lets it be picked out
    ///    in (itemdispenser.py IsMouseTargetable). A use gives the row's item template and takes it
    ///    to USE_ID_STATE_INACTIVE; it is active again after its reset. Mission acquisitions are
    ///    recorded as for any item granted (InventoryManager.GrantItemToInventory).
    ///  - ITEMACCEPTOR (43): USE_IA_STATE_ACTIVE, the only state the client offers its use in
    ///    (itemacceptor.py). A use takes the row's count of items of its entity class from the
    ///    player and goes to USE_IA_STATE_ACCEPTED; active again after its reset. Without the
    ///    items it is refused (PM_MISSING_REQ_ITEM).
    ///  - BOMB (42): USE_BOMB_STATE_DISARMED. A use arms it (taking the row's item, if it names
    ///    one); a use of an armed one disarms it; left armed it goes off after its fuse -
    ///    USE_BOMB_STATE_DETONATED and a blast, OBJ_DAMAGE_BOMB unless the row says otherwise -
    ///    and is disarmed again after its reset. Detonated, the client takes it for no target
    ///    (bomb.py).
    ///  - RANDOMEXPLOSION (48): USE_RE_STATE_0, its one state, whose effect is the fall or the
    ///    build-up (battlefield_falling_bombs.pkg, areki_random_geyser.pkg). It goes off at random
    ///    between the row's shortest and longest wait: Use to the same state, which plays the
    ///    transition's effect for the classes that have one (the reactor's fireballs, the toroid
    ///    cannon), then the blast, OBJ_DAMAGE with its arg (Recv_PerformObjectAbility), which
    ///    takes the state's effect off (randomexplosion.py OnObjectAbility); it is put back
    ///    <see cref="RearmMs"/> later. Its arg is the row's, or by its class's name: lightning,
    ///    geyser, hornito, a falling bomb, a large explosion for a mortar or a cannon, a small one
    ///    otherwise.
    ///  - CONTROLPANEL, ACCESSMODULE, PRESSUREPLATESWITCH, POWERMODULE, POWEREDACCESSMODULE,
    ///    POWERGENERATOR, ENERGYHOPPER, OVERLOADER, ADVENTURELAUNCHER, ADVENTUREEXTRACTOR,
    ///    BENEFACTORDISKPUZZLEPIECE, MECHPAD: shown in their state; nothing more is known of them
    ///    without the missions that drove them, and they are not used.
    /// A class another table places is left out with the name of that table: the destroyables
    /// and stateless switches (world_destructible), control points (control_point), crafting
    /// stations (kraftwerks), wormholes (teleporter), loot dispensers, which the server makes
    /// for loot, the lootable proxies, clan lockboxes and challenge boards.
    ///
    /// A blast reaches every living player within its radius of the object and hurts each,
    /// resisted as its damage type is (Electrical for lightning, Fire for a geyser or a hornito,
    /// Physical otherwise), taken by a shield and armour first, from no actor. A row's numbers
    /// are 0 for the kind's own: <see cref="DefaultDispenserResetSeconds"/> and the rest.
    ///
    /// A player uses one as a footlocker: in reach (DynamicObjectManager.MaxUseDistance), the
    /// windup to them, and at the recovery the change, to everyone in range. One use of an
    /// object at a time. Each map channel has its own; a copy's are its own.
    /// </summary>
    public static class PlacedUsables
    {
        public enum Kind
        {
            TwoState,           // TWOSTATESWITCH 9
            TriggeredTwoState,  // TRIGGEREDTWOSTATE 65
            ThreeState,         // THREESTATESWITCH 71
            FiveState,          // FIVESTATESWITCH 75
            Door,               // DOOR 10
            Dispenser,          // ITEMDISPENSER 34
            Acceptor,           // ITEMACCEPTOR 43
            Bomb,               // BOMB 42
            Explosion,          // RANDOMEXPLOSION 48
            Display             // the rest that can be shown in a state
        }

        /// <summary>How long a use takes, as a footlocker's.</summary>
        public const int UseWindupMs = 100;

        public const uint DefaultDispenserResetSeconds = 60;
        public const uint DefaultAcceptorResetSeconds = 10;
        public const uint DefaultBombFuseSeconds = 5;
        public const uint DefaultBombResetSeconds = 30;
        public const uint DefaultBombDamageMin = 300;
        public const uint DefaultBombDamageMax = 500;
        public const double DefaultBombRadius = 8;
        public const uint DefaultExplosionMinSeconds = 20;
        public const uint DefaultExplosionMaxSeconds = 40;
        public const uint DefaultExplosionDamageMin = 100;
        public const uint DefaultExplosionDamageMax = 200;
        public const double DefaultExplosionRadius = 6;

        /// <summary>How long after a blast a random explosion's state effect is put back.</summary>
        public const long RearmMs = 3000;

        /// <summary>
        /// How late a random explosion may be and still go off. Later than this, its map was not
        /// being worked - the map workers run only while someone is on it - and every explosion on
        /// it that came due meanwhile would go off in the same tick, so it is given a new place in
        /// its cycle instead (<see cref="Worker"/>). Ours: a map that is worked is late by a tick
        /// (Server.MainLoopTime, 100 ms) or by a slow pass, not by seconds.
        /// </summary>
        public const long MissedMs = 5000;

        /// <summary>OBJ_DAMAGE's args (actiondata).</summary>
        public const uint ExplosionSmall = 1, ExplosionLarge = 2, Lightning = 3, Mine = 4, BombBlast = 5, Geyser = 6, Hornito = 7;

        /// <summary>The states of each augmentation this places, as the client lists them (usableaugmentationstate).</summary>
        public static readonly IReadOnlyDictionary<AugmentationType, UseObjectState[]> StatesOf = new Dictionary<AugmentationType, UseObjectState[]>
        {
            [AugmentationType.TwoStateSwitch] = new[] { UseObjectState.TsState0, UseObjectState.TsState1 },
            [AugmentationType.TriggeredTwoState] = new[] { UseObjectState.TsState0, UseObjectState.TsState1 },
            [AugmentationType.ThreeStateSwitch] = new[] { UseObjectState.ThreeStateSwithState0, UseObjectState.ThreeStateSwithState1, UseObjectState.ThreeStateSwithState2 },
            [AugmentationType.FiveStateSwitch] = new[] { UseObjectState.FiveStateSwithState0, UseObjectState.FiveStateSwithState1, UseObjectState.FiveStateSwithState2, UseObjectState.FiveStateSwithState3, UseObjectState.FiveStateSwithState4 },
            [AugmentationType.Door] = new[] { UseObjectState.DoorStateClosed, UseObjectState.DoorStateOpen },
            [AugmentationType.ItemDispenser] = new[] { UseObjectState.IdStateActive, UseObjectState.IdStateInactive },
            [AugmentationType.ItemAcceptor] = new[] { UseObjectState.IaStateActive, UseObjectState.IaStateInactive, UseObjectState.IaStateAccepted },
            [AugmentationType.Bomb] = new[] { UseObjectState.BombStateDisarmed, UseObjectState.BombStateArmed, UseObjectState.BombStateDetonated },
            [AugmentationType.RandomExplosion] = new[] { UseObjectState.ReState0 },
            [AugmentationType.ControlPanel] = new[] { UseObjectState.CpStateClosed, UseObjectState.CpStateOpen },
            [AugmentationType.AccessModule] = new[] { UseObjectState.AmState0, UseObjectState.AmState1, UseObjectState.StateOutOfOrder, UseObjectState.StateDestroyed },
            [AugmentationType.PressurePlateSwitch] = new[] { UseObjectState.PpStateOff, UseObjectState.PpStateOn },
            [AugmentationType.PowerModule] = new[] { UseObjectState.StatePowerUp, UseObjectState.StatePowerDown, UseObjectState.StateOutOfOrder, UseObjectState.StateDestroyed },
            [AugmentationType.PoweredAccessModule] = new[] { UseObjectState.AmState0, UseObjectState.AmState1, UseObjectState.PamStateInUse, UseObjectState.StatePowerDown, UseObjectState.StateOutOfOrder, UseObjectState.StateDestroyed },
            [AugmentationType.PowerGenerator] = new[] { UseObjectState.StatePowerUp, UseObjectState.StatePowerDown, UseObjectState.PgStateEmpty, UseObjectState.StateOutOfOrder, UseObjectState.StateDestroyed },
            [AugmentationType.EnergyHopper] = new[] { UseObjectState.EhStateActive, UseObjectState.EhStateInactive, UseObjectState.StateOutOfOrder, UseObjectState.StateDestroyed },
            [AugmentationType.Overloader] = new[] { UseObjectState.OlStateOn, UseObjectState.OlStateOff, UseObjectState.OlStateOverload0, UseObjectState.OlStateOverload1, UseObjectState.OlStateOverload2, UseObjectState.OlStateOverload3, UseObjectState.OlStateOverload4, UseObjectState.StateDestroyed },
            [AugmentationType.AdventureLauncher] = new[] { UseObjectState.AlStateReadyToLaunch, UseObjectState.StatePowerDown, UseObjectState.StatePowerUp },
            [AugmentationType.AdventureExtractor] = new[] { UseObjectState.AeStateReadyToExtract, UseObjectState.StatePowerDown },
            [AugmentationType.BenefactorDiskPuzzlePiece] = new[] { UseObjectState.BpuzStateIncorrect, UseObjectState.BpuzStateCorrect },
            [AugmentationType.MechPad] = new[] { UseObjectState.MechpadOnReady, UseObjectState.MechpadOff, UseObjectState.MechpadBoarding, UseObjectState.MechpadOnEmpty }
        };

        /// <summary>The tables that place the classes this leaves out, by augmentation.</summary>
        public static readonly IReadOnlyDictionary<AugmentationType, string> PlacedElsewhere = new Dictionary<AugmentationType, string>
        {
            [AugmentationType.StatelessSwitch] = "world_destructible",
            [AugmentationType.Shrine] = "world_destructible",
            [AugmentationType.InertDestroyable] = "world_destructible",
            [AugmentationType.TeslaCoil] = "world_destructible",
            [AugmentationType.CreatureSpawner] = "world_destructible",
            [AugmentationType.DestroyableCreatureSpawner] = "world_destructible",
            [AugmentationType.DestroyableStatelessSwitch] = "world_destructible",
            [AugmentationType.ForceField] = "world_destructible",
            [AugmentationType.ClanForceField] = "world_destructible",
            [AugmentationType.OwnableForceField] = "world_destructible",
            [AugmentationType.ControlPoint] = "control_point",
            [AugmentationType.ClanControlPoint] = "control_point",
            [AugmentationType.OwnableControlPoint] = "control_point",
            [AugmentationType.Kraftwerks] = "kraftwerks",
            [AugmentationType.Wormhole] = "teleporter",
            [AugmentationType.LootDispenser] = "the server's loot (LootDispenserManager)",
            [AugmentationType.LootableProxy] = "nothing: a lootable proxy is the server's to make",
            [AugmentationType.ClanLockbox] = "control_point (a clan control point's lockbox)",
            [AugmentationType.ChallengeBoard] = "control_point (a clan control point's challenge board)"
        };

        public sealed class Placement
        {
            public uint Id { get; init; }
            public uint MapContextId { get; init; }
            public EntityClasses ClassId { get; init; }
            public AugmentationType Augmentation { get; init; }
            public Kind Kind { get; init; }
            public Vector3 Position { get; init; }
            public double Rotation { get; init; }

            /// <summary>The state it starts in, its kind's if the row gives none.</summary>
            public UseObjectState State { get; init; }
            public bool Usable { get; init; }
            public uint ItemTemplateId { get; init; }
            public uint ItemClassId { get; init; }
            public uint ItemCount { get; init; }
            public long ResetMs { get; init; }
            public long IntervalMinMs { get; init; }
            public long IntervalMaxMs { get; init; }
            public uint DamageMin { get; init; }
            public uint DamageMax { get; init; }
            public float Radius { get; init; }
            public uint ActionArg { get; init; }
            public string Comment { get; init; }

            /// <summary>Put down by a GM: no row, gone with its map channel.</summary>
            public bool IsPutDown { get; init; }

            public override string ToString() => IsPutDown
                ? $"put down {(uint)ClassId} ({Kind})"
                : $"world_usable {Id} ({(uint)ClassId}, {Kind})";
        }

        /// <summary>A placement on one map channel.</summary>
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
            public UseObjectState State => Object.StateId;

            /// <summary>When it goes back to how it started (a dispenser, an acceptor, a bomb, a door); 0 for nothing due.</summary>
            public long ResetAt { get; internal set; }

            /// <summary>When an armed bomb goes off; 0 if it is not armed.</summary>
            public long FuseAt { get; internal set; }
            public ulong ArmedBy { get; internal set; }

            /// <summary>When a random explosion goes off next, and when its effect is put back.</summary>
            public long NextBlastAt { get; internal set; }
            public long RearmAt { get; internal set; }
        }

        private static List<Placement> _placements = new List<Placement>();
        private static readonly ConditionalWeakTable<MapChannel, List<Live>> Channels = new();
        private static IGameUnitOfWorkFactory _unitOfWorkFactory;

        public static IReadOnlyList<Placement> All => _placements;

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A roll from the least to the most inclusive; a test's to replace.</summary>
        internal static Func<long, long, long> Roll { get; set; } = (min, max) => Random.Shared.NextInt64(min, max + 1);

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Roll = (min, max) => Random.Shared.NextInt64(min, max + 1);
        }

        #region Loading and placing

        /// <summary>Loads world_usable and puts each object on its map. Runs after MapChannelInit.</summary>
        public static void Init(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _unitOfWorkFactory = gameUnitOfWorkFactory;

            using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();

            var rows = unitOfWork.WorldUsables.Get();
            var kept = Load(rows);
            var placed = 0;

            foreach (var mapContextId in _placements.Select(p => p.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += Place(mapChannel);
                else
                    Logger.WriteLog(LogType.Initialize, $"  world usables: map {mapContextId} is not loaded");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {kept} of {rows.Count} world usables, {placed} in the world");
        }

        /// <summary>
        /// Takes the rows as what is placed from here on. One whose class is not loaded, is no usable
        /// this places, or whose state is not one of its class's, is logged and left out. Returns how
        /// many were kept.
        /// </summary>
        public static int Load(IEnumerable<WorldUsableEntry> rows)
        {
            var placements = new List<Placement>();

            foreach (var row in rows ?? Enumerable.Empty<WorldUsableEntry>())
            {
                var placement = PlacementOf(row, out var why);

                if (placement == null)
                {
                    Logger.WriteLog(LogType.Error, $"world_usable {row.Id}: {why}; left out");
                    continue;
                }

                placements.Add(placement);
            }

            _placements = placements;

            return placements.Count;
        }

        /// <summary>A row as it is to be placed, its kind's defaults filled in; null, with why, if it cannot be.</summary>
        public static Placement PlacementOf(WorldUsableEntry row, out string why, bool isPutDown = false)
        {
            why = WhyNot((EntityClasses)row.ClassId, out var augmentation, out var kind);

            if (why != null)
                return null;

            var states = StatesOf[augmentation];
            var state = row.State == 0 ? states[0] : (UseObjectState)row.State;

            if (!states.Contains(state))
            {
                why = $"state {row.State} is not one of its class's ({string.Join(", ", states.Select(s => (int)s))})";
                return null;
            }

            if (kind == Kind.Dispenser && row.Usable != 0 && row.ItemTemplateId != 0 && ItemManager.Instance?.GetItemTemplateById(row.ItemTemplateId) == null)
            {
                why = $"item template {row.ItemTemplateId} is not loaded";
                return null;
            }

            var className = EntityClassManager.Instance.GetClassInfo((EntityClasses)row.ClassId)?.ClassName ?? "";

            long Seconds(uint value, uint fallback) => (value != 0 ? value : fallback) * 1000L;

            var damageMin = row.DamageMin != 0 ? row.DamageMin : kind == Kind.Bomb ? DefaultBombDamageMin : DefaultExplosionDamageMin;
            var damageMax = Math.Max(damageMin, row.DamageMax != 0 ? row.DamageMax : kind == Kind.Bomb ? DefaultBombDamageMax : DefaultExplosionDamageMax);
            var intervalMin = kind == Kind.Bomb ? Seconds(row.IntervalMinSeconds, DefaultBombFuseSeconds) : Seconds(row.IntervalMinSeconds, DefaultExplosionMinSeconds);
            var intervalMax = Math.Max(intervalMin, kind == Kind.Bomb ? intervalMin : Seconds(row.IntervalMaxSeconds, DefaultExplosionMaxSeconds));

            return new Placement
            {
                Id = row.Id,
                MapContextId = row.MapContextId,
                ClassId = (EntityClasses)row.ClassId,
                Augmentation = augmentation,
                Kind = kind,
                Position = row.Position,
                Rotation = row.Rotation,
                State = state,
                Usable = row.Usable != 0 && kind is not (Kind.Explosion or Kind.Display),
                ItemTemplateId = row.ItemTemplateId,
                ItemClassId = row.ItemClassId,
                ItemCount = Math.Max(1, row.ItemCount),
                ResetMs = kind switch
                {
                    Kind.Dispenser => Seconds(row.ResetSeconds, DefaultDispenserResetSeconds),
                    Kind.Acceptor => Seconds(row.ResetSeconds, DefaultAcceptorResetSeconds),
                    Kind.Bomb => Seconds(row.ResetSeconds, DefaultBombResetSeconds),
                    _ => row.ResetSeconds * 1000L
                },
                IntervalMinMs = intervalMin,
                IntervalMaxMs = intervalMax,
                DamageMin = damageMin,
                DamageMax = damageMax,
                Radius = (float)(row.Radius > 0 ? row.Radius : kind == Kind.Bomb ? DefaultBombRadius : DefaultExplosionRadius),
                ActionArg = row.ActionArg != 0 ? row.ActionArg : kind == Kind.Bomb ? BombBlast : ArgOf(className),
                Comment = row.Comment ?? "",
                IsPutDown = isPutDown
            };
        }

        /// <summary>Why a class cannot be placed here, or null if it can; with its augmentation and what it would be.</summary>
        public static string WhyNot(EntityClasses classId, out AugmentationType augmentation, out Kind kind)
        {
            augmentation = 0;
            kind = Kind.Display;

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var found) ? found : null;

            if (classInfo == null)
                return $"entity class {(uint)classId} is not loaded";

            var augmentations = classInfo.Augmentations ?? new List<AugmentationType>();
            var elsewhere = augmentations.FirstOrDefault(a => PlacedElsewhere.ContainsKey(a));

            if (PlacedElsewhere.ContainsKey(elsewhere))
                return $"entity class {(uint)classId} ({classInfo.ClassName}) is placed by {PlacedElsewhere[elsewhere]}";

            var mine = augmentations.Where(a => StatesOf.ContainsKey(a)).ToList();

            if (mine.Count == 0)
                return $"entity class {(uint)classId} ({classInfo.ClassName}) is no usable this places";

            augmentation = mine[0];
            kind = KindOf(augmentation);
            return null;
        }

        public static Kind KindOf(AugmentationType augmentation) => augmentation switch
        {
            AugmentationType.TwoStateSwitch => Kind.TwoState,
            AugmentationType.TriggeredTwoState => Kind.TriggeredTwoState,
            AugmentationType.ThreeStateSwitch => Kind.ThreeState,
            AugmentationType.FiveStateSwitch => Kind.FiveState,
            AugmentationType.Door => Kind.Door,
            AugmentationType.ItemDispenser => Kind.Dispenser,
            AugmentationType.ItemAcceptor => Kind.Acceptor,
            AugmentationType.Bomb => Kind.Bomb,
            AugmentationType.RandomExplosion => Kind.Explosion,
            _ => Kind.Display
        };

        /// <summary>A random explosion's OBJ_DAMAGE arg by its class's name.</summary>
        public static uint ArgOf(string className)
        {
            var name = className?.ToLowerInvariant() ?? "";

            if (name.Contains("lightning")) return Lightning;
            if (name.Contains("geyser")) return Geyser;
            if (name.Contains("hornito")) return Hornito;
            if (name.Contains("bomb")) return BombBlast;
            if (name.Contains("mortar") || name.Contains("cannon")) return ExplosionLarge;
            return ExplosionSmall;
        }

        /// <summary>The damage type of a blast of an OBJ_DAMAGE arg.</summary>
        public static DamageType DamageTypeOf(uint arg) => arg switch
        {
            Lightning => DamageType.Electrical,
            Geyser or Hornito => DamageType.Fire,
            _ => DamageType.Physical
        };

        /// <summary>
        /// Puts this map's objects on one channel of it - the open world's or a private copy - and
        /// tells whoever is in range. One the channel already has is left alone. Returns how many
        /// were added.
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

        /// <summary>Puts one down on a map channel that is no row and is gone when the channel is (.worldusable put). Null, with why, if it cannot be.</summary>
        public static Live PutDown(MapChannel mapChannel, WorldUsableEntry row, out string why)
        {
            why = "there is no map here";

            if (mapChannel?.MapInfo == null || row == null)
                return null;

            row.MapContextId = mapChannel.MapInfo.MapContextId;
            row.Comment = "put down by a GM";

            var placement = PlacementOf(row, out why, isPutDown: true);

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
                DynamicObjectType = DynamicObjectType.PlacedUsable,
                Position = placement.Position,
                Rotation = placement.Rotation,
                MapContextId = placement.MapContextId,
                TargetCategory = TargetCategory.Object,
                StateId = placement.State,
                IsEnabled = placement.Usable,
                IsInWorld = true,
                ObjectData = live,
                WindupTime = UseWindupMs,
                Comment = placement.ToString()
            };

            if (placement.Kind == Kind.Explosion)
                live.NextBlastAt = Now() + NextWait(placement);

            CellManager.Instance.AddToWorld(map, live.Object);

            var lives = Channels.GetOrCreateValue(map);

            lock (lives)
                lives.Add(live);

            return live;
        }

        private static long NextWait(Placement placement) =>
            Math.Clamp(Roll(placement.IntervalMinMs, placement.IntervalMaxMs), placement.IntervalMinMs, placement.IntervalMaxMs);

        private static void TakeAway(Live live)
        {
            if (Channels.TryGetValue(live.Map, out var lives))
                lock (lives)
                    lives.Remove(live);

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

        /// <summary>The placement an object stands for, or null.</summary>
        public static Live LiveOf(DynamicObject obj) => obj?.ObjectData as Live;

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

        #endregion

        #region Using

        /// <summary>
        /// RequestUseObject for one of these (DynamicObjectManager): refused as the client would put
        /// it, or wound up for the player, the change to come at the recovery (<see cref="UseRecovery"/>).
        /// Returns whether it was taken.
        /// </summary>
        public static bool RequestUse(Client client, DynamicObject obj, RequestUseObjectPacket packet)
        {
            var player = client?.Player;
            var map = player?.MapChannel;

            if (obj?.ObjectData is not Live live || player == null || map == null)
                return false;

            void Refuse(PlayerMessage message) => ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, message);

            if (!ReferenceEquals(live.Map, map))
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} asked to use {live.Placement}, which is not on their map channel. Ignored.");
                return false;
            }

            if (packet.ActionId != ActionId.UseObject || !live.Placement.Usable)
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

            var refusal = RefusalOf(client, live);

            if (refusal != null)
            {
                Refuse(refusal.Value);
                return false;
            }

            if (map.PerformRecovery.Any(action => action.ActionId == ActionId.UseObject && (action.Actor == player || action.SourceId == obj.EntityId)))
            {
                Refuse(PlayerMessage.PmCannotPerformActionNow);
                return false;
            }

            client.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
            map.PerformRecovery.Add(new ActionData(player, packet.ActionId, packet.ActionArgId, UseWindupMs) { SourceId = obj.EntityId });

            return true;
        }

        /// <summary>Why the player cannot use it as it is now, or null if they can.</summary>
        private static PlayerMessage? RefusalOf(Client client, Live live)
        {
            switch (live.Placement.Kind)
            {
                case Kind.Dispenser:
                    if (live.State != UseObjectState.IdStateActive || live.Placement.ItemTemplateId == 0)
                        return PlayerMessage.PmUseObjectNotUsable;
                    break;

                case Kind.Acceptor:
                    if (live.State != UseObjectState.IaStateActive || live.Placement.ItemClassId == 0)
                        return PlayerMessage.PmUseObjectNotUsable;
                    if (InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)live.Placement.ItemClassId) < live.Placement.ItemCount)
                        return PlayerMessage.PmMissingReqItem;
                    break;

                case Kind.Bomb:
                    if (live.State == UseObjectState.BombStateDetonated)
                        return PlayerMessage.PmUseObjectNotUsable;
                    if (live.State == UseObjectState.BombStateDisarmed && live.Placement.ItemClassId != 0 &&
                        InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)live.Placement.ItemClassId) < live.Placement.ItemCount)
                        return PlayerMessage.PmMissingReqItem;
                    break;
            }

            return null;
        }

        /// <summary>Whether a use-object action is a use of one of these.</summary>
        public static bool IsUseOf(ActionData action) =>
            action != null && action.ActionId == ActionId.UseObject &&
            EntityManager.Instance.TryGetObject(action.SourceId, out var obj) && obj?.ObjectData is Live;

        /// <summary>
        /// The recovery of a use: unless it was interrupted, or its user died or left the map in the
        /// meantime, or it can no longer be used so, the change. Returns the state it went to, or null.
        /// </summary>
        public static UseObjectState? UseRecovery(MapChannel map, ActionData action)
        {
            if (action == null || action.IsInrerrupted || !EntityManager.Instance.TryGetObject(action.SourceId, out var obj) ||
                obj?.ObjectData is not Live live || !ReferenceEquals(live.Map, map))
                return null;

            if (action.Actor is not Manifestation user || user.State == CharacterState.Dead || user.MapChannel != map)
                return null;

            var client = map.ClientList.FirstOrDefault(candidate => ReferenceEquals(candidate?.Player, user));

            if (client == null || RefusalOf(client, live) != null)
                return null;

            var placement = live.Placement;
            var now = Now();
            UseObjectState next;

            switch (placement.Kind)
            {
                case Kind.Dispenser:
                    if (!Give(client, placement))
                        return null;

                    next = UseObjectState.IdStateInactive;
                    live.ResetAt = now + placement.ResetMs;
                    break;

                case Kind.Acceptor:
                    if (InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)placement.ItemClassId, placement.ItemCount) != 0)
                        return null;

                    next = UseObjectState.IaStateAccepted;
                    live.ResetAt = now + placement.ResetMs;
                    break;

                case Kind.Bomb:
                    if (live.State == UseObjectState.BombStateArmed)
                    {
                        next = UseObjectState.BombStateDisarmed;
                        live.FuseAt = 0;
                        live.ArmedBy = 0;
                        break;
                    }

                    if (placement.ItemClassId != 0 &&
                        InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)placement.ItemClassId, placement.ItemCount) != 0)
                        return null;

                    next = UseObjectState.BombStateArmed;
                    live.FuseAt = now + placement.IntervalMinMs;
                    live.ArmedBy = user.EntityId;
                    break;

                case Kind.Door:
                    next = live.State == UseObjectState.DoorStateOpen ? UseObjectState.DoorStateClosed : UseObjectState.DoorStateOpen;
                    live.ResetAt = next == UseObjectState.DoorStateOpen && placement.ResetMs > 0 ? now + placement.ResetMs : 0;
                    break;

                default:
                    next = NextOf(placement.Augmentation, live.State);
                    live.ResetAt = placement.ResetMs > 0 && next != placement.State ? now + placement.ResetMs : 0;
                    break;
            }

            Show(live, next, user.EntityId);

            return next;
        }

        /// <summary>The state a switch steps to from <paramref name="current"/>: the next of its class's, the last back to the first.</summary>
        public static UseObjectState NextOf(AugmentationType augmentation, UseObjectState current)
        {
            var states = StatesOf[augmentation];
            var index = Array.IndexOf(states, current);

            return states[(index + 1) % states.Length];
        }

        /// <summary>The dispenser's item into the player's inventory. False, with the client told so, if none of it fitted.</summary>
        private static bool Give(Client client, Placement placement)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(placement.ItemTemplateId, placement.ItemCount);

            if (item == null)
                return false;

            var wanted = item.StackSize;

            if (InventoryManager.Instance.GrantItemToInventory(client, item) != null)
                return true;

            // What did not fit goes away; some may have.
            var placed = wanted - item.StackSize;

            EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);

            if (item.Id != 0 && _unitOfWorkFactory != null)
                using (var unitOfWork = _unitOfWorkFactory.CreateChar())
                    unitOfWork.Items.DeleteItem(item.Id);

            if (placed == 0)
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            return placed > 0;
        }

        #endregion

        #region Coming back, fuses and blasts

        /// <summary>Every tick: what is due back, bombs whose fuse has burnt down, and random explosions due to go off.</summary>
        public static void Worker(MapChannel map)
        {
            var lives = On(map);

            if (lives.Count == 0)
                return;

            var now = Now();

            foreach (var live in lives)
            {
                var placement = live.Placement;

                if (live.FuseAt != 0 && now >= live.FuseAt)
                {
                    live.FuseAt = 0;
                    Show(live, UseObjectState.BombStateDetonated, live.ArmedBy);
                    Blast(live);
                    live.ArmedBy = 0;
                    live.ResetAt = now + placement.ResetMs;
                    continue;
                }

                if (live.ResetAt != 0 && now >= live.ResetAt)
                {
                    live.ResetAt = 0;

                    if (live.State != placement.State)
                        Show(live, placement.State, 0);
                }

                if (placement.Kind != Kind.Explosion)
                    continue;

                if (live.RearmAt != 0 && now >= live.RearmAt)
                {
                    live.RearmAt = 0;
                    CellManager.Instance.CellCallMethod(map, live.Object, new ForceStatePacket(UseObjectState.ReState0, 0));
                }

                if (now >= live.NextBlastAt)
                {
                    // Missed while the map stood empty: somewhere in its next longest wait, as
                    // each of the others is, rather than all of them at once as someone arrives.
                    if (now - live.NextBlastAt > MissedMs)
                    {
                        live.NextBlastAt = now + Math.Clamp(Roll(0, placement.IntervalMaxMs), 0, placement.IntervalMaxMs);
                        continue;
                    }

                    Show(live, UseObjectState.ReState0, 0);
                    Blast(live);
                    live.RearmAt = now + RearmMs;
                    live.NextBlastAt = now + NextWait(placement);
                }
            }
        }

        /// <summary>
        /// The object blows up: every living player within its radius takes a roll of its damage,
        /// resisted, and everyone in range sees the blast and the hits. Returns the players hit.
        /// </summary>
        public static List<Manifestation> Blast(Live live)
        {
            var placement = live.Placement;
            var map = live.Map;
            var origin = live.Object.Position;
            var damageType = DamageTypeOf(placement.ActionArg);
            var packet = new PerformObjectDamagePacket(placement.ActionArg, live.Object.EntityId);
            var hit = new List<Manifestation>();

            foreach (var client in map.ClientList.ToList())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || !ReferenceEquals(player.MapChannel, map) ||
                    player.State == CharacterState.Dead || player.State == CharacterState.Dying ||
                    Vector3.Distance(player.Position, origin) > placement.Radius)
                    continue;

                if (player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
                    continue;

                var rolled = (int)Math.Clamp(Roll(placement.DamageMin, placement.DamageMax), placement.DamageMin, placement.DamageMax);
                var amount = GameEffectManager.ApplyResist(player, rolled, out var resisted, damageType);
                var taken = ActorManager.Instance.Damage(map, player, amount, null, out var outcome, damageType);

                packet.Hits.Add(new PerformObjectDamagePacket.Hit
                {
                    TargetId = player.EntityId,
                    DamageType = damageType,
                    Amount = outcome.Delivered,
                    Resisted = resisted,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    DeathBlow = taken > 0 && player.Attributes.TryGetValue(Attributes.Health, out var after) && after.Current <= 0
                });

                hit.Add(player);
            }

            CellManager.Instance.CellCallMethod(map, live.Object, packet);

            return hit;
        }

        #endregion

        /// <summary>A GM's (.worldusable state): the state, to everyone in range, whatever it was; anything due is forgotten.</summary>
        public static void SetState(Live live, UseObjectState state, ulong actorId)
        {
            if (live == null)
                return;

            live.ResetAt = 0;
            live.FuseAt = 0;
            live.Object.StateId = state;
            CellManager.Instance.CellCallMethod(live.Map, live.Object, new ForceStatePacket(state, 0));
        }

        /// <summary>Its state, to everyone in range, by Use: each of these has the transition.</summary>
        private static void Show(Live live, UseObjectState state, ulong actorId)
        {
            live.Object.StateId = state;
            CellManager.Instance.CellCallMethod(live.Map, live.Object, new UsePacket(actorId, state, 0));
        }
    }
}
