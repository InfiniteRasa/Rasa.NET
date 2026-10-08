namespace Rasa.Structures
{
    /// <summary>
    /// A player and the lava (Managers.LavaDamage): when they were last found standing in it, and
    /// when it may burn them again. Both are Environment.TickCount64, and 0 before the first time.
    /// </summary>
    public class LavaContact
    {
        /// <summary>The last time they were found in lava.</summary>
        public long LastTouch { get; set; }

        /// <summary>No burn before this: one every LavaDamage.IntervalMs, however often they step in and out.</summary>
        public long NextBurn { get; set; }
    }
}
