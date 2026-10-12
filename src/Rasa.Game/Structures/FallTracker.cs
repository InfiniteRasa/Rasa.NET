using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    /// <summary>
    /// A player's current descent, as Managers.FallDamage follows it through their Move packets:
    /// where it started, each Move of it with how fast it was coming down, and the fall within it
    /// that is still under way.
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

        /// <summary>
        /// The descent's Moves, oldest first: the Environment.TickCount64 each was reported at,
        /// where it put the player, and the descent speed over the stretch back to it that is at
        /// least FallDamage.SpeedWindowMs long (-1 before the descent is that long).
        /// </summary>
        public List<(long Tick, Vector3 At, float Speed)> Samples { get; } = new();

        /// <summary>The first Move of the fall under way that came down at FallDamage.FallSpeed, or -1 with none under way.</summary>
        public int FirstFastIndex { get; set; } = -1;

        /// <summary>The last Move of the fall under way that came down at FallDamage.FallSpeed, or -1.</summary>
        public int LastFastIndex { get; set; } = -1;

        /// <summary>Where the last fall of this descent ended: the next one cannot start before it.</summary>
        public int ClosedThroughIndex { get; set; }

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
            FirstFastIndex = -1;
            LastFastIndex = -1;
            ClosedThroughIndex = 0;
        }

        /// <summary>Drops the oldest Moves, keeping the indexes that point into what is left right.</summary>
        public void DropOldest(int count)
        {
            if (count <= 0)
                return;

            Samples.RemoveRange(0, count);

            FirstFastIndex = FirstFastIndex < 0 ? -1 : System.Math.Max(0, FirstFastIndex - count);
            LastFastIndex = LastFastIndex < 0 ? -1 : System.Math.Max(0, LastFastIndex - count);
            ClosedThroughIndex = System.Math.Max(0, ClosedThroughIndex - count);
        }
    }
}
