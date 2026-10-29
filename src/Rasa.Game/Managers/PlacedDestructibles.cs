using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;
    using global::Rasa.Missions.Runtime;

    /// <summary>
    /// Destructible world objects the server puts on maps: rows of world_destructible, put down on
    /// their map when it comes up and on each private copy of it as that is made, as the ambient
    /// figures are (AmbientNpcs). The client has 133 classes with a destroyed state - their
    /// explosions and wrecks - and the .map places only a few (WorldDestructibles); the rest were
    /// the live servers' to put down, and their placements were not kept. Later migrations put
    /// them in (WorldDestructiblePreloader); a GM can put one down to look at it (.destructible).
    ///
    /// What one is comes from its class's augmentation:
    ///  - InertDestroyable (41): a barrel, a locker, machinery. Intact, then down its damaged
    ///    states to its explosion and wreck (DestroyableStates); back by Use, destroyed to intact.
    ///  - DestroyableCreatureSpawner (68), "spawns when destroyed": the same, and its creatures
    ///    come out of it when it goes, after whoever destroyed it.
    ///  - CreatureSpawner (61), "spawn creatures into the world": a hive. Idle; a living player who
    ///    comes within <see cref="SpawnerTriggerRadius"/> sets it off - Use to BEGIN, to SPAWN with
    ///    its creatures coming out, to END, and back to IDLE, the client's transitions - and it goes
    ///    again once what came out is dead and <see cref="SpawnerRearmMs"/> have passed. Straight to
    ///    destroyed from any of those, and back to idle.
    ///  - TeslaCoil (58): straight to destroyed, back to POWER_DOWN; it zaps while standing (TeslaCoils).
    ///  - DestroyableStatelessSwitch (69), the Hortimonculus: grown (POWER_UP), destroyed from there;
    ///    back as a sprout that grows (ForceState POWER_DOWN, Use POWER_UP), as the client has no
    ///    way from destroyed.
    ///  - a force field (62, 79, 84): ForceFields, on the row's side (the Bane's by default, which
    ///    players can shoot), repaired when it is due back. A sealed row's (world_destructible.sealed)
    ///    takes nothing from shots (ForceFields.Field.Sealed) in every copy of the map; a mission
    ///    takes it down for good in one copy (<see cref="TakeDown"/>, MissionActionKind.TakeDownField).
    ///  - a Shrine (13) or a StatelessSwitch (8) has destroyed content and is no destroyable to the
    ///    client: it is put down as scenery in its one state, and nothing hurts it.
    ///  - a class with no augmentation at all - a plain model, as the .map's own pieces - is left
    ///    out, unless <see cref="PlainScenery"/> names it: then it is put down as scenery with no
    ///    state, as the Pools' Eloh obelisk at the western fort, which the live servers had and the
    ///    1.16.5 .map has not (PoolsObeliskV1).
    ///
    /// Hit points (ours, by size, unless the row gives some): <see cref="SmallHitPoints"/>,
    /// <see cref="MediumHitPoints"/>, <see cref="LargeHitPoints"/> (<see cref="SizeOf"/>); a force
    /// field ForceFields.DefaultHealth. Only players hurt them (PracticeTargetManager.CanHit); a
    /// force field, whoever it stops. Destroyed, one is back <see cref="RespawnMinMs"/> to
    /// <see cref="RespawnMaxMs"/> later at full hit points. Every map channel has its own.
    ///
    /// A spawner's creature is the row's, or its class's (<see cref="SpawnerDefaults"/>): the
    /// creature of that kind the map's spawn pools use most, or one of the kind if they use none.
    /// What comes out is born beside it, held through its birth and goes for whoever set it off
    /// (FithikEggClusters.Hatch); it is taken away 5 minutes later if it is fighting nothing.
    ///
    /// The client is told the object's state with its creation, and its hit points, that it can
    /// be damaged while it stands - which makes it a target, for a class that is targetable - and
    /// that it is an object. Twenty-five of the classes are not targetable (entityclass target
    /// flag 0): they can be placed, and nothing a player does on the client picks them out.
    /// </summary>
    public static class PlacedDestructibles
    {
        public const uint SmallHitPoints = 300;
        public const uint MediumHitPoints = 600;
        public const uint LargeHitPoints = 1500;

        public const long RespawnMinMs = WorldDestructibles.RespawnMinMs;
        public const long RespawnMaxMs = WorldDestructibles.RespawnMaxMs;

        /// <summary>How near on the ground a living player sets off a creature spawner (61).</summary>
        public const float SpawnerTriggerRadius = 20f;

        /// <summary>How far above or below a spawner's origin they may be and still set it off.</summary>
        public const float SpawnerTriggerHeight = 8f;

        public const int SpawnerBeginMs = 1000;
        public const int SpawnerSpawnMs = 2000;
        public const int SpawnerEndMs = 3000;

        /// <summary>A spawner goes again no sooner than this after it last spawned, and only once what came out is dead.</summary>
        public const long SpawnerRearmMs = 60_000;

        /// <summary>How often a copy looks at who is in it for sealed fields their missions took down (<see cref="CatchUp"/>).</summary>
        public const long CatchUpMs = 1000;

        /// <summary>The Practice Dummy has its own ways (PracticeTargetManager) and is no row's.</summary>
        public const uint PracticeDummyClass = PracticeTargetManager.EntityClassId;

        /// <summary>
        /// The plain models (no augmentation) a row may put down, as scenery with no state. Any other
        /// is a row's mistake and is left out: a destructible table is not for the .map's pieces.
        /// </summary>
        public static readonly IReadOnlySet<uint> PlainScenery = new HashSet<uint>
        {
            10259,  // ArchElohObeliskV01: the Pools' obelisk at the western Bane fort (PoolsObeliskV1)
        };

        public enum Kind
        {
            Inert,              // INERTDESTROYABLE 41
            DestroyedSpawner,   // DESTROYABLECREATURESPAWNER 68
            Spawner,            // CREATURESPAWNER 61
            TeslaCoil,          // TESLACOIL 58
            Switch,             // DESTROYABLESTATELESSSWITCH 69
            ForceField,         // FORCEFIELD 62, CLANFORCEFIELD 79, OWNABLEFORCEFIELD 84
            Scenery             // SHRINE 13, STATELESSSWITCH 8: not destroyable to the client
        }

        public enum Size { Small, Medium, Large }

        /// <summary>
        /// The size of each class with destroyed content (ours): small 300, medium 600, large 1500.
        /// A destroyable class that is not here is medium.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, Size> Sizes = new Dictionary<uint, Size>
        {
            // Creature spawners
            [10000053] = Size.Medium,   // UsableAbilityHortimonculus
            [21551] = Size.Medium,      // UsableCrSpawnerDELETEV01
            [24996] = Size.Medium,      // UsableCrSpawnerDestAttaEggClusterV01DONOTUSE
            [24997] = Size.Medium,      // UsableCrSpawnerDestAttaEggClusterV02DONOTUSE
            [25301] = Size.Small,       // UsableCrSpawnerDestAttaEggV01DONOTUSE
            [21455] = Size.Medium,      // UsableCrSpawnerDestDELETEDUPE
            [20758] = Size.Small,       // UsableCrSpawnerDestFithikEgg
            [10180] = Size.Small,       // UsableCrSpawnerDestFithikEggClusterV01
            [26517] = Size.Medium,      // UsableCrSpawnerDestWardenbotV01
            [26021] = Size.Medium,      // UsableCrSpawnerDestWardenbotV01DELETEDUPE
            [26518] = Size.Medium,      // UsableCrSpawnerDestWardenbotV02
            [24689] = Size.Medium,      // UsableCrSpawnerDestWardenbotV02DELETEDUPE
            [21804] = Size.Medium,      // UsableCrSpawnerDestWarnetHiveV01
            [21805] = Size.Medium,      // UsableCrSpawnerDestWarnetHiveV02

            // Inert destroyables
            [10088] = Size.Small,       // UsableInertDestAriekiFlamepodsV01
            [26166] = Size.Small,       // UsableInertDestAttaBlobV01
            [7964] = Size.Medium,       // UsableInertDestAttaDoorV01DELETEV01
            [25386] = Size.Medium,      // UsableInertDestAttaQueeneggV01
            [7877] = Size.Medium,       // UsableInertDestBaneArmoryMachineryV01
            [10183] = Size.Medium,      // UsableInertDestBaneArmoryMachineryV01DELETEDUPE
            [6318] = Size.Medium,       // UsableInertDestBaneArticulatedDrillV01
            [10670] = Size.Small,       // UsableInertDestBaneBarrelMaroxinDELETEDUPE
            [9260] = Size.Small,        // UsableInertDestBaneBarrelV01
            [10363] = Size.Small,       // UsableInertDestBaneBarrelV01DELETEDUPE
            [10290] = Size.Small,       // UsableInertDestBaneCameraV01
            [25498] = Size.Large,       // UsableInertDestBaneCentralLabMachine
            [25921] = Size.Large,       // UsableInertDestBaneCentralLabTower
            [26458] = Size.Large,       // UsableInertDestBaneCentralLabTowerCanisters
            [23173] = Size.Medium,      // UsableInertDestBaneCommandChairFloorV01
            [7502] = Size.Medium,       // UsableInertDestBaneConsoleBrainBase
            [21983] = Size.Medium,      // UsableInertDestBaneDrillDELETEDUPE
            [23091] = Size.Large,       // UsableInertDestBaneFoundryV01
            [12991] = Size.Small,       // UsableInertDestBaneFuelDepotFuelV01
            [12998] = Size.Large,       // UsableInertDestBaneFuelDepotTankPumpV01
            [12981] = Size.Large,       // UsableInertDestBaneFuelDepotTankV01
            [7906] = Size.Large,        // UsableInertDestBaneGasHarvesterV01
            [20594] = Size.Medium,      // UsableInertDestBaneGeneratorSmallV01
            [7548] = Size.Large,        // UsableInertDestBaneHarvesterDELETEDUPE
            [28434] = Size.Large,       // UsableInertDestBaneIndustrialChunnelMachine05V01
            [6225] = Size.Small,        // UsableInertDestBaneInfestationV01
            [20642] = Size.Small,       // UsableInertDestBaneLabEquipmentCeilingV01
            [20643] = Size.Medium,      // UsableInertDestBaneLabEquipmentMachineV01
            [20639] = Size.Small,       // UsableInertDestBaneLabEquipmentShelfV01
            [20641] = Size.Small,       // UsableInertDestBaneLabEquipmentTableV01
            [20595] = Size.Small,       // UsableInertDestBaneLabEquipmentV01
            [20591] = Size.Small,       // UsableInertDestBaneLockerClosed
            [20589] = Size.Small,       // UsableInertDestBaneLockerOpen
            [9600] = Size.Large,        // UsableInertDestBaneMachinaFactoryBaseUpper
            [9272] = Size.Large,        // UsableInertDestBaneMachinaFactoryCenter
            [9485] = Size.Large,        // UsableInertDestBaneMachinaFactoryTubes
            [10159] = Size.Medium,      // UsableInertDestBaneMachineV01
            [7874] = Size.Medium,       // UsableInertDestBaneMachineryV01DELETEDUPE
            [24922] = Size.Large,       // UsableInertDestBaneMethalineProcessingTowerV01
            [21873] = Size.Large,       // UsableInertDestBaneMissileLauncherV01
            [3857] = Size.Large,        // UsableInertDestBaneOutpostGenerator
            [20592] = Size.Small,       // UsableInertDestBanePipeV01
            [20593] = Size.Small,       // UsableInertDestBanePipeV02
            [10174] = Size.Large,       // UsableInertDestBanePrototypeWormholeDeviceV01
            [23133] = Size.Large,       // UsableInertDestBaneRawFuelPumpGroupV01
            [10190] = Size.Large,       // UsableInertDestBaneRawFuelPumpHouseV01
            [21007] = Size.Large,       // UsableInertDestBaneRawFuelPumpHouseV02
            [4443] = Size.Large,        // UsableInertDestBaneReactorPlasmaPumpV01
            [24733] = Size.Medium,      // UsableInertDestBaneSecurityMachineryTallV01
            [24735] = Size.Medium,      // UsableInertDestBaneSecurityMachineryTallV02
            [10191] = Size.Medium,      // UsableInertDestBaneSecurityMachineryV01
            [21000] = Size.Medium,      // UsableInertDestBaneSecurityMachineryV02
            [20621] = Size.Medium,      // UsableInertDestBaneSleepPodV01
            [20661] = Size.Medium,      // UsableInertDestBaneSleepPodV02
            [20760] = Size.Medium,      // UsableInertDestBaneSleepPodV03
            [9257] = Size.Medium,       // UsableInertDestBaneSonicFence
            [23070] = Size.Large,       // UsableInertDestBaneSonicTowerDELETE
            [10282] = Size.Large,       // UsableInertDestBaneSonicTowerV01
            [21964] = Size.Large,       // UsableInertDestBaneSonicTowerV02
            [10192] = Size.Small,       // UsableInertDestBaneSpotlightV01
            [7197] = Size.Small,        // UsableInertDestBaneStasisChamber
            [20752] = Size.Small,       // UsableInertDestBaneStasisChamberBrannFV01
            [20753] = Size.Small,       // UsableInertDestBaneStasisChamberBrannMV01
            [20755] = Size.Small,       // UsableInertDestBaneStasisChamberHumFV01
            [20756] = Size.Small,       // UsableInertDestBaneStasisChamberHumMV01
            [10193] = Size.Large,       // UsableInertDestBaneSupplyCraneV01
            [20612] = Size.Medium,      // UsableInertDestBaneTankV01
            [20613] = Size.Medium,      // UsableInertDestBaneTankV02
            [20614] = Size.Medium,      // UsableInertDestBaneTankV03
            [10603] = Size.Large,       // UsableInertDestBaneTeleporterV01DELETEDUPE
            [29105] = Size.Medium,      // UsableInertDestBaneTeslaCoilFluxiteCloudV01
            [21062] = Size.Large,       // UsableInertDestBaneWallV01DELETEDUPE
            [10285] = Size.Large,       // UsableInertDestBaneWormholeDeviceV02DELETEDUPE
            [22106] = Size.Small,       // UsableInertDestBlackBoxV01
            [24053] = Size.Small,       // UsableInertDestBrannCameraV01
            [26590] = Size.Medium,      // UsableInertDestBrannDoorV01
            [10776] = Size.Large,       // UsableInertDestBrannGeneratorV01
            [7921] = Size.Medium,       // UsableInertDestBrannMachineryV01DELETEDUPE
            [24050] = Size.Medium,      // UsableInertDestBrannMachineryv01
            [24607] = Size.Small,       // UsableInertDestBrannTableDissectionAttaV01
            [25269] = Size.Small,       // UsableInertDestBrannTableDissectionAttaV01DELETEDUPE
            [25602] = Size.Small,       // UsableInertDestBrannTableDissectionEmptyV01
            [9536] = Size.Small,        // UsableInertDestHumBarrelV01
            [28546] = Size.Small,       // UsableInertDestHumBulletLightV01
            [28548] = Size.Small,       // UsableInertDestHumBulletLightV01Smoke
            [28547] = Size.Small,       // UsableInertDestHumBulletLightV01Sparks
            [26703] = Size.Small,       // UsableInertDestHumCannisterV01
            [7859] = Size.Small,        // UsableInertDestHumCrateV01
            [20000058] = Size.Large,    // UsableInertDestHumDropshipDestroyedV03
            [29346] = Size.Small,       // UsableInertDestHumFloorTile
            [9247] = Size.Large,        // UsableInertDestHumFortDoorLargeV01
            [26406] = Size.Medium,      // UsableInertDestHumPenumbraThraxMachinaTankV01
            [7099] = Size.Medium,       // UsableInertDestMinoswarnetnestDELETEV01
            [23098] = Size.Medium,      // UsableInertDestMisElohValeRock_DONOTUSE
            [29223] = Size.Large,       // UsableInertDestTerraCaveinV01
            [10287] = Size.Medium,      // UsableInertDestThraxMachineryV01DELETEDUPE
            [7158] = Size.Medium,       // UsableInertDestTreebackpoweconduitDELETEDUPE
            [26027] = Size.Small,       // UsableInertDestWardenbotPatch
            [25980] = Size.Medium,      // UsableInertDestWardenbotPylonV01
            [30551] = Size.Medium,      // UsableInertDestWardenbotPylonV01TEST
            [9644] = Size.Medium,       // UsableInertDestWarnetNestDELETEV02
            [7116] = Size.Medium,       // UsableInertDestWarnetnestDELETEV01
            [10176] = Size.Medium,      // UsableInertDestXanxNest

            // Tesla coil
            [3899] = Size.Medium        // UsableTeslaCoilBaneV01
        };

        /// <summary>A spawner class's creature: its kind (creature class), the row to fall back on, and how many come out at a time.</summary>
        public sealed record SpawnerDefault(uint CreatureClass, uint FallbackCreatureId, uint Count);

        /// <summary>The creature spawners named for what is in them (ours: the kind, and how many).</summary>
        public static readonly IReadOnlyDictionary<uint, SpawnerDefault> SpawnerDefaults = new Dictionary<uint, SpawnerDefault>
        {
            // Fithik: the Ranja egg clusters' hatchling, Bane_Fithik_Wingless_EggCluster.
            [20758] = new SpawnerDefault(21499, 552001, 1),     // UsableCrSpawnerDestFithikEgg
            [10180] = new SpawnerDefault(21499, 552001, 3),     // UsableCrSpawnerDestFithikEggClusterV01

            // Atta: grubs (Creature_Atta_Grub_Standard), Plains' if the map has none.
            [24996] = new SpawnerDefault(29089, 531081, 3),     // UsableCrSpawnerDestAttaEggClusterV01DONOTUSE
            [24997] = new SpawnerDefault(29089, 531081, 3),     // UsableCrSpawnerDestAttaEggClusterV02DONOTUSE
            [25301] = new SpawnerDefault(29089, 531081, 1),     // UsableCrSpawnerDestAttaEggV01DONOTUSE

            // Warnet: soldiers (Creature_Warnet_Soldier), Palisades' if the map has none.
            [21804] = new SpawnerDefault(6262, 540005, 3),      // UsableCrSpawnerDestWarnetHiveV01
            [21805] = new SpawnerDefault(6262, 540005, 3),      // UsableCrSpawnerDestWarnetHiveV02

            // Wardenbots: Warden Bots (Creature_Prison_Bot_Warden), Incline's if the map has none.
            [26517] = new SpawnerDefault(6961, 531044, 2),      // UsableCrSpawnerDestWardenbotV01
            [26518] = new SpawnerDefault(6961, 531044, 2),      // UsableCrSpawnerDestWardenbotV02
            [26021] = new SpawnerDefault(6961, 531044, 2),      // UsableCrSpawnerDestWardenbotV01DELETEDUPE
            [24689] = new SpawnerDefault(6961, 531044, 2)       // UsableCrSpawnerDestWardenbotV02DELETEDUPE
        };

        /// <summary>How many come out of a spawner that has a creature and no count of its own or its class's.</summary>
        public const uint DefaultSpawnCount = 1;

        public sealed class Placement
        {
            public uint Id { get; init; }
            public uint MapContextId { get; init; }
            public EntityClasses ClassId { get; init; }
            public Kind Kind { get; init; }
            public Vector3 Position { get; init; }
            public double Rotation { get; init; }

            /// <summary>The row's: 0 for the class's.</summary>
            public uint HitPoints { get; init; }
            public uint CreatureId { get; init; }
            public uint CreatureCount { get; init; }
            public uint Side { get; init; }

            /// <summary>A force field shots do not take down: only <see cref="TakeDown"/> does.</summary>
            public bool Sealed { get; init; }

            public string Comment { get; init; }

            /// <summary>Put down by a GM: no row, gone with its map channel.</summary>
            public bool IsPutDown { get; init; }

            public override string ToString() => IsPutDown
                ? $"put down {(uint)ClassId} ({Kind})"
                : $"world_destructible {Id} ({(uint)ClassId}, {Kind})";
        }

        internal enum Stage { Idle, Begin, Spawn, End, Resting }

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

            /// <summary>Its object: a force field's too.</summary>
            public DynamicObject Object { get; internal set; }

            /// <summary>A force field's, or null.</summary>
            public ForceFields.Field Field { get; internal set; }

            public uint MaxHitPoints { get; internal set; }

            /// <summary>When it comes back, while it is down; 0 while it stands.</summary>
            public long RespawnAt { get; internal set; }

            /// <summary>A spawner's creature and how many come out at a time: 0 for none.</summary>
            public uint CreatureId { get; internal set; }
            public uint CreatureCount { get; internal set; }

            /// <summary>What has come out of it and is still about.</summary>
            public List<Creature> Brood { get; } = new List<Creature>();

            internal Stage Stage { get; set; }
            internal long NextAt { get; set; }
            internal long SpawnedAt { get; set; }
            internal ulong SetOffBy { get; set; }

            public uint HitPoints => Field != null ? (uint)Math.Max(0, Field.Health) : Object?.CurrentHitPoints ?? 0;

            public bool IsDown => Placement.Kind != Kind.Scenery && HitPoints == 0;
        }

        private static List<Placement> _placements = new List<Placement>();
        private static readonly ConditionalWeakTable<MapChannel, List<Live>> Channels = new();
        private static readonly ConditionalWeakTable<MapChannel, StrongBox<long>> NextCatchUps = new();

        /// <summary>The rows that were kept.</summary>
        public static IReadOnlyList<Placement> All => _placements;

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>The missions a destruction is recorded with; a test's to replace. Null records none.</summary>
        internal static Func<MissionApplication> MissionsOf { get; set; } = () => MissionApplication.Instance;

        private static MissionApplication Missions => MissionsOf?.Invoke();

        /// <summary>The wait before one comes back, from the least to the most inclusive; a test's to replace.</summary>
        internal static Func<long, long, long> Roll { get; set; } = (min, max) => Random.Shared.NextInt64(min, max + 1);

        /// <summary>Brings a creature out of a spawner (FithikEggClusters.Hatch); a test's to replace.</summary>
        internal static Func<MapChannel, DynamicObject, uint, Manifestation, Creature> Hatch { get; set; } = FithikEggClusters.Hatch;

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Roll = (min, max) => Random.Shared.NextInt64(min, max + 1);
            Hatch = FithikEggClusters.Hatch;
            MissionsOf = () => MissionApplication.Instance;
        }

        #region Loading and placing

        /// <summary>Loads world_destructible and puts each object on its map. Runs after MapChannelInit and the spawn pools.</summary>
        public static void Init(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();

            var rows = unitOfWork.WorldDestructibles.Get();
            var kept = Load(rows);
            var placed = 0;

            foreach (var mapContextId in _placements.Select(p => p.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += Place(mapChannel);
                else
                    Logger.WriteLog(LogType.Initialize, $"  world destructibles: map {mapContextId} is not loaded");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {kept} of {rows.Count} world destructibles, {placed} in the world");
        }

        /// <summary>
        /// Takes the rows as what is placed from here on. One whose class the server's data does not
        /// have, or that has no destroyed state the server can show, is logged and left out.
        /// Returns how many were kept.
        /// </summary>
        public static int Load(IEnumerable<WorldDestructibleEntry> rows)
        {
            var placements = new List<Placement>();

            foreach (var row in rows ?? Enumerable.Empty<WorldDestructibleEntry>())
            {
                var why = WhyNot((EntityClasses)row.ClassId, out var kind);

                if (why != null)
                {
                    Logger.WriteLog(LogType.Error, $"world_destructible {row.Id}: {why}; left out");
                    continue;
                }

                if (row.Sealed && kind != Kind.ForceField)
                    Logger.WriteLog(LogType.Error, $"world_destructible {row.Id}: sealed, and it is no force field; sealed is left out");

                placements.Add(new Placement
                {
                    Id = row.Id,
                    MapContextId = row.MapContextId,
                    ClassId = (EntityClasses)row.ClassId,
                    Kind = kind,
                    Position = row.Position,
                    Rotation = row.Rotation,
                    HitPoints = row.HitPoints,
                    CreatureId = row.CreatureId,
                    CreatureCount = row.CreatureCount,
                    Side = row.Side,
                    Sealed = row.Sealed && kind == Kind.ForceField,
                    Comment = row.Comment ?? ""
                });
            }

            _placements = placements;

            return placements.Count;
        }

        /// <summary>Why a class cannot be placed, or null if it can; with what it would be.</summary>
        public static string WhyNot(EntityClasses classId, out Kind kind)
        {
            kind = Kind.Scenery;

            if ((uint)classId == PracticeDummyClass)
                return $"entity class {(uint)classId} is the Practice Dummy, which PracticeTargetManager has";

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var found) ? found : null;

            if (classInfo == null)
                return $"entity class {(uint)classId} is not loaded";

            if (KindOf(classInfo) is not { } of)
                return $"entity class {(uint)classId} ({classInfo.ClassName}) has no destroyed state the server shows";

            if (of == Kind.ForceField && ForceFields.ClassOf(((uint)classId).ToString()) == null)
                return $"entity class {(uint)classId} ({classInfo.ClassName}) is a force field ForceFields has no extents for";

            kind = of;
            return null;
        }

        /// <summary>What a class is, by its augmentations; null for one this does not place.</summary>
        public static Kind? KindOf(EntityClass classInfo)
        {
            var augmentations = classInfo?.Augmentations;

            if (augmentations == null)
                return null;

            if (augmentations.Count == 0)
                return PlainScenery.Contains(classInfo.ClassId) ? Kind.Scenery : null;

            if (augmentations.Contains(AugmentationType.ForceField) || augmentations.Contains(AugmentationType.ClanForceField) ||
                augmentations.Contains(AugmentationType.OwnableForceField))
                return Kind.ForceField;

            if (augmentations.Contains(AugmentationType.DestroyableCreatureSpawner))
                return Kind.DestroyedSpawner;

            if (augmentations.Contains(AugmentationType.CreatureSpawner))
                return Kind.Spawner;

            if (augmentations.Contains(AugmentationType.InertDestroyable))
                return Kind.Inert;

            if (augmentations.Contains(AugmentationType.TeslaCoil))
                return Kind.TeslaCoil;

            if (augmentations.Contains(AugmentationType.DestroyableStatelessSwitch))
                return Kind.Switch;

            if (augmentations.Contains(AugmentationType.Shrine) || augmentations.Contains(AugmentationType.StatelessSwitch))
                return Kind.Scenery;

            return null;
        }

        /// <summary>A class's size: medium if it is not in <see cref="Sizes"/>.</summary>
        public static Size SizeOf(EntityClasses classId) => Sizes.TryGetValue((uint)classId, out var size) ? size : Size.Medium;

        /// <summary>The hit points of a size.</summary>
        public static uint HitPointsOf(Size size) => size switch
        {
            Size.Small => SmallHitPoints,
            Size.Large => LargeHitPoints,
            _ => MediumHitPoints
        };

        /// <summary>A placement's hit points: its own, or its class's - a force field's ForceFields.DefaultHealth.</summary>
        public static uint HitPointsOf(Placement placement)
        {
            if (placement.HitPoints > 0)
                return placement.HitPoints;

            return placement.Kind == Kind.ForceField ? ForceFields.DefaultHealth : HitPointsOf(SizeOf(placement.ClassId));
        }

        /// <summary>The state one of a kind stands in, and comes back to.</summary>
        public static UseObjectState UpStateOf(Kind kind, EntityClasses classId) => kind switch
        {
            Kind.Inert or Kind.DestroyedSpawner => UseObjectState.IdesStateIntact,
            Kind.Spawner => UseObjectState.CsStateIdle,
            Kind.TeslaCoil => UseObjectState.StatePowerDown,
            Kind.Switch => UseObjectState.StatePowerUp,
            _ => EntityClassManager.Instance.GetClassInfo(classId)?.Augmentations switch
            {
                { } list when list.Contains(AugmentationType.Shrine) => UseObjectState.ShrineState0,
                { Count: > 0 } => UseObjectState.SsState0,
                // A plain model has no usable to take a state: none is sent (DynamicObjectManager's scenery).
                _ => 0
            }
        };

        /// <summary>
        /// The creature a spawner puts out: the row's; or for a class named for one
        /// (<see cref="SpawnerDefaults"/>) the creature of that kind the map's spawn pools use most,
        /// the class's own fallback if they use none, or the first of the kind. 0 for none.
        /// </summary>
        public static uint CreatureFor(Placement placement)
        {
            if (placement == null || placement.Kind is not (Kind.Spawner or Kind.DestroyedSpawner))
                return 0;

            if (placement.CreatureId != 0)
                return placement.CreatureId;

            if (!SpawnerDefaults.TryGetValue((uint)placement.ClassId, out var spawner))
                return 0;

            var loaded = CreatureManager.Instance.LoadedCreatures;
            bool OfKind(uint id) => loaded.TryGetValue(id, out var creature) && creature != null && (uint)creature.EntityClass == spawner.CreatureClass;

            var used = SpawnPoolManager.Instance.LoadedSpawnPools.Values
                .Where(pool => pool?.MapContextId == placement.MapContextId && pool.SpawnSlot != null)
                .SelectMany(pool => pool.SpawnSlot)
                .Select(slot => slot.CreatureId)
                .Where(OfKind)
                .GroupBy(id => id)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault();

            if (used != 0)
                return used;

            if (OfKind(spawner.FallbackCreatureId))
                return spawner.FallbackCreatureId;

            return loaded.Keys.Where(OfKind).OrderBy(id => id).FirstOrDefault();
        }

        /// <summary>How many come out of a spawner at a time: the row's, its class's, or <see cref="DefaultSpawnCount"/>.</summary>
        public static uint CountFor(Placement placement)
        {
            if (placement.CreatureCount > 0)
                return placement.CreatureCount;

            return SpawnerDefaults.TryGetValue((uint)placement.ClassId, out var spawner) ? spawner.Count : DefaultSpawnCount;
        }

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

                if (Build(mapChannel, placement) != null)
                    placed++;
            }

            return placed;
        }

        /// <summary>
        /// Puts one down on a map channel that is no row and is gone when the channel is: a GM's
        /// look at one (.destructible). Null if the class cannot be placed.
        /// </summary>
        public static Live PutDown(MapChannel mapChannel, EntityClasses classId, Vector3 position, double rotation,
            uint hitPoints = 0, uint creatureId = 0, uint creatureCount = 0, uint side = WorldDestructibleEntry.SideDefault, bool @sealed = false)
        {
            if (mapChannel?.MapInfo == null || WhyNot(classId, out var kind) != null)
                return null;

            return Build(mapChannel, new Placement
            {
                MapContextId = mapChannel.MapInfo.MapContextId,
                ClassId = classId,
                Kind = kind,
                Position = position,
                Rotation = rotation,
                HitPoints = hitPoints,
                CreatureId = creatureId,
                CreatureCount = creatureCount,
                Side = side,
                Sealed = @sealed && kind == Kind.ForceField,
                Comment = "put down by a GM",
                IsPutDown = true
            });
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
            var live = new Live(placement, map)
            {
                MaxHitPoints = HitPointsOf(placement),
                CreatureId = CreatureFor(placement)
            };

            live.CreatureCount = live.CreatureId == 0 ? 0 : CountFor(placement);

            if (placement.Kind == Kind.ForceField)
            {
                var fieldClass = ForceFields.ClassOf(((uint)placement.ClassId).ToString());

                if (fieldClass == null)
                    return null;

                var side = placement.Side == WorldDestructibleEntry.SideAfs ? ForceFields.Side.A : ForceFields.Side.B;

                live.Field = ForceFields.Place(map, fieldClass, side, placement.Position, (float)placement.Rotation, (int)live.MaxHitPoints);
                live.Field.Sealed = placement.Sealed;
                live.Object = live.Field.Object;
            }
            else
            {
                live.Object = new DynamicObject
                {
                    EntityClassId = placement.ClassId,
                    DynamicObjectType = placement.Kind == Kind.Scenery ? DynamicObjectType.Scenery : DynamicObjectType.Destructible,
                    Position = placement.Position,
                    Rotation = placement.Rotation,
                    MapContextId = placement.MapContextId,
                    TargetCategory = TargetCategory.Object,
                    StateId = UpStateOf(placement.Kind, placement.ClassId),
                    CurrentHitPoints = placement.Kind == Kind.Scenery ? 0 : live.MaxHitPoints,
                    IsEnabled = true,
                    IsInWorld = true,
                    ObjectData = live,
                    Comment = placement.ToString()
                };

                CellManager.Instance.AddToWorld(map, live.Object);
            }

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

            if (live.Field != null)
                ForceFields.Remove(live.Field);
            else if (live.Object != null)
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
        public static Live LiveOf(DynamicObject obj)
        {
            if (obj?.ObjectData is Live live)
                return live;

            if (obj?.ObjectData is ForceFields.Field field)
                return On(field.MapChannel).FirstOrDefault(of => of.Field == field);

            return null;
        }

        /// <summary>
        /// A map channel going away (MapChannelManager.CleanupPrivateMapChannel): everything placed
        /// on it out of the world, its force fields out of ForceFields, and the channel forgotten.
        /// Returns how many went.
        /// </summary>
        public static int Forget(MapChannel mapChannel)
        {
            var all = On(mapChannel);

            foreach (var live in all)
                TakeAway(live);

            if (mapChannel != null)
            {
                Channels.Remove(mapChannel);
                NextCatchUps.Remove(mapChannel);
            }

            return all.Count;
        }

        #endregion

        #region Being shot

        /// <summary>
        /// What a client is sent with the object's creation (DynamicObjectManager.CreateDynamicObjectOnClient):
        /// that it is an object, and its hit points - damageable while it stands, which is what makes
        /// it a target.
        /// </summary>
        public static IEnumerable<PythonPacket> EntityData(DynamicObject obj)
        {
            if (obj?.ObjectData is not Live live)
                yield break;

            yield return new TargetCategoryPacket(TargetCategory.Object);
            yield return new DamageInfoPacket(!live.IsDown, false, live.MaxHitPoints, live.HitPoints);
        }

        /// <summary>A placed object on this map channel that can be hit now: there, and standing. Not a force field: ForceFields has those.</summary>
        public static bool TryGetTarget(MapChannel map, ulong entityId, out DynamicObject target)
        {
            target = null;

            if (map == null || !EntityManager.Instance.TryGetObject(entityId, out var candidate) ||
                candidate?.ObjectData is not Live live || live.Field != null || live.Placement.Kind == Kind.Scenery ||
                !ReferenceEquals(live.Map, map) || !ReferenceEquals(candidate.RuntimeMapChannel, map) ||
                !candidate.IsInWorld || live.IsDown)
                return false;

            target = candidate;
            return true;
        }

        /// <summary>Whether an object is one of these (not a force field).</summary>
        public static bool IsOne(DynamicObject obj) => obj?.ObjectData is Live;

        /// <summary>The tesla coils standing on a map channel (TeslaCoils).</summary>
        public static IEnumerable<DynamicObject> StandingCoils(MapChannel map) =>
            On(map).Where(live => live.Placement.Kind == Kind.TeslaCoil && !live.IsDown).Select(live => live.Object);

        /// <summary>
        /// A hit by <paramref name="by"/> (a player's, from PracticeTargetManager.RecordHit; a GM's):
        /// the hit points down, the bar to those in range, the damaged states on the way, and its
        /// destruction when they run out - out of a spawner that spawns when destroyed, its
        /// creatures, after whoever destroyed it. Returns the hit points left.
        /// </summary>
        public static uint Hit(MapChannel map, Actor by, DynamicObject target, int damage)
        {
            if (map == null || damage <= 0 || target?.ObjectData is not Live live || live.Field != null ||
                live.Placement.Kind == Kind.Scenery || !ReferenceEquals(live.Map, map) || live.IsDown)
                return target?.CurrentHitPoints ?? 0;

            var obj = live.Object;
            var shown = obj.StateId;
            var byId = by?.EntityId ?? 0;

            obj.CurrentHitPoints = (uint)damage >= obj.CurrentHitPoints ? 0 : obj.CurrentHitPoints - (uint)damage;
            CellManager.Instance.CellCallMethod(map, obj, new UpdateHitPointsPacket((int)obj.CurrentHitPoints));

            if (obj.CurrentHitPoints > 0)
            {
                // Down to the damaged state its hit points are at, where its class has them.
                if (HasHealthStates(live.Placement.Kind))
                    foreach (var step in DestroyableStates.Down(shown, DestroyableStates.HealthStateFor(obj.CurrentHitPoints, live.MaxHitPoints)))
                        Show(live, step, byId, use: true);

                return obj.CurrentHitPoints;
            }

            // Destroyed: no longer damageable, and so no longer a target; through to its wreck.
            CellManager.Instance.CellCallMethod(map, obj, new DamageInfoPacket(false, false, live.MaxHitPoints, 0));

            switch (live.Placement.Kind)
            {
                case Kind.Inert:
                case Kind.DestroyedSpawner:
                    foreach (var step in DestroyableStates.Down(shown, UseObjectState.IdesState25pHealth))
                        Show(live, step, byId, use: true);

                    // 186 to 2: the explosion, then the wreck.
                    Show(live, UseObjectState.StateDestroyed, byId, use: true);
                    break;

                case Kind.Switch:
                    // Grown, the client has the way to destroyed; still a sprout, it has none.
                    Show(live, UseObjectState.StateDestroyed, byId, use: shown == UseObjectState.StatePowerUp);
                    break;

                default:
                    // A spawner from any of its states, a coil from either of its: straight there.
                    Show(live, UseObjectState.StateDestroyed, byId, use: true);
                    break;
            }

            // A coil's wreck is no target: the client takes a coil's targetability from SetUsable
            // alone (teslacoil.py OnEnabledChanged), not from its state.
            if (live.Placement.Kind == Kind.TeslaCoil)
                DynamicObjectManager.Instance.SetEnabled(obj, false);

            live.Stage = Stage.Idle;
            live.NextAt = 0;
            live.RespawnAt = RespawnAtFor(map, Now());

            if (live.Placement.Kind == Kind.DestroyedSpawner)
                Release(live, by as Manifestation);

            // A Caves of Donn egg cluster: Bugged to Death (CavesOfDonn).
            if (!live.Placement.IsPutDown)
                CavesOfDonn.Destroyed(map, live.Placement.Id);

            // A Bane base's databank: its alarm (BaneAlarms).
            BaneAlarms.Destroyed(map, live, by);

            // A mission's count of what the player destroys - "Destroy 8 warnet nests" - shared
            // with their squad as a kill is (GroupCreditService).
            if (by is Manifestation destroyer && GameEffectManager.ClientOf(map, destroyer) is { } client)
                Missions?.Credit.RecordKill(client,
                    new[] { MissionProgressEvent.ObjectDestroyed((uint)obj.EntityClassId) }, obj.Position);

            return 0;
        }

        /// <summary>
        /// When something destroyed now is back: RespawnMinMs to RespawnMaxMs on, on an ordinary
        /// map. In a squad's instance, the instance's own respawn (SquadInstanceState): what has
        /// been destroyed stays destroyed as long as what has been killed stays killed, and for
        /// good, until the instance is closed, where that respawn is off - a camera shot out or a
        /// nest smashed on the way in is not back a minute later, as it was.
        /// </summary>
        public static long RespawnAtFor(MapChannel map, long now)
        {
            if (map?.SquadState is { } squad)
                return squad.RespawnMs > 0 ? now + squad.RespawnMs : long.MaxValue;

            return now + Math.Clamp(Roll(RespawnMinMs, RespawnMaxMs), RespawnMinMs, RespawnMaxMs);
        }

        /// <summary>Takes it down at once, as if shot down by <paramref name="by"/> (.destructible kill). False if it is down already.</summary>
        public static bool Destroy(Live live, Actor by)
        {
            if (live == null || live.IsDown || live.Placement.Kind == Kind.Scenery)
                return false;

            if (live.Field != null)
                return ForceFields.Damage(live.Field, live.Field.Health, by?.EntityId ?? 0) > 0;

            return Hit(live.Map, by, live.Object, (int)live.Object.CurrentHitPoints) == 0;
        }

        private static bool HasHealthStates(Kind kind) => kind is Kind.Inert or Kind.DestroyedSpawner;

        /// <summary>
        /// A row's force field down for good in one copy of its map (MapChannel.IsCopy: a squad's,
        /// a private or a shared instance), the others keeping theirs, as TorcastraPrison opens its
        /// gate: unsealed, its hit points to none, and never due back while the copy stands (a
        /// copy made again has it up again, until <see cref="CatchUp"/> takes it down). A
        /// mission's MissionActionKind.TakeDownField. False where nothing is taken down: on a
        /// channel that is no copy (logged: the open world's stay up for everyone); on another
        /// map than the row's - the player moved on before the transition ran, and CatchUp has it
        /// when they are back (Debug); where the channel has no force field of the row (logged).
        /// </summary>
        public static bool TakeDown(MapChannel map, uint rowId, ulong byEntityId)
        {
            if (map != null && !map.IsCopy)
            {
                Logger.WriteLog(LogType.Error,
                    $"world_destructible {rowId}: warning, map {map.MapInfo?.MapContextId.ToString() ?? "none"} is no copy; a sealed field is taken down only in a squad's copy, so nothing is.");
                return false;
            }

            if (map?.MapInfo != null && _placements.FirstOrDefault(placement => placement.Id == rowId) is { } row &&
                row.MapContextId != map.MapInfo.MapContextId)
            {
                Logger.WriteLog(LogType.Debug,
                    $"world_destructible {rowId}: on map {row.MapContextId}, not {map.MapInfo.MapContextId}; taken down when a player who has been through it is in a copy of its map.");
                return false;
            }

            var live = LiveOf(map, rowId);

            if (live?.Field is not { } field)
            {
                Logger.WriteLog(LogType.Error,
                    $"world_destructible {rowId}: no force field of it on map {map?.MapInfo?.MapContextId.ToString() ?? "none"} to take down{(live == null ? "" : $" ({live.Placement.Kind})")}.");
                return false;
            }

            field.Sealed = false;

            if (!field.IsDestroyed)
                ForceFields.Damage(field, field.Health, byEntityId);

            live.RespawnAt = long.MaxValue;
            return true;
        }

        #endregion

        #region Coming back, and spawning

        /// <summary>
        /// Every second or so: what is due back comes back, force fields that are down are given a
        /// time to come back, and the creature spawners go through their spawning.
        /// </summary>
        public static void Worker(MapChannel map)
        {
            var lives = On(map);

            if (lives.Count == 0)
                return;

            var now = Now();

            CatchUp(map, lives, now);

            foreach (var live in lives)
            {
                if (live.Placement.Kind == Kind.Scenery)
                    continue;

                live.Brood.RemoveAll(creature => !Alive(creature));

                if (live.IsDown)
                {
                    // A force field ForceFields took down: when it is due back.
                    if (live.RespawnAt == 0)
                        live.RespawnAt = RespawnAtFor(map, now);
                    else if (now >= live.RespawnAt)
                        Respawn(live);

                    continue;
                }

                if (live.Placement.Kind == Kind.Spawner)
                    Spawning(live, now);
            }
        }

        /// <summary>
        /// A copy made again has its sealed fields up, as every copy does: a squad that has taken
        /// one down before has it down again as soon as one of them is in, as TorcastraPrison
        /// opens its gate again (CatchUp). Once every <see cref="CatchUpMs"/>, on a copy with a
        /// sealed field standing, for each player in it: a field whose row a TakeDownField action
        /// of their missions names, on a transition they have been through, goes down
        /// (<see cref="TakeDown"/>). The open world's are left alone.
        /// </summary>
        internal static void CatchUp(MapChannel map, List<Live> lives, long now)
        {
            if (map?.IsCopy != true)
                return;

            var next = NextCatchUps.GetOrCreateValue(map);

            if (now < next.Value)
                return;

            next.Value = now + CatchUpMs;

            var standing = lives.Where(live => live.Field != null && live.Placement.Sealed && !live.Placement.IsPutDown && !live.IsDown).ToList();

            if (standing.Count == 0)
                return;

            var missions = Missions?.LoadedMissions;

            if (missions == null)
                return;

            foreach (var client in map.ClientList.ToList())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || !ReferenceEquals(player.MapChannel, map))
                    continue;

                foreach (var live in standing.Where(live => !live.IsDown))
                    if (HasTakenDown(player, missions, live.Placement.Id))
                    {
                        Logger.WriteLog(LogType.Debug,
                            $"{live.Placement} on map {live.Placement.MapContextId}: down again, {player.FamilyName}'s mission has taken it down.");
                        TakeDown(map, live.Placement.Id, player.EntityId);
                    }
            }
        }

        /// <summary>
        /// Whether one of the player's missions, active or done, has a transition with a
        /// TakeDownField action for the row that the player has been through: its objective is in
        /// the transition's state (completed, if it names none), or the mission is done and the
        /// transition completes its objective.
        /// </summary>
        public static bool HasTakenDown(Manifestation player, IReadOnlyDictionary<uint, Mission> missions, uint rowId)
        {
            if (player == null || missions == null || !TakeDownsOf(missions).TryGetValue(rowId, out var transitions))
                return false;

            foreach (var (missionId, objectiveId, to) in transitions)
                if ((player.Missions.ContainsKey(missionId) || player.MissionHistory.ContainsKey(missionId)) &&
                    HasBeenThrough(player, missionId, objectiveId, to))
                    return true;

            return false;
        }

        /// <summary>The transitions that take a row's field down, by row: built once for each set of loaded missions.</summary>
        private sealed class TakeDownIndex
        {
            public TakeDownIndex(IReadOnlyDictionary<uint, Mission> missions, IReadOnlyDictionary<uint, List<(uint, uint, MissionObjectiveState)>> rows)
            {
                Missions = missions;
                Rows = rows;
            }

            public IReadOnlyDictionary<uint, Mission> Missions { get; }
            public IReadOnlyDictionary<uint, List<(uint MissionId, uint ObjectiveId, MissionObjectiveState To)>> Rows { get; }
        }

        private static volatile TakeDownIndex _takeDowns;

        /// <summary>How many times the index was built; a test's to read.</summary>
        internal static int TakeDownIndexBuilds;

        /// <summary>
        /// Each row a TakeDownField action names, with the mission, objective and objective state of
        /// each transition that carries one (completed, where the transition names none). Built the
        /// first time it is asked for a set of loaded missions, and again for another set.
        /// </summary>
        internal static IReadOnlyDictionary<uint, List<(uint MissionId, uint ObjectiveId, MissionObjectiveState To)>> TakeDownsOf(
            IReadOnlyDictionary<uint, Mission> missions)
        {
            if (_takeDowns is { } built && ReferenceEquals(built.Missions, missions))
                return built.Rows;

            var rows = new Dictionary<uint, List<(uint, uint, MissionObjectiveState)>>();

            foreach (var mission in missions.Values.Where(mission => mission != null))
                foreach (var objective in mission.Objectives.Values)
                    foreach (var transition in objective.GetExecutableTransitionsOrLegacyDefault())
                        foreach (var action in transition.Actions)
                            if (action.Kind == MissionActionKind.TakeDownField &&
                                action.ItemIntent is global::Rasa.Missions.Scenes.TakeDownFieldIntent takeDown)
                            {
                                if (!rows.TryGetValue(takeDown.DestructibleId, out var list))
                                    rows[takeDown.DestructibleId] = list = new List<(uint, uint, MissionObjectiveState)>();

                                list.Add((mission.MissionId, objective.ObjectiveId, transition.ToState ?? MissionObjectiveState.Completed));
                            }

            Interlocked.Increment(ref TakeDownIndexBuilds);
            var index = new TakeDownIndex(missions, rows.ToDictionary(entry => entry.Key, entry => entry.Value));
            _takeDowns = index;
            return index.Rows;
        }

        private static bool HasBeenThrough(Manifestation player, uint missionId, uint objectiveId, MissionObjectiveState to)
        {
            if (player.Missions.TryGetValue(missionId, out var log))
            {
                if (log.Objectives.TryGetValue(objectiveId, out var objective) && Reached(objective.State, to))
                    return true;

                return to == MissionObjectiveState.Completed && log.State is MissionState.Completed or MissionState.Success;
            }

            return to == MissionObjectiveState.Completed &&
                player.MissionHistory.TryGetValue(missionId, out var state) && state is MissionState.Completed or MissionState.Success;
        }

        /// <summary>An objective's state is the transition's, or later: revealed, then active, then completed or failed.</summary>
        private static bool Reached(MissionObjectiveState state, MissionObjectiveState to) => to switch
        {
            MissionObjectiveState.NotAssigned => state is MissionObjectiveState.NotAssigned or MissionObjectiveState.Incomplete or
                MissionObjectiveState.Completed or MissionObjectiveState.Failed,
            MissionObjectiveState.Incomplete => state is MissionObjectiveState.Incomplete or MissionObjectiveState.Completed or MissionObjectiveState.Failed,
            _ => state == to
        };

        /// <summary>Back at full hit points, in the state it stands in.</summary>
        public static void Respawn(Live live)
        {
            if (live == null)
                return;

            live.RespawnAt = 0;
            live.Stage = Stage.Idle;
            live.NextAt = 0;

            if (live.Field != null)
            {
                if (live.Placement.Sealed)
                    live.Field.Sealed = true;

                ForceFields.Repair(live.Field, 0);
                return;
            }

            var obj = live.Object;
            var up = UpStateOf(live.Placement.Kind, live.Placement.ClassId);

            obj.CurrentHitPoints = live.MaxHitPoints;

            if (live.Placement.Kind == Kind.Switch)
            {
                // A sprout, which grows: the client has no way from destroyed.
                Show(live, UseObjectState.StatePowerDown, 0, use: false);
                Show(live, UseObjectState.StatePowerUp, 0, use: true);
            }
            else
                // Destroyed to intact, to idle, to POWER_DOWN: each class has the way.
                Show(live, up, 0, use: true);

            // A coil a target again, once it is out of its destroyed state (OnEnabledChanged asks).
            if (live.Placement.Kind == Kind.TeslaCoil)
                DynamicObjectManager.Instance.SetEnabled(obj, true);

            CellManager.Instance.CellCallMethod(live.Map, obj, new DamageInfoPacket(true, false, live.MaxHitPoints, live.MaxHitPoints));
            CellManager.Instance.CellCallMethod(live.Map, obj, new UpdateHitPointsPacket((int)live.MaxHitPoints));
        }

        /// <summary>A creature spawner (61) set off, on its way through it, or resting after.</summary>
        private static void Spawning(Live live, long now)
        {
            if (live.CreatureId == 0)
                return;

            switch (live.Stage)
            {
                case Stage.Idle:
                    var player = Near(live);

                    if (player == null)
                        return;

                    live.SetOffBy = player.EntityId;
                    live.Stage = Stage.Begin;
                    live.NextAt = now + SpawnerBeginMs;
                    Show(live, UseObjectState.CsStateBegin, player.EntityId, use: true);
                    return;

                case Stage.Begin when now >= live.NextAt:
                    live.Stage = Stage.Spawn;
                    live.NextAt = now + SpawnerSpawnMs;
                    Show(live, UseObjectState.CsStateSpawn, live.SetOffBy, use: true);
                    Spawn(live, EntityManager.Instance.Players.TryGetValue(live.SetOffBy, out var target) ? target : null);
                    live.SpawnedAt = now;
                    return;

                case Stage.Spawn when now >= live.NextAt:
                    live.Stage = Stage.End;
                    live.NextAt = now + SpawnerEndMs;
                    Show(live, UseObjectState.CsStateEnd, live.SetOffBy, use: true);
                    return;

                case Stage.End when now >= live.NextAt:
                    live.Stage = Stage.Resting;
                    Show(live, UseObjectState.CsStateIdle, live.SetOffBy, use: true);
                    return;

                case Stage.Resting when now >= live.SpawnedAt + SpawnerRearmMs && live.Brood.Count == 0:
                    live.Stage = Stage.Idle;
                    return;
            }
        }

        /// <summary>A spawner that spawns when destroyed has gone: its creatures, after whoever did it.</summary>
        private static void Release(Live live, Manifestation by)
        {
            if (live.CreatureId != 0)
                Spawn(live, by);
        }

        private static void Spawn(Live live, Manifestation target)
        {
            for (var i = 0; i < live.CreatureCount; i++)
            {
                var creature = Hatch(live.Map, live.Object, live.CreatureId, target);

                if (creature != null)
                    live.Brood.Add(creature);
            }

            Logger.WriteLog(LogType.Debug,
                $"{live.Placement} on map {live.Placement.MapContextId}: {live.Brood.Count} of creature {live.CreatureId} out, after {target?.FamilyName ?? "nobody"}.");
        }

        /// <summary>A living player near enough a spawner to set it off, if there is one.</summary>
        private static Manifestation Near(Live live)
        {
            var origin = live.Object.Position;

            foreach (var client in live.Map.ClientList.ToList())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || !ReferenceEquals(player.MapChannel, live.Map) ||
                    player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                    continue;

                if (player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
                    continue;

                var ground = Vector2.Distance(new Vector2(player.Position.X, player.Position.Z), new Vector2(origin.X, origin.Z));

                if (ground <= SpawnerTriggerRadius && Math.Abs(player.Position.Y - origin.Y) <= SpawnerTriggerHeight)
                    return player;
            }

            return null;
        }

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        #endregion

        /// <summary>Its state, to everyone in range: by Use where the client has the way from the state it was in (its transition, and so its effect), ForceState where it has not.</summary>
        private static void Show(Live live, UseObjectState state, ulong actorId, bool use)
        {
            live.Object.StateId = state;
            CellManager.Instance.CellCallMethod(live.Map, live.Object, use
                ? new UsePacket(actorId, state, 0)
                : new ForceStatePacket(state, 0));
        }
    }
}
