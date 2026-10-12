using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Demolitionist's delayed and triggered explosions on an enemy.
    ///
    /// Controlled Fission (abilities.controlledfission): CONTROLLED_FISSION_EFFECT 207 - the
    /// client's ControlledFissionEffect is a BombEffect - on the target. DELAY_TIME_MS (10 s)
    /// later it explodes: every hostile within EFFECT_RADIUS (10 → 20 m) of the target, the target
    /// included, takes DAMAGE_AMOUNT (150 → 450) scaled to the caster's level, with a crit roll
    /// like any ability damage. The blast is CallGameEffectMethod DoExplosion(damageData), which
    /// plays the pump's FX at the target and floats the numbers; the effect then ends. A target
    /// that dies first takes its bomb with it. The data has no damage type: physical.
    ///
    /// Explosive Nanites (abilities.explodingnanites): EXPLODING_NANITES_EFFECT 10000020 on the
    /// target for DURATION (30 s), "Increases damage taken by this target". Each time it takes
    /// damage, from anyone, the nanites explode on it for DAMAGE_AMOUNT (40-50, scaled) of the
    /// pump's DAMAGE_TYPE (sonic, photonic, incendiary, EMP, virulent), no sooner than
    /// USE_DROPOFF (5 s) after the last, USE_COUNT (3) times plus PER_PUMP_MOD (3) for every pump
    /// of the skill owned past the first - the tooltip's "Base Explosions: 3 / +3 explosions per
    /// pump". Shown through the effect's AnnounceDamage. The effect ends with its last charge.
    /// </summary>
    public partial class AbilityManager
    {
        private const int ControlledFissionTypeId = 207;        // CONTROLLED_FISSION_EFFECT
        private const int ExplodingNanitesTypeId = 10000020;    // EXPLODING_NANITES_EFFECT

        /// <summary>skilldata T4_DEMOLITIONIST_EXPLOSIVE_NANITES: pumps owned add charges.</summary>
        private const int ExplosiveNanitesSkillId = 159;

        private static readonly Random BombRandom = new Random();

        /// <summary>Nanite charges for the pump used and the pumps owned: base + per pump past the first.</summary>
        public static int NaniteCharges(int baseCharges, int perPump, int pumpsOwned) => Math.Max(1, baseCharges + perPump * Math.Max(0, pumpsOwned - 1));

        private void AttachControlledFission(MapChannel mapChannel, Manifestation player, Actor target, ActionLevelInfo info)
        {
            var delayMs = Math.Max(500, info.Get(AbilityProperty.DelayTimeMs, 10000));
            var bomb = NewEffect(mapChannel, player, info, ControlledFissionTypeId, null);

            bomb.IsBuff = false;
            bomb.TickDamageMin = info.Get(AbilityProperty.DamageAmountMin);
            bomb.TickDamageMax = Math.Max(bomb.TickDamageMin, info.Get(AbilityProperty.DamageAmountMax, bomb.TickDamageMin));
            bomb.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            bomb.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            bomb.TickRadius = info.Get(AbilityProperty.EffectRadius, 10);

            // One tick, the blast, then gone; the expiry is only a backstop.
            bomb.TickIntervalMs = delayMs;
            bomb.NextTickTick = Environment.TickCount64 + delayMs;
            bomb.ExpiresTick = Environment.TickCount64 + delayMs + 2000;
            bomb.OnTick = Detonate;

            GameEffectManager.Instance.Attach(mapChannel, target, bomb);
        }

        /// <summary>Controlled Fission's blast: every hostile within the radius of the holder, the holder too.</summary>
        private void Detonate(MapChannel mapChannel, Actor holder, GameEffect bomb)
        {
            if (!(bomb.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId || holder == null)
            {
                GameEffectManager.Instance.DettachEffect(mapChannel, holder, bomb);
                return;
            }

            var target = holder;
            var victims = VictimsWithin(mapChannel, player, target.Position, bomb.TickRadius);

            if (!victims.Contains(target) && IsAttackable(player, target))
                victims.Insert(0, target);

            var blast = new GameEffectAnnounceDamagePacket(bomb.EffectId, "DoExplosion");
            var critChance = CriticalHits.AttackerChance(player, false);

            foreach (var victim in victims)
            {
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(bomb.SourceLevel, BombRandom.Next(bomb.TickDamageMin, bomb.TickDamageMax + 1), bomb.TickScaleType));
                var crit = CriticalHits.Resolve(player, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, bomb.TickDamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, player, out var outcome, bomb.TickDamageType);

                var entry = new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = bomb.TickDamageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                };

                blast.Hits.Add(entry);

                // A crit's side effect is announced by the blast (HitEffects).
                if (crit && !outcome.Immune && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    using (HitEffects.On(victim, player, entry.TargetEffectIds))
                        CritEffects.OnCritical(mapChannel, victim, player, bomb.TickDamageType, amount);
            }

            // Sent on the holder, which may just have died in its own blast: the client still has it.
            CellManager.Instance.CellCallMethod(mapChannel, target, blast);

            GameEffectManager.Instance.DettachEffect(mapChannel, target, bomb);
        }

        private void AttachExplosiveNanites(MapChannel mapChannel, Manifestation player, Actor target, ActionLevelInfo info)
        {
            var nanites = NewEffect(mapChannel, player, info, ExplodingNanitesTypeId, info.Get(AbilityProperty.Duration, 30));
            var pumps = Math.Max((int)info.Level, ManifestationManager.SkillPump(player, ExplosiveNanitesSkillId));

            nanites.IsBuff = false;
            nanites.OnDamagedMin = info.Get(AbilityProperty.DamageAmountMin);
            nanites.OnDamagedMax = Math.Max(nanites.OnDamagedMin, info.Get(AbilityProperty.DamageAmountMax, nanites.OnDamagedMin));
            nanites.OnDamagedType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Sonic);
            nanites.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            nanites.OnDamagedCharges = NaniteCharges(info.Get(AbilityProperty.UseCount, 3), info.Get(AbilityProperty.PerPumpMod, 3), pumps);
            nanites.OnDamagedIntervalMs = Math.Max(0, info.Get(AbilityProperty.UseDropoff, 5)) * 1000;

            GameEffectManager.Instance.Attach(mapChannel, target, nanites);
        }

        /// <summary>
        /// A player carrying Explosive Nanites - a creature's (CreatureEffectAttacks), or an enemy
        /// player's (Pvp) - has taken damage: if the nanites are ready they explode on them - the
        /// rolled amount as the creature's row gives it, or scaled to the player who put them on as
        /// on a creature, of the argument's type, through the player's resistances - shown through
        /// the effect's AnnounceDamage as nanites on a creature are. The ready time moves on first,
        /// so the explosion's own damage cannot set off another.
        /// </summary>
        internal static void OnPlayerNanites(MapChannel mapChannel, Manifestation player)
        {
            if (player == null || player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return;

            var now = Environment.TickCount64;

            foreach (var nanites in player.ActiveEffects.Values.Where(e => e.OnDamagedCharges > 0 && e.OnDamagedMax > 0).ToList())
            {
                if (now < nanites.OnDamagedReadyAt || nanites.IsExpired)
                    continue;

                if (!(nanites.Source is Actor thrax) || thrax.MapContextId != mapChannel.MapInfo.MapContextId || thrax == player)
                    continue;

                nanites.OnDamagedReadyAt = now + nanites.OnDamagedIntervalMs;
                nanites.OnDamagedCharges--;

                int rolled;

                lock (BombRandom)
                    rolled = BombRandom.Next(nanites.OnDamagedMin, nanites.OnDamagedMax + 1);

                if (thrax is Manifestation bomber)
                    rolled = GameEffectManager.ApplyDamageDealt(bomber, Scale(nanites.SourceLevel, rolled, nanites.TickScaleType));

                var amount = GameEffectManager.ApplyResist(player, rolled, out var resisted, nanites.OnDamagedType);
                var taken = ActorManager.Instance.Damage(mapChannel, player, amount, thrax, out var outcome, nanites.OnDamagedType);

                var announce = new GameEffectAnnounceDamagePacket(nanites.EffectId);

                announce.Hits.Add(new TickEntry
                {
                    EntityId = player.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = nanites.OnDamagedType,
                    DeathBlow = false
                });

                CellManager.Instance.CellCallMethod(mapChannel, player, announce);

                if (nanites.OnDamagedCharges <= 0 && player.ActiveEffects.ContainsKey(nanites.EffectId))
                    GameEffectManager.Instance.DettachEffect(mapChannel, player, nanites);
            }
        }

        /// <summary>
        /// Called when a creature has taken damage and is still standing: its Explosive Nanites, if
        /// any are ready, explode on it. The ready time is moved on before the explosion is dealt,
        /// so the explosion's own damage cannot set off another.
        /// </summary>
        internal static void OnCreatureDamaged(MapChannel mapChannel, Creature creature)
        {
            if (creature == null || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            var now = Environment.TickCount64;

            // An enemy's Hack lets go of a machine that is hurt.
            ReleaseOnDamage(mapChannel, creature);

            // An armed Called Shot goes off first: taken off, then run, so the wound it opens
            // cannot set it off a second time.
            foreach (var armed in creature.ActiveEffects.Values.Where(e => e.OnDamaged != null).ToList())
            {
                var land = armed.OnDamaged;

                armed.OnDamaged = null;
                GameEffectManager.Instance.DettachEffect(mapChannel, creature, armed);
                land(mapChannel, creature, armed);
            }

            foreach (var nanites in creature.ActiveEffects.Values.Where(e => e.OnDamagedCharges > 0 && e.OnDamagedMax > 0).ToList())
            {
                if (now < nanites.OnDamagedReadyAt || nanites.IsExpired)
                    continue;

                if (!(nanites.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                nanites.OnDamagedReadyAt = now + nanites.OnDamagedIntervalMs;
                nanites.OnDamagedCharges--;

                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(nanites.SourceLevel, BombRandom.Next(nanites.OnDamagedMin, nanites.OnDamagedMax + 1), nanites.TickScaleType));
                var amount = GameEffectManager.ApplyResist(creature, rolled, out var resisted, nanites.OnDamagedType);
                var taken = ActorManager.Instance.Damage(mapChannel, creature, amount, player, out var outcome, nanites.OnDamagedType);

                var announce = new GameEffectAnnounceDamagePacket(nanites.EffectId);

                announce.Hits.Add(new TickEntry
                {
                    EntityId = creature.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = nanites.OnDamagedType,
                    DeathBlow = taken > 0 && creature.Attributes[Attributes.Health].Current <= 0
                });

                CellManager.Instance.CellCallMethod(mapChannel, creature, announce);

                if (nanites.OnDamagedCharges <= 0 && creature.ActiveEffects.ContainsKey(nanites.EffectId))
                    GameEffectManager.Instance.DettachEffect(mapChannel, creature, nanites);

                if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                    return;
            }
        }
    }
}
