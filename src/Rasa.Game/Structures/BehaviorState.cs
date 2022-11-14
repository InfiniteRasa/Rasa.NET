using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;

    public class BehaviorState
    {
        public long DeadTime { get; set; } // amount of time that has passed since the actor died
        public byte CurrentAction { get; set; }
        public Factions Faction { get; set; }
        // combat info
        public long TimerPathUpdateLock = 5000; // avoids path-update-spamming for permanently moving units => ToDo: see to we need to increase or decrease value
        // path info
        public List<Vector3> Path = new List<Vector3>(); // calculate path nodes
        // maybe we can optimize this to not waste as much memory?
        public int PathIndex { get; set; }     // the path node we are currently at
        public AiPathFollowing AiPathFollowing = new AiPathFollowing();
        public ActionFighting ActionFighting = new ActionFighting();
        public ActionWander ActionWander = new ActionWander();
        //public long[] ActionLockTime { get; set; }
    }

    public class ActionFighting
    {
        public Vector3 LockedTargetPosition = new Vector3();    // the creature position we are pathing to
        public ulong TargetEntityId { get; set; }
    }

    public class ActionWander
    {
        public byte State { get; set; }
        public Vector3 WanderDestination = new Vector3();
    }
    
    public class AiPathFollowing
    {
        // ai path following (general path, not the current path)
        public AiPath GeneralPath { get; set; }
        public int GeneralPathCurrentNodeIndex { get; set; }
        public float[] RandomPathNodeBiasXZ = new float[2];
    }

    public class AiPath
    {
        public uint PathId { get; set; }
        public uint Spawnpool { get; set; } // non-zero means the references spawnpool should use this path
        public byte Mode { get; set; }
        public float NodeOffsetRandomization { get; set; } // how far away can creatures pass a path node
        public int NumberOfPathNodes { get; set; }
        public AiPathNode[] PathNodeList { get; set; }
    }

    public class AiPathNode
    {
        public float[] Pos = new float[3];
        // maybe add flags? And a waittime?
    }
}
