using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Whether a hit does nothing at all to its target - "Immune", not resisted. The client shows
    /// it from the hit's rawInfo: wasImmune set makes Actor.AnnounceDamage post
    /// COMBAT_IMMUNE_ANNOUNCED in place of COMBAT_DAMAGE_ANNOUNCED, which floats "Immune" over the
    /// target (UI_FLOAT_IMMUNE, PM_COMBAT_IMMUNE) for the attacker and the target, and puts a
    /// combat log line in the right filter for whoever sees it: PM_COMBAT_IMMUNE_YOUR_HIT to the
    /// attacker, PM_COMBAT_IMMUNE_OTHER_HITS for a squad mate's or a pet's hit,
    /// PM_COMBAT_IMMUNE_HIT_ON_YOU / _NO_SOURCE to the target, PM_COMBAT_IMMUNE_HIT_ON_OTHER for a
    /// squad mate hit. It rides on the hit so it lands with the attack, not before it.
    ///
    /// An actor is immune to a hit when:
    /// - it is a creature a mission scene has made invulnerable (CreatureGameplayRules);
    /// - it is a creature running home after a leash (BehaviorManager.IsReturning);
    /// - it is immune to all damage, or to the hit's type (Actor.ImmuneToAllDamage,
    ///   Actor.DamageImmunities: class data, a script, or .immune);
    /// - an effect on it makes it so (GameEffect.ImmuneToAllDamage, GameEffect.ImmuneDamageTypes).
    ///
    /// Asked by every hit on its way in: weapon fire and creature attacks in MissileManager, and
    /// everything that goes through ActorManager.Damage - abilities, damage over time, bombs,
    /// lightning, traps, reflections. So when weapons, skills and creature attacks deal their own
    /// damage types, a type immunity answers them with nothing more to wire. A hit with no type
    /// (0) counts as Physical, as the client and MissileManager read it.
    /// </summary>
    public static class DamageImmunity
    {
        public static bool IsImmune(Actor target, DamageType damageType)
        {
            if (target == null)
                return false;

            var type = damageType == 0 ? DamageType.Physical : damageType;

            if (target is Creature creature &&
                (Game.Missions.World.CreatureGameplayRules.IsInvulnerable(creature) || BehaviorManager.IsReturning(creature)))
                return true;

            if (target.ImmuneToAllDamage || target.DamageImmunities.Contains(type))
                return true;

            return target.ActiveEffects.Values.Any(effect => effect.ImmuneToAllDamage || effect.ImmuneDamageTypes.Contains(type));
        }
    }
}
