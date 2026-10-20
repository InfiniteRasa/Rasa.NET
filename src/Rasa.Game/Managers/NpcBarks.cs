using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Barks: a line an NPC says now and then, in a bubble over it, voiced where the client has a
    /// voice for it. The client says one when told to: Creature.Recv_Bark(barkID)
    /// (client/augmentations/creature.py) looks the bark up in generated/client/bark for an audio
    /// id and a duration, plays the audio on the NPC if it has one and shows the line
    /// (barklanguage) for the duration. Nothing in the client starts one, and which NPC barked
    /// which was the 2009 server's.
    ///
    /// Which NPC has which barks is data: npc_bark gives a creature row its barks
    /// (<see cref="Load"/>). An NPC with none never barks.
    ///
    /// When is ours. An NPC with barks barks at a time drawn when it is first seen and again
    /// each time it has barked: within <see cref="HalfMs"/> (10 minutes) on half the draws,
    /// uniformly, and between that and <see cref="WithinMs"/> (30 minutes) on the other half,
    /// uniformly. So it has barked within 10 minutes half the time and always within 30. One
    /// of its barks is chosen each time, all alike. The bark goes to everyone around it
    /// (CellCallMethod); with nobody there it is said to nobody, and the next is drawn all the
    /// same.
    /// </summary>
    public static class NpcBarks
    {
        /// <summary>Half the barks come within this of the last (or of the NPC being first seen): 10 minutes.</summary>
        public const int HalfMs = 10 * 60 * 1000;

        /// <summary>Every bark comes within this: 30 minutes.</summary>
        public const int WithinMs = 30 * 60 * 1000;

        /// <summary>A roll under its argument, from 0. Tests set it.</summary>
        internal static Func<int, int> Roll = max => Random.Shared.Next(max);

        /// <summary>The clock barks are timed by, in milliseconds. Tests set it.</summary>
        internal static Func<long> Now = () => Environment.TickCount64;

        private static Dictionary<uint, uint[]> _byCreature = new Dictionary<uint, uint[]>();

        /// <summary>Takes the barks (npc_bark). Returns how many were taken.</summary>
        public static int Load(IEnumerable<NpcBarkEntry> rows)
        {
            var byCreature = (rows ?? Enumerable.Empty<NpcBarkEntry>())
                .GroupBy(row => row.CreatureId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.BarkId).Distinct().OrderBy(id => id).ToArray());

            _byCreature = byCreature;

            return byCreature.Values.Sum(barks => barks.Length);
        }

        /// <summary>The barks a creature has, by its creature row; none for one that has none.</summary>
        public static IReadOnlyList<uint> BarksOf(Creature creature) =>
            creature != null && _byCreature.TryGetValue(creature.DbId, out var barks) ? barks : Array.Empty<uint>();

        /// <summary>How long until the next bark: under HalfMs on half the draws, from HalfMs to under WithinMs on the other.</summary>
        internal static long NextDelay() =>
            Roll(2) == 0
                ? Roll(HalfMs)
                : HalfMs + Roll(WithinMs - HalfMs);

        /// <summary>
        /// Called for a living NPC on every think (BehaviorManager.CreatureThink): its first bark
        /// drawn when it is first seen, and a bark said when one is due and the next drawn.
        /// </summary>
        public static void Observe(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null)
                return;

            var barks = BarksOf(creature);

            if (barks.Count == 0)
                return;

            var now = Now();

            if (creature.NextBarkAt == null)
            {
                creature.NextBarkAt = now + NextDelay();
                return;
            }

            if (now < creature.NextBarkAt)
                return;

            creature.NextBarkAt = now + NextDelay();
            Bark(mapChannel, creature, barks[barks.Count == 1 ? 0 : Roll(barks.Count)]);
        }

        /// <summary>The bark said by the creature to everyone around it.</summary>
        public static void Bark(MapChannel mapChannel, Creature creature, uint barkId) =>
            CellManager.Instance.CellCallMethod(mapChannel, creature, new BarkPackage(barkId));
    }
}
