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
        // timers
        //public int TimerClientEffectUpdate { get; set; }
        //public int TimerMissileUpdate { get; set; }
        //public int TimerDynObjUpdate { get; set; }
        public long MapChannelElapsed { get; set; }
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
    }
}
