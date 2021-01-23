namespace Rasa.Structures
{
    using Data;
    using Managers;

    public class BehaviorState
    {
        public long DeadTime { get; set; } // amount of time that has passed since the actor died
        public byte CurrentAction { get; set; }
        public Factions Faction { get; set; }
        // combat info
        public long TimerPathUpdateLock = 5000; // avoids path-update-spamming for permanently moving units => ToDo: see to we need to increase or decrease value
        // path info
        public float[] Path = new float[3 * BehaviorManager.PathLengthLimit]; // calculate path nodes
        // maybe we can optimize this to not waste as much memory?
        public uint PathIndex { get; set; }     // the path node we are currently at
        public uint PathLength { get; set; }    // how many nodes the current path has, 0 means no active path
        public AiPathFollowing AiPathFollowing = new AiPathFollowing();
        public ActionFighting ActionFighting = new ActionFighting();
        public ActionWander ActionWander = new ActionWander();
        //public long[] ActionLockTime { get; set; }
    }

    public class ActionFighting
    {
        public float[] LockedTargetPosition = new float[3];    // the creature position we are pathing to
        public ulong TargetEntityId { get; set; }
    }

    public class ActionWander
    {
        public byte State { get; set; }
        public float[] WanderDestination = new float[3];
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
