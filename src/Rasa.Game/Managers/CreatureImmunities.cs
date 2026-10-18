using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// A creature's IMMUNE_* flags (on its class or on itself): what it takes nothing from.
    /// The flags are the client's damage types, so each names one:
    ///
    /// - Damage: IMMUNE_PHYSICAL, _FIRE, _ICE, _VIRULENT, _EMP, _LIGHT (Laser), _SONIC and
    ///   _ENERGY (Electrical). A hit of that type does nothing and shows "Immune"
    ///   (DamageImmunity.IsImmune, which every hit asks: weapon fire, creature attacks, abilities,
    ///   damage over time, bombs, lightning).
    /// - Control: IMMUNE_KNOCKBACK, _STUN, _SLEEP, _SNARE, _ROOT, _CONFUSION, _FEAR and _BLIND,
    ///   and IMMOBILIZE_IMMUNITY, which is IMMUNE_ROOT under another name.
    ///   An effect that would do that to it is refused as it is attached, with "Immune" over it
    ///   (GameEffectManager.Attach), and does nothing else: a knockback does not throw it, a
    ///   stun neither stops it nor opens its Critical Death window, a root does not stop it. What
    ///   an effect does is read from it (<see cref="ControlOf"/>): a knockback's effect type, a
    ///   stun, a root, a blind, a slow, Mind Control's Frighten (fear) and Confusion or
    ///   Subversion (confusion), or the kind an effect is made with (GameEffect.ControlKind).
    ///   A slow on a damage over time loses the slow and keeps its damage.
    ///   A Vortex pull is a knockback the other way, and an IMMUNE_KNOCKBACK creature is not
    ///   dragged (CrowdControl.Pull). Nothing in the game puts a creature to sleep yet; the flag
    ///   is honoured for whatever does.
    ///
    /// A buff, and the world's own effects (lava), are never refused. Immunity is no number: the
    /// strategy guide's "Immune to Laser damage" is nothing taken, and resistances and
    /// vulnerabilities, which would need one, are not these.
    /// </summary>
    public static class CreatureImmunities
    {
        /// <summary>Each damage type's IMMUNE_ flag.</summary>
        public static readonly IReadOnlyDictionary<DamageType, CreatureFlag> Flags = new Dictionary<DamageType, CreatureFlag>
        {
            [DamageType.Physical] = CreatureFlag.ImmunePhysical,
            [DamageType.Fire] = CreatureFlag.ImmuneFire,
            [DamageType.Ice] = CreatureFlag.ImmuneIce,
            [DamageType.Virulent] = CreatureFlag.ImmuneVirulent,
            [DamageType.EMP] = CreatureFlag.ImmuneEmp,
            [DamageType.Laser] = CreatureFlag.ImmuneLight,
            [DamageType.Sonic] = CreatureFlag.ImmuneSonic,
            [DamageType.Electrical] = CreatureFlag.ImmuneEnergy,
            [DamageType.KnockBack] = CreatureFlag.ImmuneKnockback,
            [DamageType.Stun] = CreatureFlag.ImmuneStun,
            [DamageType.Sleep] = CreatureFlag.ImmuneSleep,
            [DamageType.Snare] = CreatureFlag.ImmuneSnare,
            [DamageType.Root] = CreatureFlag.ImmuneRoot,
            [DamageType.Confusion] = CreatureFlag.ImmuneConfusion,
            [DamageType.Fear] = CreatureFlag.ImmuneFear,
            [DamageType.Blind] = CreatureFlag.ImmuneBlind
        };

        /// <summary>Whether the actor is a creature immune to this type, of damage or of control. A hit with no type (0) is Physical.</summary>
        public static bool IsImmune(Actor actor, DamageType type)
        {
            if (!(actor is Creature creature))
                return false;

            if (type == 0)
                type = DamageType.Physical;

            // IMMOBILIZE_IMMUNITY is IMMUNE_ROOT by another name: nothing holds it where it stands.
            if (type == DamageType.Root && CreatureManager.HasFlag(creature, CreatureFlag.ImmobilizeImmunity))
                return true;

            return Flags.TryGetValue(type, out var flag) && CreatureManager.HasFlag(creature, flag);
        }

        /// <summary>
        /// What an effect would do to hold or hinder whoever it goes on, as the IMMUNE_ flag that
        /// turns it away names it; 0 for a buff, the world's own, or anything that does none of it.
        /// </summary>
        public static DamageType ControlOf(GameEffect effect)
        {
            if (effect == null || effect.IsBuff || effect.Environmental)
                return 0;

            if (effect.TypeId == CrowdControl.KnockbackTypeId || effect.TypeId == CrowdControl.CritSonicTypeId)
                return DamageType.KnockBack;

            if (effect.IsStun)
                return DamageType.Stun;

            if (effect.TypeId == AbilityManager.MindControlTypeId)
                return effect.MindControlPump == AbilityManager.MindControlFrighten ? DamageType.Fear
                    : effect.MindControlPump == AbilityManager.MindControlConfusion || effect.MindControlPump == AbilityManager.MindControlSubversion ? DamageType.Confusion
                    : 0;

            return ItemModuleBonuses.ControlKindOf(effect);
        }

        /// <summary>
        /// Whether the actor is a creature that turns this effect away (GameEffectManager.Attach).
        /// A slow that rides on damage over time (Ruin P5's "Movement: -20%") is not turned away
        /// by IMMUNE_SNARE: the slow is taken off it and the damage goes on, to be answered tick
        /// by tick by the creature's damage immunities.
        /// </summary>
        public static bool RefusesEffect(Actor actor, GameEffect effect)
        {
            var control = ControlOf(effect);

            if (control == 0 || !IsImmune(actor, control))
                return false;

            if (control == DamageType.Snare && effect.TickDamageMax > 0)
            {
                effect.MovementModifierPercent = 0;
                return false;
            }

            return true;
        }
    }
}
