using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A creature that a shot misses sidesteps it: DODGE (action 184), "VM - Dodge - Left" (arg 1,
    /// animation family 612) or "- Right" (arg 2, family 176), 1,000 ms, 5 s reuse
    /// (actiondata.actionArguments). The Bane soldiers' skeletons have both families, and so do
    /// the human ones (1 and 23); no other creature has them.
    ///
    /// The client's action is a bare BaseActorAction (actions/dodge.py) and nothing on the
    /// client starts it (TargetedAction.DoDodges is empty), so it is the server's to play, as a
    /// PerformRecovery on the creature. The miss goes out as a dodge (misstype 2) with it, and as
    /// a plain miss (1) when the creature does not dodge: one on another skeleton, still within
    /// its reuse, winding up or stunned.
    ///
    /// A recovery cancels whatever the client has the creature doing, so a creature in a windup
    /// is left to it.
    /// </summary>
    public static class CreatureDodges
    {
        public const uint LeftArg = 1;
        public const uint RightArg = 2;
        public const long ReuseMs = 5_000;

        /// <summary>The skeletons with both dodge families (animationdata.animationFamilyData).</summary>
        public static readonly IReadOnlySet<int> Skeletons = new HashSet<int>
        {
            1, 23,
            13867, 13885, 13895, 13904, 13905, 13916, 13918, 13923, 13926, 13928, 13931, 13933,
            13937, 13938, 13939, 13941, 13961, 13962, 13963, 13964, 13965, 13967, 13968, 13969,
            13970, 13974, 13981, 13999, 14003, 14004, 14008, 14025, 14026, 14027, 14028, 14035
        };

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Left or right; a test's to replace.</summary>
        internal static Func<uint> Side { get; set; } = () => Random.Shared.Next(2) == 0 ? LeftArg : RightArg;

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Side = () => Random.Shared.Next(2) == 0 ? LeftArg : RightArg;
        }

        private static readonly ConditionalWeakTable<Creature, StrongBox<long>> LastDodge = new();

        /// <summary>Whether a creature has the dodge animations at all.</summary>
        public static bool CanDodge(Creature creature)
        {
            var body = creature == null ? null : EntityClassManager.Instance.GetClassInfo(creature.EntityClass);

            return body != null && CritDeathAnimations.SkeletonOf(body.MeshId) is { } skeleton && Skeletons.Contains(skeleton);
        }

        /// <summary>
        /// A shot has missed the creature: it dodges, if it can now, and everyone in range sees it.
        /// Returns whether it did.
        /// </summary>
        public static bool TryDodge(MapChannel map, Creature creature)
        {
            if (map == null || creature == null || creature.State is CharacterState.Dead or CharacterState.Dying
                || !CanDodge(creature) || CreatureWindups.IsWindingUp(creature) || CreatureWindups.HasPending(creature)
                || Stuns.IsStunned(creature))
                return false;

            var now = Now();
            var last = LastDodge.GetValue(creature, _ => new StrongBox<long>(long.MinValue));

            lock (last)
            {
                if (last.Value != long.MinValue && now - last.Value < ReuseMs)
                    return false;

                last.Value = now;
            }

            CellManager.Instance.CellCallMethod(map, creature, new PerformRecoveryPacket(PerformType.TwoArgs, ActionId.Dodge, Side()));
            return true;
        }
    }
}
