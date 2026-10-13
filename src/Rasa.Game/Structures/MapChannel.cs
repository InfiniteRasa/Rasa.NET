using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class MapChannel
    {
        internal System.Guid MissionEpoch { get; } = System.Guid.NewGuid();
        // ToDo
        public MapInfo MapInfo { get; set; }
        public uint InstanceId { get; set; } = 1;
        public bool IsPrivateInstance { get; set; }
        public uint OwnerCharacterId { get; set; }

        /// <summary>
        /// A further copy of a map that anyone may enter (MapChannelManager.Instances): opened
        /// when the copies before it are full, and nobody's. Unlike a private instance it is a
        /// public place in every way but one: it is not the map's own channel.
        /// </summary>
        public bool IsSharedInstance { get; set; }

        /// <summary>
        /// A squad's own copy of a map (MapChannelManager's squad instances): made when its owner
        /// or one of their squad walks in, and kept until the weekly reset. A public place to
        /// those in it, as a shared copy is - not the one-owner place a private instance is.
        /// </summary>
        public bool IsSquadInstance { get; set; }

        /// <summary>Whose a squad instance is: the character of the squad's leader, or of a player in no squad. 0 for every other channel.</summary>
        public uint SquadOwnerCharacterId { get; set; }

        /// <summary>A squad instance is not closed before this (the manager's clock): somebody is being sent into it.</summary>
        internal long HeldUntil { get; set; }

        /// <summary>What a squad instance keeps of itself and saves (Managers.SquadInstanceState); null for every other channel.</summary>
        public Managers.SquadInstanceState SquadState { get; set; }

        /// <summary>Whether this is a copy of a map - somebody's, a squad's, or shared - rather than the map's own channel.</summary>
        public bool IsCopy => IsPrivateInstance || IsSharedInstance || IsSquadInstance;

        /// <summary>When a shared copy was last seen with nobody in it or on the way (Environment.TickCount64 as the manager reads it); 0 while it has somebody.</summary>
        public long EmptySince { get; set; }

        /// <summary>The players sent to this channel whose clients have not arrived yet: they count towards how full it is.</summary>
        internal HashSet<Client> Arriving { get; } = new HashSet<Client>();
        // timers
        //public int TimerClientEffectUpdate { get; set; }
        //public int TimerMissileUpdate { get; set; }
        //public int TimerDynObjUpdate { get; set; }
        public long MapChannelElapsed { get; set; }

        /// <summary>When each once-a-second worker last ran on this map, by the manager's clock (MapChannelManager.SinceLastRun).</summary>
        internal Dictionary<string, long> WorkerRanAt { get; } = new Dictionary<string, long>();

        /// <summary>The tick at which this channel's sky started running: when the channel was made (Managers.SkyClock).</summary>
        internal long SkyStartedTick { get; set; } = System.Environment.TickCount64;
        /// <summary>Milliseconds since this map's creatures last ran BehaviorManager.CreatureThink.</summary>
        public long ControllerElapsed { get; set; }
        //public int TimerPlayerUpdate { get; set; }
        // player
        public int PlayerLimit { get; set; }
        public List<Client> ClientList { get; set; }
        // queue
        public readonly Queue<Client> QueuedClients = new Queue<Client>();
        // action
        public readonly List<ActionData> PerformRecovery = new List<ActionData>();
        // cell
        public MapCellInfo MapCellInfo = new MapCellInfo();

        /// <summary>The map's navmesh, or null when no navmesh/&lt;map&gt;.nav was built for it. See NavMeshManager.</summary>
        public Navigation.NavMeshQuery NavMesh { get; set; }

        /// <summary>
        /// The height below which a player has fallen out of the world and is put back
        /// (Managers.SafetyFloor); null when the map has no navmesh. Set with the navmesh.
        /// </summary>
        public float? SafetyFloorY { get; set; }

        /// <summary>The highest walkable surface on the map, from its navmesh; null without one.</summary>
        public float? TopWalkableY { get; set; }

        /// <summary>What stands between two points on the map, for cover; null when no navmesh/&lt;map&gt;.cover was built. See Managers.Cover.</summary>
        public Navigation.CoverMesh Cover { get; set; }
        // effect
        public int CurrentEffectId { get; set; } // increases with every spawned game effect

        /// <summary>
        /// Every actor on the map with at least one effect on it - players and creatures alike -
        /// so the effect worker walks only those rather than every creature on the map.
        /// GameEffectManager keeps it.
        /// </summary>
        public readonly HashSet<Actor> ActorsWithEffects = new HashSet<Actor>();

        /// <summary>
        /// Reused copies of the cell table for the passes that walk every cell while something
        /// under them may add a cell (GetCell creates the ones it is asked for): the creature
        /// think every 250 ms, and the creature armour regeneration every second. Each used to
        /// copy the table afresh - sixteen bytes a cell, over the large object heap's threshold
        /// once a map has touched five thousand cells, four times a second per map. The loop
        /// thread runs one pass at a time, so one buffer each is enough.
        /// </summary>
        internal readonly List<MapCell> ThinkCells = new List<MapCell>();
        internal readonly List<MapCell> RegenCells = new List<MapCell>();

        // Dynamic Object List
        public List<DynamicObject> DynamicObjects = new List<DynamicObject>();

        /// <summary>Spawn pools configured for this concrete map instance.</summary>
        public List<SpawnPool> SpawnPools = new List<SpawnPool>();

        // Dictionary<uniqueControlPointId, dataAboutdynamicObject> ControlPoints
        public Dictionary<uint, DynamicObject> ControlPoints = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueFootlockerId, dataAboutdynamicObject> Footlockers
        public Dictionary<uint, DynamicObject> FootLockers = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueTeleporterId, dataAboutdynamicObject> Teleporters
        public Dictionary<uint,DynamicObject> Teleporters = new Dictionary<uint, DynamicObject>();

        /// <summary>Crafting stations by kraftwerks row id; see KraftwerksManager.</summary>
        public Dictionary<uint, DynamicObject> Kraftwerks = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueLootDispenserId, dataAboutLootDispenser> LootDispensers
        public Dictionary<ulong, LootDispenser> LootDispensers = new Dictionary<ulong, LootDispenser>();
        /// <summary>
        /// Protects loot dispensers, corpse lifetime decisions, and looter state. When both are
        /// needed, acquire Client.SyncRoot before this lock; never acquire a client lock while
        /// holding this one.
        /// </summary>
        internal object LootSyncRoot { get; } = new object();

        // Missiles on this mapChannel
        public List<Missile> QueuedMissiles = new List<Missile>();

        /// <summary>
        /// The Practice Dummies a hit has knocked back, by entity id, and the tick each is up
        /// again (PracticeTargetManager.Swing). Locked on itself.
        /// </summary>
        internal readonly Dictionary<ulong, long> PracticeSwings = new Dictionary<ulong, long>();
    }
}
