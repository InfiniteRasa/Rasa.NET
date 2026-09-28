using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Models;

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
        public ActionFollow ActionFollow = new ActionFollow();
        internal ScriptedMove ScriptedMove { get; set; }
        internal Movement LastMovement { get; set; }
        //public long[] ActionLockTime { get; set; }
    }

    internal sealed class ScriptedMove
    {
        internal Vector3 Destination { get; init; }
        internal double Orientation { get; init; }
        internal bool Arrived { get; set; }
    }

    public class ActionFighting
    {
        public Vector3 LockedTargetPosition = new Vector3();    // the creature position we are pathing to
        public ulong TargetEntityId { get; set; }
    }

    /// <summary>
    /// Where a minion is meant to be when it is not doing something else.
    ///
    /// The client's Command System help calls this an *anchor point*: "Players can assign an
    /// anchor point to their subordinate with the Go/Stay command; this will tell the subordinate
    /// to return to this location after it completes an action." Go sets it to a picked location,
    /// Stay sets it to wherever the minion is standing, and Follow Me clears it so the minion
    /// regroups on its master again.
    ///
    /// Exactly one of the two is in force: with an anchor the minion returns to a fixed point,
    /// without one it trails <see cref="FollowTargetId"/> - normally its master, or another player
    /// after a Follow Target order.
    /// </summary>
    public class ActionFollow
    {
        /// <summary>The entity this minion trails when it has no anchor. 0 means nothing to follow.</summary>
        public ulong FollowTargetId { get; set; }

        /// <summary>
        /// Who this minion is assisting, or 0. Assist is not movement: "assist mode will
        /// automatically issue a Target command whenever the player attacks an enemy so that the
        /// subordinate is targeting the same enemy", so it is a standing instruction to copy
        /// someone else's target, held separately from whatever the minion is following.
        /// </summary>
        public ulong AssistTargetId { get; set; }

        public bool HasAnchor { get; set; }

        public Vector3 Anchor = new Vector3();

        /// <summary>Throttles repathing while chasing a target that is itself moving.</summary>
        public long PathUpdateTime { get; set; }
        internal bool CatchUpRunning { get; set; }
        internal Creature OwnerAttackTarget { get; set; }
    }

    public class ActionWander
    {
        public byte State { get; set; }
        public Vector3 WanderDestination = new Vector3();

        /// <summary>How long this idle spell lasts before the next stroll; drawn anew each time the creature stops.</summary>
        public long RestDuration { get; set; }
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
