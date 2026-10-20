using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A Warden Bot brought low closes up: nothing hurts it while it is shut.
    ///
    /// The client has the ability and nothing that says what it does or when: WardenBotTurtleAbility
    /// (CR_WARDEN_BOT_TURTLE 445/1, "Warden Bot turtle ability", TARGET_SELF, blockMovement,
    /// stopMovement), a 1.666 s windup (Ability - Creature - Warden Bot - Turtle - Windup, 1289)
    /// and a 5 s recovery (- Resolve, 1288) on the Warden Bot's model, and no ability data. Its
    /// game effect, WARDEN_BOT_TURTLE (308), names a client class
    /// (gameeffects.ai.wardenbotturtleeffect) that is not among the client's scripts we have, so
    /// it is kept on the server (GameEffect.ServerOnly) and the turtle is shown by the action.
    ///
    /// When and what are ours: the first time in a fight that a Warden Bot is below
    /// <see cref="HealthPercent"/> of its health, it winds the turtle up (standing, doing nothing
    /// else, CreatureWindups.After), and for the recovery's 5 s after that it is shut - immune to
    /// all damage (GameEffect.ImmuneToAllDamage, which every hit asks through DamageImmunity:
    /// each one shows "Immune"), not moving and not attacking. Killed or stunned during the windup,
    /// it does not close. It turtles again only in a later fight, once it is out of this one and
    /// back above the line.
    /// </summary>
    public static class WardenBotTurtle
    {
        public const ActionId Action = ActionId.CrWardenBotTurtle;
        public const uint ArgId = 1;

        /// <summary>WARDEN_BOT_TURTLE.</summary>
        public const int TypeId = 308;

        /// <summary>It turtles below this share of its health. Ours.</summary>
        public const int HealthPercent = 50;

        /// <summary>The argument's windup and recovery, when the action data has none.</summary>
        public const int WindupMs = 1666;
        public const int ShutMs = 5000;

        /// <summary>Creature_Prison_Bot_Warden and Creature_Prison_Bot_Warden_Boss.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)6961, (EntityClasses)10315
        };

        private static readonly object Sync = new object();
        private static readonly HashSet<Creature> Turtled = new HashSet<Creature>();

        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);

        /// <summary>Whether it is shut: immune to everything.</summary>
        public static bool IsShut(Creature creature) =>
            creature != null && creature.ActiveEffects.Values.Any(e => e.TypeId == TypeId && !e.IsExpired);

        /// <summary>Whether it has turtled in this fight.</summary>
        public static bool HasTurtled(Creature creature)
        {
            lock (Sync)
                return Turtled.Contains(creature);
        }

        public static bool Below(int current, int max) =>
            max > 0 && current > 0 && (long)current * 100 < (long)max * HealthPercent;

        /// <summary>
        /// Every think (BehaviorManager): one in a fight and below the line that has not turtled in
        /// it yet starts to; one out of its fight and back above the line may turtle again.
        /// </summary>
        public static void Observe(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || !Is(creature) || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying
                || !creature.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            var below = Below(health.Current, health.CurrentMax);

            if (creature.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
            {
                if (!below)
                    lock (Sync)
                        Turtled.Remove(creature);

                return;
            }

            if (!below || HasTurtled(creature) || IsShut(creature) || Stuns.IsStunned(creature)
                || CreatureWindups.IsWindingUp(creature) || CreatureWindups.HasPending(creature))
                return;

            Start(mapChannel, creature);
        }

        /// <summary>The windup now, and the turtle when it is done.</summary>
        public static void Start(MapChannel mapChannel, Creature creature)
        {
            lock (Sync)
                Turtled.Add(creature);

            var windupMs = AbilityManager.Instance != null && AbilityManager.Instance.TryGetLevel(Action, ArgId, out var info) && info.WindupMs > 0
                ? info.WindupMs
                : WindupMs;

            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new PerformWindupPacket(PerformType.ThreeArgs, Action, ArgId, creature.EntityId));

            CreatureWindups.After(mapChannel, creature, windupMs, () => Shut(mapChannel, creature),
                new CreatureAction { ActionId = Action, ActionArgId = ArgId });
        }

        /// <summary>Shut: immune to all damage for the recovery, and standing still for it.</summary>
        public static void Shut(MapChannel mapChannel, Creature creature)
        {
            var shutMs = AbilityManager.Instance != null && AbilityManager.Instance.TryGetLevel(Action, ArgId, out var info) && info.RecoveryMs > 0
                ? info.RecoveryMs
                : ShutMs;
            var until = Environment.TickCount64 + shutMs;

            GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
            {
                TypeId = TypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                AllowDetach = false,
                ServerOnly = true,
                ImmuneToAllDamage = true,
                ExpiresTick = until
            });

            // Shut is the recovery: nothing else until it is over (CreatureWindups.IsWindingUp).
            creature.Controller.WindupUntil = until;

            var recovery = new AbilityRecoveryPacket(Action, ArgId, AbilityRecoveryPacket.HitDataKind.None);
            recovery.Hits.Add(new AbilityHit { EntityId = creature.EntityId });
            CellManager.Instance.CellCallMethod(mapChannel, creature, recovery);
        }

        /// <summary>It has left the world (CellManager.RemoveCreatureFromWorld): nothing kept for it.</summary>
        public static void Forget(Creature creature)
        {
            lock (Sync)
                Turtled.Remove(creature);
        }
    }
}
