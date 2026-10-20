using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A creature that turns where it stands plays the turn: the client's locked-turn actions,
    /// actiondata.actionModules 168 and 169 (module lockedturn), each a windup with an animation
    /// family and no recovery animation - 168 "State - Turn Right" (family 650), 169 "State -
    /// Turn Left" (651), 3,000 ms (actionArguments). Before this the creature's facing was only
    /// the new yaw in its movement, and it pivoted with its feet sliding.
    ///
    /// The turn is the windup, sent as the turn starts (PerformWindup), and is ended with the
    /// recovery once the turn has had its time (PerformRecovery): the windup animation is held
    /// until then. LockedTurn.DoAction takes the new yaw, so the recovery carries it.
    ///
    /// Which way is which is the animation family's name. The two action names say the
    /// opposite - TURN_LEFT is the one with "Turn Right" - and nothing else in the client tells
    /// them apart, so if creatures in game turn one way with the other foot first, the two ids
    /// are the ones to swap. A turn of more yaw is to the creature's right: east of a creature
    /// facing south is a turn the other way to west, and the world's x is east and z north.
    ///
    /// Only a turn of more than Patrols.TurnInPlace (a heading change smaller than that is made
    /// on the move), and only for a creature whose skeleton has both families
    /// (<see cref="Skeletons"/>, animationdata.animationFamilyData); one that is winding up,
    /// or has a windup due, is left to it.
    /// </summary>
    public static class CreatureTurns
    {
        /// <summary>TURN_LEFT by name; its windup is "State - Turn Right" (650).</summary>
        public const ActionId TurnRight = (ActionId)168;

        /// <summary>TURN_RIGHT by name; its windup is "State - Turn Left" (651).</summary>
        public const ActionId TurnLeft = (ActionId)169;

        public const uint TurnArg = 1;

        /// <summary>The 94 skeletons a mesh is drawn on that have both turn families.</summary>
        public static readonly IReadOnlySet<int> Skeletons = new HashSet<int>
        {
            1, 21, 23,
            13867, 13868, 13872, 13881, 13884, 13885, 13886, 13887, 13888, 13889, 13890, 13891, 13892,
            13893, 13894, 13895, 13900, 13904, 13905, 13909, 13912, 13913, 13914, 13916, 13918, 13921,
            13922, 13923, 13927, 13928, 13930, 13931, 13937, 13939, 13941, 13943, 13947, 13949, 13951,
            13952, 13956, 13957, 13958, 13960, 13961, 13962, 13963, 13964, 13965, 13967, 13968, 13969,
            13970, 13972, 13974, 13975, 13979, 13980, 13981, 13982, 13984, 13986, 13989, 13991, 13992,
            13993, 13998, 13999, 14000, 14002, 14003, 14004, 14005, 14008, 14014, 14017, 14024, 14025,
            14026, 14027, 14028, 14030, 14031, 14032, 14033, 14034, 14035, 14036, 14037, 14038, 14041
        };

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        private sealed class Turn
        {
            public ActionId Action;
            public float Yaw;
            public long EndsAt;
        }

        private static readonly ConditionalWeakTable<Creature, Turn> Turning = new();

        /// <summary>Whether a creature has the turn animations at all.</summary>
        public static bool CanTurn(Creature creature)
        {
            var body = creature == null ? null : EntityClassManager.Instance.GetClassInfo(creature.EntityClass);

            return body != null && CritDeathAnimations.SkeletonOf(body.MeshId) is { } skeleton && Skeletons.Contains(skeleton);
        }

        /// <summary>Whether the creature is in a turn it was started on and not yet told the end of.</summary>
        public static bool IsTurning(Creature creature) => creature != null && Turning.TryGetValue(creature, out _);

        /// <summary>
        /// A standing creature is turning <paramref name="turn"/> radians (the short way, as
        /// Patrols.Turn gives it) to face <paramref name="yaw"/>, taking <paramref name="turnMs"/>:
        /// its turn plays for everyone in range. Returns whether it did.
        /// </summary>
        public static bool Begin(Creature creature, float turn, float yaw, long turnMs)
        {
            if (creature == null || MathF.Abs(turn) <= Patrols.TurnInPlace || !CanTurn(creature)
                || CreatureWindups.IsWindingUp(creature) || CreatureWindups.HasPending(creature))
                return false;

            var action = turn > 0 ? TurnRight : TurnLeft;

            // A turn begun before the last one was told its end ends that one first.
            if (Turning.TryGetValue(creature, out var previous))
                End(creature, previous);

            Turning.AddOrUpdate(creature, new Turn { Action = action, Yaw = yaw, EndsAt = Now() + Math.Max(1, turnMs) });
            CellManager.Instance.CellCallMethod(creature, new PerformWindupPacket(PerformType.TwoArgs, action, TurnArg));

            return true;
        }

        /// <summary>
        /// From the creature's think: a turn that has had its time is ended. One the creature has
        /// begun something else over - an attack's windup, which the client starts by cancelling
        /// the turn - is only forgotten: its recovery now would cancel that instead.
        /// </summary>
        public static void Finish(Creature creature)
        {
            if (creature == null || !Turning.TryGetValue(creature, out var turn))
                return;

            if (CreatureWindups.IsWindingUp(creature) || CreatureWindups.HasPending(creature))
            {
                Turning.Remove(creature);
                return;
            }

            if (Now() >= turn.EndsAt)
                End(creature, turn);
        }

        private static void End(Creature creature, Turn turn)
        {
            Turning.Remove(creature);
            CellManager.Instance.CellCallMethod(creature, new PerformRecoveryPacket(turn.Action, TurnArg, turn.Yaw));
        }
    }
}
