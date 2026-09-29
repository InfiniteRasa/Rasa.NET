using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Stealing an attribute: what the item modules Health Vamp, Vamp Power, Vamp Chi and Vamp
    /// Armor did on a hit ("Steal Health / Power / Adrenaline / Armor: %(amount)s", module
    /// variants 24-27). The modules are not wired yet; .vamp does it by hand.
    ///
    /// The client shows a steal through a vampiric damage effect on the victim whose source is
    /// the thief (damageeffect.py BaseVampiricDamageEffect): the server attaches it, moves the
    /// attribute, and calls the effect's AnnounceVamp with what the thief gained and what the
    /// victim lost, which floats both numbers and announces both bars. The effect is taken off
    /// again after <see cref="EffectMs"/>; it has no icon and nothing else to show.
    ///
    /// - Health is taken as damage (ActorManager.Damage): armour first, and it can kill, with the
    ///   kill the thief's. The thief heals by all the victim lost, armour included.
    /// - Power, adrenaline (chi) and armour come straight off the victim, down to nothing, and go
    ///   onto the thief up to their maximum.
    /// A victim whose debuffs are blocked (Cure, a creature running home) turns the effect away,
    /// and then nothing is taken.
    /// </summary>
    public static class VampiricDamage
    {
        public const int HealthTypeId = 324;    // VAMPIRIC_HEALTH_DAMAGE
        public const int PowerTypeId = 325;     // VAMPIRIC_POWER_DAMAGE
        public const int ChiTypeId = 326;       // VAMPIRIC_CHI_DAMAGE
        public const int ArmorTypeId = 327;     // VAMPIRIC_ARMOR_DAMAGE

        /// <summary>How long the effect stays on the victim once the steal is announced.</summary>
        public const long EffectMs = 2000;

        /// <summary>The effect type that shows a steal of this attribute; 0 for one that cannot be stolen.</summary>
        public static int TypeIdOf(Attributes attribute) => attribute switch
        {
            Attributes.Health => HealthTypeId,
            Attributes.Power => PowerTypeId,
            Attributes.Chi => ChiTypeId,
            Attributes.Armor => ArmorTypeId,
            _ => 0
        };

        /// <summary>
        /// What a steal of <paramref name="amount"/> moves between two bars: what comes off the
        /// victim's (no more than it has) and what goes onto the thief's (no more than it has room for).
        /// </summary>
        public static (int Taken, int Given) Split(int amount, int victimCurrent, int thiefCurrent, int thiefMax)
        {
            var taken = Math.Clamp(amount, 0, Math.Max(0, victimCurrent));
            var given = Math.Clamp(taken, 0, Math.Max(0, thiefMax - thiefCurrent));

            return (taken, given);
        }

        /// <summary>
        /// The thief takes up to <paramref name="amount"/> of the attribute from the victim.
        /// Returns what the victim lost, 0 when nothing could be taken.
        /// </summary>
        public static int Steal(MapChannel mapChannel, Actor thief, Actor victim, Attributes attribute, int amount)
        {
            var typeId = TypeIdOf(attribute);

            if (mapChannel == null || thief == null || victim == null || thief == victim || typeId == 0 || amount <= 0
                || IsDown(thief) || IsDown(victim)
                || !victim.Attributes.TryGetValue(attribute, out var victimBar)
                || !thief.Attributes.TryGetValue(attribute, out var thiefBar))
                return 0;

            var effect = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = thief.EntityId,
                Source = thief,
                SourceLevel = thief switch { Creature c => (int)c.Level, Manifestation m => m.Level, _ => 1 },
                IsBuff = false,
                AnnounceOnAttach = false,
                AnnounceToNewcomers = false,
                ExpiresTick = Environment.TickCount64 + EffectMs
            };

            GameEffectManager.Instance.Attach(mapChannel, victim, effect);

            // Turned away: immune to debuffs.
            if (!victim.ActiveEffects.ContainsKey(effect.EffectId))
                return 0;

            var announce = new GameEffectAnnounceVampPacket(effect.EffectId);
            int lost;

            if (attribute == Attributes.Health)
            {
                victim.Attributes.TryGetValue(Attributes.Armor, out var victimArmor);
                var armorBefore = victimArmor?.Current ?? 0;
                var healthBefore = victimBar.Current;

                lost = ActorManager.Instance.Damage(mapChannel, victim, amount, thief);

                var armorLost = armorBefore - (victimArmor?.Current ?? 0);
                var healthLost = healthBefore - Math.Max(0, victimBar.Current);

                if (healthLost > 0)
                    announce.TargetData.Add((Attributes.Health, -healthLost));

                if (armorLost > 0)
                    announce.TargetData.Add((Attributes.Armor, -armorLost));

                var healed = ActorManager.Instance.Heal(thief, lost, thief.EntityId);

                if (healed > 0)
                    announce.SourceData.Add((Attributes.Health, healed));
            }
            else
            {
                var (taken, given) = Split(amount, victimBar.Current, thiefBar.Current, thiefBar.CurrentMax);

                victimBar.Current -= taken;
                thiefBar.Current += given;
                lost = taken;

                if (taken > 0)
                {
                    announce.TargetData.Add((attribute, -taken));
                    SendBar(mapChannel, victim, attribute, victimBar);
                }

                if (given > 0)
                {
                    announce.SourceData.Add((attribute, given));
                    SendBar(mapChannel, thief, attribute, thiefBar);
                }
            }

            if (lost > 0)
                CellManager.Instance.CellCallMethod(mapChannel, victim, announce);

            return lost;
        }

        private static bool IsDown(Actor actor) =>
            actor.State == CharacterState.Dead || actor.State == CharacterState.Dying
            || (actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0);

        /// <summary>
        /// The new value to whoever shows it: armour to everyone in range, as damage sends it;
        /// power and adrenaline to the player's own client, the only one that draws them.
        /// </summary>
        private static void SendBar(MapChannel mapChannel, Actor actor, Attributes attribute, ActorAttributes bar)
        {
            var withRegen = GameEffectManager.WithRegen(actor, bar);

            switch (attribute)
            {
                case Attributes.Armor:
                    CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(withRegen, actor is Creature ? actor.EntityId : 0));
                    break;

                case Attributes.Power when actor is Manifestation:
                    GameEffectManager.ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, new UpdatePowerPacket(withRegen, 0));
                    break;

                case Attributes.Chi when actor is Manifestation:
                    GameEffectManager.ClientOf(mapChannel, actor)?.CallMethod(actor.EntityId, new UpdateChiPacket(withRegen, 0));
                    break;
            }
        }
    }
}
