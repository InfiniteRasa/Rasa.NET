using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The two Guardian abilities that answer damage taken, now that a creature's attack says what
    /// type it is (CreatureAttacks).
    ///
    /// Reflection (abilities.reflection): REFLECTION 93 on the performer for DURATION (120 s),
    /// "Damage Reflected: DAMAGE_PERCENT_MAX%" of the types the pump has reached. The pumps add a
    /// type each rather than replacing it - "Damage Type: Laser", then "Laser, Sonic", then
    /// "Laser, Sonic and Physical", then "... Electric", then "... Incendiary" - and each pump's
    /// DAMAGE_TYPE is the one it adds, so the effect carries every type up to the pump used.
    /// Reflected damage is extra: the wearer still takes all of the hit, as with Reflective Armor,
    /// and the creature takes 50 % of it back as damage of the same type. The client's
    /// ReflectionEffect flies it from the wearer to the attacker (AnnounceReflect) and it may
    /// kill. Right-clicking it off is allowed (allowDetach).
    ///
    /// Conversion (abilities.conversion): CONVERSION 199 on the performer for DURATION (30 s),
    /// "Damage Received: +DAMAGE_PERCENT_MAX%" and "Damage Converted to Healing: +HEAL_PERCENT_MAX%"
    /// within "EFFECT_RADIUS m radius". The extra damage is a vulnerability of the same size -
    /// -20 resistance to everything is a fifth as much again, -100 is twice - and every hit that
    /// lands heals the squad around the performer for that share of what it took off them. The
    /// performer is not healed by their own suffering; they are the one paying for it. Healing is
    /// shown by the effect's AnnounceHealing.
    /// </summary>
    public partial class AbilityManager
    {
        private const int ReflectionTypeId = Reflection.ReflectionTypeId;        // REFLECTION
        private const int ConversionTypeId = 199;       // CONVERSION

        /// <summary>The types Reflection has reached, in pump order; each pump adds the next.</summary>
        private static readonly DamageType[] ReflectionPumps =
        {
            DamageType.Laser, DamageType.Sonic, DamageType.Physical, DamageType.Electrical, DamageType.Fire
        };

        /// <summary>Every type Reflection answers at this pump: the pump's own and all before it.</summary>
        public static List<DamageType> ReflectedTypes(int pump, DamageType stated)
        {
            var types = ReflectionPumps.Take(Math.Max(1, Math.Min(ReflectionPumps.Length, pump))).ToList();

            // A pump whose data names a type the table does not reach yet (the sixth, which is
            // incendiary again) still answers it.
            if (stated != 0 && !types.Contains(stated))
                types.Add(stated);

            return types;
        }

        private void AttachReflection(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var reflection = NewEffect(mapChannel, player, info, ReflectionTypeId, info.Get(AbilityProperty.Duration, 120));
            var percent = info.Get(AbilityProperty.DamagePercentMax, info.Get(AbilityProperty.DamagePercentMin, 50));

            reflection.IsBuff = true;
            reflection.AllowDetach = true;
            reflection.ReflectPercent = percent;
            reflection.ReflectTypes.AddRange(ReflectedTypes((int)info.Level, (DamageType)info.Get(AbilityProperty.DamageType)));
            reflection.Tooltip["level"] = (int)info.Level;          // "Reflection Level %(level)s"

            GameEffectManager.Instance.Attach(mapChannel, player, reflection);
        }

        private void AttachConversion(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var conversion = NewEffect(mapChannel, player, info, ConversionTypeId, info.Get(AbilityProperty.Duration, 30));
            var extraDamage = info.Get(AbilityProperty.DamagePercentMax, info.Get(AbilityProperty.DamagePercentMin));

            conversion.IsBuff = true;
            conversion.AllowDetach = true;
            conversion.ResistModifier = -extraDamage;              // a vulnerability of the same size
            conversion.HealPercentOfDamage = info.Get(AbilityProperty.HealPercentMax, info.Get(AbilityProperty.HealPercentMin));
            conversion.HealRadius = info.Get(AbilityProperty.EffectRadius, 3);
            conversion.Tooltip["dmgMod"] = extraDamage;
            conversion.Tooltip["healMod"] = conversion.HealPercentOfDamage;

            GameEffectManager.Instance.Attach(mapChannel, player, conversion);
        }

        /// <summary>
        /// Conversion: damage that landed on the player becomes healing for the squad around them,
        /// the performer apart. Called with what the hit took off armour and health together.
        /// </summary>
        private static void ConvertDamage(MapChannel mapChannel, Manifestation player, int damage)
        {
            if (damage <= 0)
                return;

            foreach (var conversion in player.ActiveEffects.Values.Where(e => e.HealPercentOfDamage > 0).ToList())
            {
                if (conversion.IsExpired)
                    continue;

                var healed = damage * conversion.HealPercentOfDamage / 100;

                if (healed <= 0)
                    continue;

                var announce = new GameEffectAnnounceHealingPacket(conversion.EffectId);

                foreach (var ally in SquadWithin(mapChannel, player, conversion.HealRadius).Where(a => a != player))
                {
                    var applied = ActorManager.Instance.Heal(ally, healed, player.EntityId);

                    if (applied > 0)
                        announce.Heals.Add((ally.EntityId, applied));
                }

                if (announce.Heals.Count > 0)
                    CellManager.Instance.CellCallMethod(mapChannel, player, announce);
            }
        }
    }
}
