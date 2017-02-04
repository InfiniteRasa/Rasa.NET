namespace Rasa.Structures
{
    public class SpawnPool
    {
        public int id { get; set; }         // id of the spawnpool
        public SpawnPoolSlot[] SpawnSlot { get; set; }
        // different spawn points
        public int LocationCount { get; set; }
        public Position[] LocationList { get; set; }
        // currently the DB structure only supports 1 spawn location, but the spawn system code already allows for multiple
        // spawn settings
        public short Mode { get; set; }     // automatic spawning, CP spawn, scripted spawn (manual trigger)
        public short AnimType { get; set; } // which effect is used to spawn creatures (bane dropship, no effect, human dropship)   // ToDo
        // spawn runtime info
        //sint32 dropshipQueue; // number of dropships that are currently delivering units
        //sint32 queuedCreatures; // number of creatures that are spawning right now (i.e. delivered via dropship)
        public int AliveCreatures { get; set; } // number of spawned creatures that are alive
        public int DeadCreatures { get; set; }  // number of spawned creatures that are dead (either killed or spawned dead)

        // respawn lock
        public int SpawnLockTime { get; set; }
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
