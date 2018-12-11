using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    public class SpawnPool
    {
        public int DbId { get; set; }         // id of the spawnpool

        public Vector3 HomePosition { get; set; }
        public Quaternion HomeRotation { get; set; }
        public List<SpawnPoolSlot> SpawnSlot { get; set; }
        // different spawn points
        //public int LocationCount { get; set; }
        //public Position[] LocationList { get; set; }
        // currently the DB structure only supports 1 spawn location, but the spawn system code already allows for multiple
        // spawn settings
        public short Mode { get; set; }     // automatic spawning, CP spawn, scripted spawn (manual trigger)
        public short AnimType { get; set; } // which effect is used to spawn creatures (bane dropship, no effect, human dropship)   // ToDo
        public int ContextId { get; set; }
        // spawn runtime info
        public int DropshipQueue { get; set; } // number of dropships that are currently delivering units
        public int QueuedCreatures { get; set; } // number of creatures that are spawning right now (i.e. delivered via dropship)
        public int AliveCreatures { get; set; } // number of spawned creatures that are alive
        public int DeadCreatures { get; set; }  // number of spawned creatures that are dead (either killed or spawned dead)

        // respawn lock
        public long UpdateTimer { get; set; }
        public int RespawnTime { get; set; }
        

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
