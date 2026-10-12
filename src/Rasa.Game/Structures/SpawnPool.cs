using Rasa.Structures.Interfaces;
using Rasa.Structures.World;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    public class SpawnPool : IHasPosition
    {
        public uint DbId { get; set; }         // id of the spawnpool

        public Vector3 Position { get; set; }
        public double Rotation { get; set; }

        /// <summary>
        /// How far from Position its creatures may stand. Zero: the old two units of scatter.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>How its creatures stand at their post (spawnpool_pose; NpcPoses); None for a pool with no pose.</summary>
        public Data.NpcPose Pose { get; set; }

        /// <summary>The beat its creature walks (spawnpool_patrol; Managers.Patrols); null for a pool with none.</summary>
        public IReadOnlyList<PatrolStep> Patrol { get; set; }
        public List<SpawnPoolSlot> SpawnSlot { get; set; }

        /// <summary>Where its creatures arrive (spawnpool_arrival): a pad or bay the Bane dropship lands on, a teleporter. Empty: they appear on its ground.</summary>
        public List<World.SpawnPoolArrivalEntry> Arrivals { get; } = new List<World.SpawnPoolArrivalEntry>();

        /// <summary>
        /// Whether it has spawned since the server started - or, for a control point's garrison,
        /// since its side took the point: the first spawn stocks the world and uses no arrival point.
        /// </summary>
        public bool HasSpawned { get; set; }

        /// <summary>Part of a control point's garrison, either side's (ControlPoints): a Bane garrison pool (mode 1) runs only as one.</summary>
        public bool IsGarrison { get; set; }

        /// <summary>The other side holds its control point (ControlPoints): it spawns nothing until its own side does.</summary>
        public bool Suspended { get; set; }

        /// <summary>
        /// Its area has no walkable ground on its map's navmesh (SpawnPoolManager.Anchor), so it
        /// spawns nothing: what it put down stood in the air or under the ground, could go
        /// nowhere, and still shot. Decided on the first pass and logged once.
        /// </summary>
        public bool NoWalkableGround { get; set; }
        // different spawn points
        //public int LocationCount { get; set; }
        //public Position[] LocationList { get; set; }
        // currently the DB structure only supports 1 spawn location, but the spawn system code already allows for multiple
        // spawn settings
        public short Mode { get; set; }     // automatic spawning, CP spawn, scripted spawn (manual trigger)
        public short AnimType { get; set; } // which effect is used to spawn creatures (bane dropship, no effect, human dropship)   // ToDo
        public uint MapContextId { get; set; }
        public MapChannel RuntimeMapChannel { get; set; }
        // spawn runtime info
        public int DropshipQueue { get; set; } // number of dropships that are currently delivering units
        public int QueuedCreatures { get; set; } // number of creatures that are spawning right now (i.e. delivered via dropship)
        public int AliveCreatures { get; set; } // number of spawned creatures that are alive
        public int DeadCreatures { get; set; }  // number of spawned creatures that are dead (either killed or spawned dead)
        internal List<Creature> QueuedCreatureList { get; set; }

        /// <summary>
        /// Its emplacements that are wrecks on their mounts (AlternateMesh): dead, kept in the
        /// world, and put back in service when the respawn comes round in place of new ones.
        /// </summary>
        internal List<Creature> Wrecks { get; } = new List<Creature>();
        public string ScenarioKey { get; set; }
        public string SceneRunId { get; set; }
        public string SceneActorRole { get; set; }
        public string SceneSharedKey { get; set; }
        public uint SceneGeneration { get; set; }
        internal global::Rasa.Missions.Scenes.SceneSpawnPose ScenePose { get; set; }
        public uint ScenarioMissionId { get; set; }
        public uint? ScenarioGroupId { get; set; }
        public string ScenarioAttemptKey { get; set; }
        public uint ScenarioOwnerCharacterId { get; set; }
        public MissionSpawnGroupPolicy SpawnPolicy { get; set; }
        public uint FollowOwnerCharacterId { get; set; }
        public ulong FollowTargetEntityId { get; set; }

        /// <summary>
        /// In a squad's instance: when the last of its creatures died, Unix milliseconds, UTC; 0
        /// while it has them, or has never been cleared (Managers.SquadInstanceState).
        /// </summary>
        public long ClearedAtUtcMs { get; set; }

        // Runtime milliseconds; the persisted respawn time (spawnpool.respown_time) is in tenths of a second.
        public long UpdateTimer { get; set; }
        public long RespawnTime { get; set; }
        

        // paths
        //sint32 pathCount;
        //aiPath_t** pathList;

        //baseBehavior_baseNode *pathnodes;
        //sint32 attackspeed;
        //sint32 attackanim;
        //float velocity;
        //sint32 attackstyle;
        //sint32 actionid;
        // default action assignment
        // nothing here until 

    }
}
