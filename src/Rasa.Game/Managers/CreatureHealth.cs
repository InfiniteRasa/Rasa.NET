using System;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Creature health regeneration, out of a fight. Nothing ever gave a creature its health back
    /// but a leash (BehaviorManager.Restore, after a chase of more than MaxChaseDistance): a
    /// creature left at 5% - its attacker died, logged out, ran out of sight or changed map - was
    /// at 5% when the next player found it, however much later, bosses on an 83-minute respawn
    /// included, so a fight was cumulative across deaths and across players (BR-181).
    ///
    /// Now a living creature that is not fighting regains <see cref="RestingRegenPercent"/> of its
    /// maximum health (at least 1) every second, so one left alone is whole again in about ten.
    /// In a fight it regains nothing, as before - its health row is tuned without a rate - so
    /// the rate the clients are told is the resting one or 0, and changes with the fight
    /// (<see cref="SyncRate"/>); the clients predict the bar from the RefreshAmount they were last
    /// given, once a second (Actor.UpdateAttribute sets the period to 1 on every update), so in
    /// between nothing is sent. The effects on the creature change the amount through
    /// GameEffectManager.RegenAmount's RegenPercent and HealthRegenPercent. The dead, a creature
    /// in its Critical Death window, and one that takes no part in fights (an object, a
    /// decoration) do not regenerate.
    ///
    /// RestingRegenPercent is ours: nothing in the client gives creature health a rate.
    /// </summary>
    public static class CreatureHealth
    {
        /// <summary>Percent of its maximum health a creature at rest regains each second. Not in the client.</summary>
        public const int RestingRegenPercent = 10;

        /// <summary>The period the clients assume for every attribute update: once a second.</summary>
        public const int RefreshPeriodSeconds = 1;

        /// <summary>A creature's resting health refresh: RestingRegenPercent of its maximum, at least 1 (0 with no health).</summary>
        public static int BaseRegen(int maxHealth)
        {
            return maxHealth <= 0 ? 0 : Math.Max(1, maxHealth * RestingRegenPercent / 100);
        }

        /// <summary>A creature's health attribute as it is made: full, with its resting rate, so the rate is sent from the start.</summary>
        public static ActorAttributes NewAttribute(int maxHealth)
        {
            return new ActorAttributes(Attributes.Health, maxHealth, maxHealth, maxHealth, BaseRegen(maxHealth), RefreshPeriodSeconds);
        }

        /// <summary>Whether the creature is at rest - a combatant with no fight on - and so regains health.</summary>
        public static bool Rests(Creature creature)
        {
            return creature != null
                && TargetCategories.IsCombatant(creature.TargetCategory)
                && creature.Controller?.CurrentAction != BehaviorManager.BehaviorActionFighting
                && !TurnedAgainstItsSide(creature);
        }

        /// <summary>
        /// Whether a player's Mind Control holds the creature as a weapon against its own side:
        /// Frighten, Confusion or Subversion. Each drops the creature's fight as it lands
        /// (AbilityManager.Confuse), so a frightened creature, out of its fight, regained a tenth
        /// of its health a second and came out of a 15-second Frighten whole. It is still the
        /// caster's enemy's fight; its health comes back when the control ends. Enslavement and
        /// Infectious turn it to the caster's side (TurnCreature), where it rests as theirs.
        /// </summary>
        private static bool TurnedAgainstItsSide(Creature creature)
        {
            foreach (var effect in creature.ActiveEffects.Values)
                if (effect.TypeId == AbilityManager.MindControlTypeId &&
                    effect.MindControlPump >= AbilityManager.MindControlFrighten &&
                    effect.MindControlPump <= AbilityManager.MindControlSubversion)
                    return true;

            return false;
        }

        /// <summary>The rate the creature's health moves at now: the resting rate, or 0 in a fight.</summary>
        public static int RateFor(Creature creature, ActorAttributes health)
        {
            return Rests(creature) ? BaseRegen(health.CurrentMax) : 0;
        }

        /// <summary>
        /// Brings the creature's health rate and period up to date - the resting rate or 0 for a
        /// fight - and tells the clients when either changed. Returns whether it changed.
        /// </summary>
        public static bool SyncRate(MapChannel mapChannel, Creature creature, ActorAttributes health)
        {
            var amount = RateFor(creature, health);

            if (health.RefreshAmount == amount && health.RefreshPeriod == RefreshPeriodSeconds)
                return false;

            health.RefreshAmount = amount;
            health.RefreshPeriod = RefreshPeriodSeconds;

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateHealthPacket(GameEffectManager.WithRegen(creature, health), creature.EntityId));

            return true;
        }

        /// <summary>One second's regeneration: the rate the effects make, up to the maximum.</summary>
        public static void Tick(Creature creature, ActorAttributes health)
        {
            var amount = GameEffectManager.RegenAmount(creature, health);

            if (amount <= 0 || health.Current >= health.CurrentMax)
                return;

            health.Current = Math.Min(health.CurrentMax, health.Current + amount);
        }
    }
}
