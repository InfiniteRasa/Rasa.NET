using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Feedback (abilities.feedback), the Engineer's punishment for acting, by its data (action
    /// 298) and the pumps' tooltips. FEEDBACK_EFFECT 383 goes on one enemy for DURATION (20 s);
    /// every time that enemy performs an action of the kind the pump watches, it takes
    /// DAMAGE_AMOUNT (50-60, scaled) of DAMAGE_TYPE (2, incendiary) - and at P3 and up everything
    /// hostile within the radius takes it with it:
    ///
    ///  - P1 Retribution, AFFECT_FRIENDLY: "Activated by: All Healing Actions, Item Use".
    ///  - P2 Counterstrike, AFFECT_HOSTILE: "All Combat Actions".
    ///  - P3 Retaliation, AFFECT_FRIENDLY + RADIUS_AROUND_TARGET 15: healing actions, 15 m.
    ///  - P4 Reprisal, AFFECT_HOSTILE + RADIUS_AROUND_TARGET 15: combat actions, 15 m.
    ///  - P5 Vengeance, both flags + EFFECT_RADIUS 15: "All Actions", 15 m.
    ///
    /// A creature's attack is the hostile action the server has today: BehaviorManager tells
    /// OnCreatureActed every time a creature launches one of its actions. Friendly creature
    /// actions - a creature healing itself or another - have nowhere to come from yet, so the
    /// flag is read and stored and the hook is there for them to call when they do.
    /// </summary>
    public partial class AbilityManager
    {
        private const int FeedbackTypeId = 383;         // FEEDBACK_EFFECT

        private void AttachFeedback(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            var feedback = NewEffect(mapChannel, player, info, FeedbackTypeId, info.Get(AbilityProperty.Duration, 20));

            feedback.IsBuff = false;
            feedback.TickDamageMin = info.Get(AbilityProperty.DamageAmountMin);
            feedback.TickDamageMax = Math.Max(feedback.TickDamageMin, info.Get(AbilityProperty.DamageAmountMax, feedback.TickDamageMin));
            feedback.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Fire);
            feedback.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            feedback.TickRadius = info.Get(AbilityProperty.RadiusAroundTarget, info.Get(AbilityProperty.EffectRadius));
            feedback.ActsOnHostile = info.Get(AbilityProperty.AffectHostile) != 0;
            feedback.ActsOnFriendly = info.Get(AbilityProperty.AffectFriendly) != 0;

            // "Deals %(dmgMin)s-%(dmgMax)s Incendiary damage in a %(aeRadius)s meter radius ..."
            feedback.Tooltip["dmgMin"] = Scale(player.Level, feedback.TickDamageMin, feedback.TickScaleType);
            feedback.Tooltip["dmgMax"] = Scale(player.Level, feedback.TickDamageMax, feedback.TickScaleType);
            feedback.Tooltip["aeRadius"] = (int)feedback.TickRadius;

            GameEffectManager.Instance.Attach(mapChannel, target, feedback);
        }

        /// <summary>
        /// A creature has just performed an action: its Feedback, if the pump watches that kind of
        /// action, burns it - and, at P3 and up, everything hostile within the radius of it.
        /// </summary>
        internal static void OnCreatureActed(MapChannel mapChannel, Creature creature, bool hostile)
        {
            if (creature == null || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            foreach (var feedback in creature.ActiveEffects.Values.Where(e => e.TypeId == FeedbackTypeId && e.TickDamageMax > 0).ToList())
            {
                if (hostile ? !feedback.ActsOnHostile : !feedback.ActsOnFriendly)
                    continue;

                if (feedback.IsExpired || !(feedback.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                var victims = new List<Creature> { creature };

                if (feedback.TickRadius > 0)
                    victims.AddRange(HostilesWithin(mapChannel, player, creature.Position, feedback.TickRadius).Where(c => c != creature));

                var announce = new GameEffectAnnounceDamagePacket(feedback.EffectId);

                foreach (var victim in victims)
                {
                    var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(feedback.SourceLevel, BombRandom.Next(feedback.TickDamageMin, feedback.TickDamageMax + 1), feedback.TickScaleType));

                    announce.Hits.Add(DealDamage(mapChannel, player, victim, rolled, feedback.TickDamageType));
                }

                CellManager.Instance.CellCallMethod(mapChannel, creature, announce);

                if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                    return;
            }
        }
    }
}
