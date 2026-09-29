using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    /// <summary>
    /// A player's current descent, as Managers.FallDamage follows it through their Move packets:
    /// where it started, the lowest point so far, and the fastest it has gone down.
    /// </summary>
    public class FallTracker
    {
        /// <summary>Going down: every Move since the descent began has been lower than the one before.</summary>
        public bool Descending { get; set; }

        /// <summary>The height the descent started from.</summary>
        public float TopY { get; set; }

        /// <summary>The lowest point so far - where they are, as each Move goes lower.</summary>
        public Vector3 Lowest { get; set; }

        /// <summary>The fastest descent so far, in metres a second, over at least FallDamage.SpeedWindowMs.</summary>
        public float PeakSpeed { get; set; }

        /// <summary>Recent heights with the Environment.TickCount64 each was reported at, oldest first.</summary>
        public List<(long Tick, float Y)> Samples { get; } = new();

        /// <summary>Environment.TickCount64 of the last accepted Move; 0 before the first.</summary>
        public long LastMoveTick { get; set; }

        /// <summary>
        /// Watching the Move flags (.moveflags): the last flags and leading byte shown, or -1
        /// before the first. Off when WatchFlags is false.
        /// </summary>
        public bool WatchFlags { get; set; }
        public int LastFlags { get; set; } = -1;

        public void Reset()
        {
            Descending = false;
            PeakSpeed = 0;
            Samples.Clear();
        }
    }
}
