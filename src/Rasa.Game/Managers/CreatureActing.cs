using System;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// A creature's attack plays on the clients for its windup and its recovery
    /// (actiondata.actionArguments), and the client plays one action of an actor at a time:
    /// a windup or recovery of the same action that arrives while one is still playing is put
    /// back and tried again every 100 ms until it is done (actor.Recv_PerformWindup and
    /// Recv_PerformRecovery, AddMethodCall(..., 100)), and one of another action cuts it short.
    ///
    /// The server let a creature attack again whenever its row's cooldown allowed, and many
    /// rows are shorter than what the client plays: the Wilderness Thrax Grenadier's grenade
    /// (1, 229) came round every 500 ms against a 1,500 ms recovery. Its clients built up a
    /// queue of throws the server had already resolved, and played them out after it died -
    /// a body on the ground still lobbing grenades, for no damage. 55 attack types were faster
    /// than they can be shown.
    ///
    /// So a creature starts no attack until the last one has played: its windup (the row's,
    /// else the argument's, as CreatureWindups has it) and its recovery, the recovery of a
    /// weapon attack drawn out by what the client draws it out by (Called Shot: Arm,
    /// CalledShotArmEffect.GetRecoveryModifier). Its cooldowns run as before; an attack ready
    /// meanwhile waits for it. The time left counts down with the cooldowns, by the think's
    /// delta (BehaviorManager).
    /// </summary>
    public static class CreatureActing
    {
        /// <summary>How long the attack plays on the clients, in ms: 0 for one with no action data.</summary>
        public static int PlayMsOf(Creature creature, CreatureAction action)
        {
            if (action == null || !AbilityManager.Instance.TryGetAction(action.ActionId, action.ActionArgId, out var module, out var info))
                return 0;

            var windup = CreatureWindups.WindupMsOf(action, info);
            var recovery = Math.Max(0, info.RecoveryMs);

            if (!CreatureWindups.IsAbility(module) && creature != null)
                recovery = (int)Math.Round(recovery * GameEffectManager.AttackRateModifierOf(creature));

            return windup + recovery;
        }

        /// <summary>The creature has just started the attack.</summary>
        public static void Began(Creature creature, CreatureAction action)
        {
            if (creature != null)
                creature.ActingMs = PlayMsOf(creature, action);
        }

        /// <summary>Whether the creature's last attack is still playing on the clients.</summary>
        public static bool IsActing(Creature creature) => creature != null && creature.ActingMs > 0;
    }
}
