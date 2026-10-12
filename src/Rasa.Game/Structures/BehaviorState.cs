using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Models;

    public class BehaviorState
    {
        /// <summary>When the ability the creature is winding up is done winding up (CreatureWindups): until then it does nothing else.</summary>
        public long WindupUntil { get; set; }

        public long DeadTime { get; set; } // amount of time that has passed since the actor died
        public byte CurrentAction { get; set; }
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
        public ActionReturning ActionReturning = new ActionReturning();
        public ActionPatrol ActionPatrol = new ActionPatrol();
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

        /// <summary>Whom the last line-of-sight test was against, and whether it was clear (BehaviorManager.HasLineOfSight).</summary>
        public ulong SightTargetId { get; set; }
        public bool SightClear { get; set; }

        /// <summary>Milliseconds until the line of sight is tested again.</summary>
        public long SightRecheckIn { get; set; }

        /// <summary>Whether the creature has made its first attack of this fight (BehaviorManager.OpensNow).</summary>
        public bool Opened { get; set; }

        /// <summary>How many thinks it has had an attack ready this fight and held it (BehaviorManager.OpensNow).</summary>
        public int OpeningRolls { get; set; }

        /// <summary>Milliseconds of this fight the creature has stood free to act and neither struck nor gained ground (BehaviorManager.Stalled).</summary>
        public long StalledMs { get; set; }
    }

    /// <summary>
    /// A creature that chased too far from home and is running back to it (BehaviorManager.Leash):
    /// it takes no damage and picks no fight on the way, and is whole again when it arrives.
    /// </summary>
    public class ActionReturning
    {
        /// <summary>How long it has been running back.</summary>
        public long Elapsed { get; set; }

        /// <summary>When it is put home however far it got - off the navmesh, stuck on a rock.</summary>
        public long TimeoutMs { get; set; }
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

    /// <summary>
    /// Where a creature is on its patrol (Creature.Patrol; Managers.Patrols). It is kept while
    /// the creature does something else - a fight, a run home - so it takes the beat up again
    /// at the step it was walking to.
    /// </summary>
    public class ActionPatrol
    {
        /// <summary>The step it is walking to, or standing on.</summary>
        public int Step { get; set; }

        /// <summary>It is on the step: it turns to the step's facing, stands out its pause, and goes on to the next.</summary>
        public bool Arrived { get; set; }

        /// <summary>On the step, it has been turned to the step's facing and its pause is running.</summary>
        public bool Faced { get; set; }

        /// <summary>How much longer it stands as it is: a turn being made, a pause.</summary>
        public long WaitMs { get; set; }

        /// <summary>
        /// It is not on its beat - just made, back from a fight, thrown - and is walking to the
        /// step across the navmesh, as any creature walks; within Patrols.RejoinDistance of the
        /// step it is on the beat again and walks it in straight lines.
        /// </summary>
        public bool Rejoining { get; set; }

        /// <summary>Where the patrol last left it. Anywhere else, something moved it, and it rejoins.</summary>
        public Vector3 LeftAt { get; set; }
    }

    public class ActionWander
    {
        public byte State { get; set; }
        public Vector3 WanderDestination = new Vector3();

        /// <summary>How long it has stood since it last stopped; at BehaviorManager.WanderIntervalMs it strolls again.</summary>
        public long IdleMs { get; set; }

        /// <summary>How long the current stroll has taken; one that runs over BehaviorManager's timeout ends where it is.</summary>
        public long MovingMs { get; set; }

        /// <summary>Running away (Mind Control's Frighten) rather than strolling: the walk is at run speed.</summary>
        public bool Fleeing { get; set; }

        /// <summary>Walking to a corpse its habit sent it to (CreatureHabits): it runs, and does the habit when it gets there.</summary>
        public bool Errand { get; set; }

        /// <summary>Walking in from an arrival point to its pool's ground: at walk speed, however far, with ArrivalTimeoutMs to get there.</summary>
        public bool Arriving { get; set; }

        /// <summary>How long a walk in from an arrival point may take.</summary>
        public long ArrivalTimeoutMs { get; set; }
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
