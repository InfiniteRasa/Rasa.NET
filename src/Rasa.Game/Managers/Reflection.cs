using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Damage reflected back at the creature that dealt it: Reflective Armor ("reflects damage
    /// done to the user back to the attacker. User still takes full damage from the attacks.
    /// Reflects all damage types", +3% a pump per piece worn) and the Guardian's Reflection
    /// (DAMAGE_PERCENT_MAX of the types its pumps list). GameEffectManager.ReflectPercentOf adds up
    /// the wearer's effects that answer the hit's type.
    ///
    /// Every direct hit from a living creature is reflected, whichever way it lands: a missile or a
    /// melee swing (MissileManager), a lightning bolt's extra damage and its arcs
    /// (CreatureLightning), a Miasma cloud's pulse (CreatureMiasma) and a bomb whose maker still
    /// stands (CreatureBombs). Not damage over time, not a dead creature's blast - there is nobody
    /// to send it back to - and never a reflection of a reflection.
    ///
    /// The creature takes it through ActorManager.Damage, so a reflection can kill, and the kill
    /// is the wearer's. The clients are told through the reflecting effect's AnnounceReflect
    /// (GameEffectAnnounceReflectPacket), which flies the effect's FX back at the creature and
    /// announces the damage on it when it arrives - hit reaction, damage effect, death. The
    /// Guardian's Reflection is on every client in range, so every one of them is told; Reflective
    /// Armor's is the hidden MEDIUM_ARMOR_SKILL skill passive, on the wearer's client alone.
    /// </summary>
    public static class Reflection
    {
        /// <summary>gameeffectdata REFLECTION: the Guardian's ability, attached for everyone in range.</summary>
        public const int ReflectionTypeId = 93;

        [ThreadStatic]
        private static bool _reflecting;

        /// <summary>
        /// <paramref name="victim"/> has taken <paramref name="amount"/> of
        /// <paramref name="damageType"/> from <paramref name="source"/>: send back what the
        /// victim's effects reflect of it. Returns what the source took.
        /// </summary>
        public static int Reflect(MapChannel mapChannel, Actor victim, Actor source, int amount, DamageType damageType)
        {
            if (_reflecting || mapChannel == null || victim == null || amount <= 0)
                return 0;

            if (!(source is Creature attacker) || attacker == victim || !Alive(attacker))
                return 0;

            // Reflection answers only the types it lists, so the attack's own type decides both
            // how much comes back and which effect is shown as having sent it.
            var percent = GameEffectManager.ReflectPercentOf(victim, damageType);

            if (percent <= 0)
                return 0;

            var reflected = amount * percent / 100;

            if (reflected <= 0)
                return 0;

            // The effect shown sending it back. Reflection before the armour when both answer:
            // every client in range has Reflection, and only the wearer's has the armour's.
            var carrier = victim.ActiveEffects.Values
                .Where(e => e.ReflectPercent > 0 && (e.ReflectTypes.Count == 0 || e.ReflectTypes.Contains(damageType)))
                .OrderByDescending(e => e.TypeId == ReflectionTypeId && !e.IsSkillPassive)
                .FirstOrDefault();

            int taken;
            DamageOutcome outcome;

            _reflecting = true;

            try
            {
                taken = ActorManager.Instance.Damage(mapChannel, attacker, reflected, victim, out outcome, damageType);
            }
            finally
            {
                _reflecting = false;
            }

            if (carrier == null || !(victim is Manifestation player))
                return taken;

            var announce = new GameEffectAnnounceReflectPacket(carrier.EffectId, attacker.EntityId, damageType, outcome.Delivered,
                taken > 0 && attacker.Attributes[Attributes.Health].Current <= 0, absorbed: outcome.Absorbed, wasImmune: outcome.Immune);

            if (carrier.TypeId == ReflectionTypeId && !carrier.IsSkillPassive)
                CellManager.Instance.CellCallMethod(mapChannel, player, announce);
            else
                mapChannel.ClientList.Find(c => c?.Player == player)?.CallMethod(player.EntityId, announce);

            return taken;
        }

        private static bool Alive(Actor actor)
        {
            return actor.State != CharacterState.Dead && actor.State != CharacterState.Dying
                && actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
        }
    }
}
