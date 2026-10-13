using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// A creature's RESIST_* and VULNERABLE_* flags (on its class or on itself). A flag names a
    /// damage type and nothing else - no grade, no amount - so each is one fixed value, the same
    /// for every creature and every type; neither the client nor the strategy guide gives it, and
    /// these are ours:
    ///
    /// - Damage: RESIST_ adds <see cref="ResistValue"/> (+50) to the creature's resistance to that
    ///   type, VULNERABLE_ adds <see cref="VulnerableValue"/> (-50). The value joins what the
    ///   effects on it add (Polarity Field, a resist debuff, Called Shot, Rage) in
    ///   GameEffectManager.ApplyResist, which every hit of a type goes through, and the total
    ///   goes through the client's own formula (shared/damageresistance.py, ResistMultiplier):
    ///   +50 takes half, -50 takes half again as much. A creature with both for one type is
    ///   even. A full Polarity Field (-50 at five pumps) brings a resistant creature to 0.
    ///   Types: PHYSICAL, FIRE, ICE, VIRULENT, EMP, LIGHT (Laser), SONIC, ENERGY (Electrical);
    ///   a hit with no type counts as Physical, as for immunity.
    /// - Control: RESIST_KNOCKBACK, _STUN, _SLEEP, _SNARE, _ROOT, _CONFUSION, _FEAR and _BLIND
    ///   are a <see cref="ControlResistPercent"/> (50%) chance to turn the effect away, with
    ///   "Resisted" over the creature - the way a player's armour module's "Resist: Root 9" is a
    ///   9% chance (ItemModuleBonuses.ResistsControl). What an effect does is read as for
    ///   immunity (CreatureImmunities.ControlOf), and a slow riding on damage over time loses the
    ///   slow and keeps its damage. A resisted knockback, stun or root does nothing else, as an
    ///   immune one does.
    /// </summary>
    public static class CreatureResistances
    {
        /// <summary>A RESIST_ flag's resistance. Ours: 50 halves the damage.</summary>
        public const int ResistValue = 50;

        /// <summary>A VULNERABLE_ flag's resistance. Ours: -50 is half again as much.</summary>
        public const int VulnerableValue = -50;

        /// <summary>A control RESIST_ flag's chance, in percent, to turn the effect away. Ours.</summary>
        public const int ControlResistPercent = 50;

        /// <summary>Each type's RESIST_ flag: the eight damage types and the eight kinds of control.</summary>
        public static readonly IReadOnlyDictionary<DamageType, CreatureFlag> ResistFlags = new Dictionary<DamageType, CreatureFlag>
        {
            [DamageType.Physical] = CreatureFlag.ResistPhysical,
            [DamageType.Fire] = CreatureFlag.ResistFire,
            [DamageType.Ice] = CreatureFlag.ResistIce,
            [DamageType.Virulent] = CreatureFlag.ResistVirulent,
            [DamageType.EMP] = CreatureFlag.ResistEmp,
            [DamageType.Laser] = CreatureFlag.ResistLight,
            [DamageType.Sonic] = CreatureFlag.ResistSonic,
            [DamageType.Electrical] = CreatureFlag.ResistEnergy,
            [DamageType.KnockBack] = CreatureFlag.ResistKnockback,
            [DamageType.Stun] = CreatureFlag.ResistStun,
            [DamageType.Sleep] = CreatureFlag.ResistSleep,
            [DamageType.Snare] = CreatureFlag.ResistSnare,
            [DamageType.Root] = CreatureFlag.ResistRoot,
            [DamageType.Confusion] = CreatureFlag.ResistConfusion,
            [DamageType.Fear] = CreatureFlag.ResistFear,
            [DamageType.Blind] = CreatureFlag.ResistBlind
        };

        /// <summary>Each damage type's VULNERABLE_ flag.</summary>
        public static readonly IReadOnlyDictionary<DamageType, CreatureFlag> VulnerableFlags = new Dictionary<DamageType, CreatureFlag>
        {
            [DamageType.Physical] = CreatureFlag.VulnerablePhysical,
            [DamageType.Fire] = CreatureFlag.VulnerableFire,
            [DamageType.Ice] = CreatureFlag.VulnerableIce,
            [DamageType.Virulent] = CreatureFlag.VulnerableVirulent,
            [DamageType.EMP] = CreatureFlag.VulnerableEmp,
            [DamageType.Laser] = CreatureFlag.VulnerableLight,
            [DamageType.Sonic] = CreatureFlag.VulnerableSonic,
            [DamageType.Electrical] = CreatureFlag.VulnerableEnergy
        };

        /// <summary>The kinds of control, whose RESIST_ flags are a chance rather than a resistance.</summary>
        private static readonly HashSet<DamageType> ControlTypes = new HashSet<DamageType>
        {
            DamageType.KnockBack, DamageType.Stun, DamageType.Sleep, DamageType.Snare,
            DamageType.Root, DamageType.Confusion, DamageType.Fear, DamageType.Blind
        };

        /// <summary>The roll for a control resistance: true with the given chance. Replaced by tests.</summary>
        internal static Func<int, bool> Roll { get; set; } = Stuns.Roll;

        /// <summary>
        /// What the actor's flags add to its resistance to a damage type: +50 for its RESIST_, -50
        /// for its VULNERABLE_, both or neither 0. Nothing for a player, or for a kind of control.
        /// </summary>
        public static int ResistOf(Actor actor, DamageType damageType)
        {
            if (!(actor is Creature creature))
                return 0;

            if (damageType == 0)
                damageType = DamageType.Physical;

            if (ControlTypes.Contains(damageType))
                return 0;

            var total = 0;

            if (ResistFlags.TryGetValue(damageType, out var resist) && CreatureManager.HasFlag(creature, resist))
                total += ResistValue;

            if (VulnerableFlags.TryGetValue(damageType, out var vulnerable) && CreatureManager.HasFlag(creature, vulnerable))
                total += VulnerableValue;

            return total;
        }

        /// <summary>
        /// Whether the actor is a creature that resists this effect this time
        /// (GameEffectManager.Attach): a control RESIST_ flag for what the effect does, and the
        /// roll. A slow riding on damage over time that is resisted loses the slow, and is let on.
        /// </summary>
        public static bool ResistsEffect(Actor actor, GameEffect effect)
        {
            if (!(actor is Creature creature))
                return false;

            var control = CreatureImmunities.ControlOf(effect);

            if (control == 0 || !ResistFlags.TryGetValue(control, out var flag) || !CreatureManager.HasFlag(creature, flag))
                return false;

            if (!Roll(ControlResistPercent))
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
