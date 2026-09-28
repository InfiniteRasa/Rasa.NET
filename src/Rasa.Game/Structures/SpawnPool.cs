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
        public List<SpawnPoolSlot> SpawnSlot { get; set; }
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

        // Runtime milliseconds; the persisted RespawnTime is in seconds.
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
