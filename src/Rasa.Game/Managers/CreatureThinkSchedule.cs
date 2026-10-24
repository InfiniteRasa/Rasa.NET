using System;

namespace Rasa.Managers
{
    /// <summary>
    /// When each of a map's creatures thinks, with CreatureAi.StaggeredThink on: the creatures are
    /// dealt into <see cref="Slots"/> groups by entity id, and each group has its own beat of the
    /// think interval, the beats spread evenly over it. A pass of the world loop thinks only the
    /// groups whose beat fell within it, each handed the time since that group last thought.
    ///
    /// Why beats and not a clock per group that starts again at zero on each think, as the map's
    /// one clock did: with those, one long pass - a stall, a garbage collection - brings two
    /// groups due together, both start again from the same moment, and they stay together from
    /// then on; after enough stalls every group thinks on the same pass again. A beat is kept by
    /// the time since the schedule began, so groups are as far apart after a stall as before.
    ///
    /// A group thinks at most once a pass. A pass longer than the interval thinks every group,
    /// once, with all the time since its last think: as the map's one think did for any pass of
    /// 250 ms or more.
    /// </summary>
    public sealed class CreatureThinkSchedule
    {
        private readonly long[] _intoBeat;
        private readonly long[] _sinceThink;
        private readonly long[] _due;

        public CreatureThinkSchedule(int slots, long interval)
        {
            Slots = Math.Max(1, slots);
            Interval = Math.Max(1, interval);
            _intoBeat = new long[Slots];
            _sinceThink = new long[Slots];
            _due = new long[Slots];

            // Group s starts (s + 1/2)/Slots of an interval into its beat: the beats are evenly
            // spaced, and half a spacing off the round multiples that passes of a round length
            // (100 ms) would otherwise land exactly on, where a millisecond either way decides.
            for (var slot = 0; slot < Slots; slot++)
                _intoBeat[slot] = (2 * slot + 1) * Interval / (2 * Slots);
        }

        /// <summary>How many groups the creatures are dealt into.</summary>
        public int Slots { get; }

        /// <summary>How often each group thinks, in milliseconds.</summary>
        public long Interval { get; }

        /// <summary>Whether any group thinks on the pass <see cref="Advance"/> last moved the schedule over.</summary>
        public bool AnyDue { get; private set; }

        /// <summary>How many groups think on that pass.</summary>
        public int DueCount { get; private set; }

        /// <summary>The group a creature is in.</summary>
        public int SlotOf(ulong entityId) => (int)(entityId % (ulong)Slots);

        /// <summary>
        /// Moves the schedule over a pass of <paramref name="delta"/> milliseconds and works out
        /// which groups think in it (<see cref="ElapsedFor"/>).
        /// </summary>
        public void Advance(long delta)
        {
            delta = Math.Max(0, delta);
            AnyDue = false;
            DueCount = 0;

            for (var slot = 0; slot < Slots; slot++)
            {
                _sinceThink[slot] += delta;

                var into = _intoBeat[slot] + delta;

                if (into < Interval)
                {
                    _intoBeat[slot] = into;
                    _due[slot] = 0;
                    continue;
                }

                // A beat fell in this pass (or several, for a pass longer than the interval: one
                // think covers them all). The beat is kept where it was.
                _intoBeat[slot] = into % Interval;
                _due[slot] = Math.Max(1, _sinceThink[slot]);
                _sinceThink[slot] = 0;
                AnyDue = true;
                DueCount++;
            }
        }

        /// <summary>
        /// The time a creature in this group thinks over on this pass: since the group's last
        /// think. 0 when the group does not think on this pass.
        /// </summary>
        public long ElapsedFor(int slot) => _due[slot];

        /// <summary>As <see cref="ElapsedFor(int)"/>, for a creature by its entity id.</summary>
        public long ElapsedFor(ulong entityId) => _due[SlotOf(entityId)];
    }
}
