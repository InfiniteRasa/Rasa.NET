using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The abilities that are a timed effect rather than an instant: what each one's
    /// action_property row means, turned into a GameEffect for GameEffectManager to run.
    ///
    /// The exchange with the client is the one in the class remarks, with one more rule: an
    /// ability's effects are attached quietly (AnnounceOnAttach false) and the PerformRecovery
    /// that follows names the entities hit, and the client announces the effects on those - it
    /// plays the attach FX and posts the icon when the action resolves, not a packet earlier.
    /// TargetedAction.OnServerResolution does that for the class's targetGameEffect on every hit
    /// and its sourceGameEffect on the performer; ReconstructionAction.DoAbility does it from
    /// (entityId, effectTypeId) pairs in hitdata. An ability whose client class names no effect
    /// (Resistance, Sacrifice) gets an announced attach instead.
    ///
    /// Every tooltip key an effect's format string uses is set, whether or not the server applies
    /// that part yet, because the client's % formatting throws on a missing key.
    /// </summary>
    public partial class AbilityManager
    {
        // gameeffectdata ids, by the constants the client classes use.
        private const int RageTypeId = 235;                         // RAGE, on the squad
        private const int RageSourceTypeId = 236;                   // RAGESOURCE, on the performer
        private const int ResistanceTypeId = 10000085;              // RESISTANCE
        private const int SacrificeTypeId = 192;                    // SACRIFICE
        private const int DecayTypeId = 82;                         // DECAY (Ruin)
        private const int ScourgeTypeId = 256;                      // SCOURGE_EFFECT
        private const int ReconstructionHelpTypeId = 180;           // RECONSTRUCTION_HELP_EFFECT, a HealOverTime
        private const int ReconstructionHarmTypeId = 10000063;      // RECONSTRUCTION_HARM_EFFECT, a DamageOverTime
        private const int ReconstructionHelpPoolTypeId = 10000064;  // RECONSTRUCTION_HELP_POOL_EFFECT
        private const int ReconstructionHarmPoolTypeId = 10000065;  // RECONSTRUCTION_HARM_POOL_EFFECT
        private const int RegenerationWaveTypeId = 10000019;        // REGENERATIONWAVE
        private const int BaseWaveTypeId = 206;                     // BASEWAVEEFFECT
        private const int MedpackTypeId = 280;                      // CONSUMABLE_MED_PACK

        /// <summary>
        /// How long the effect that carries an instant Reconstruction's numbers stays on. The
        /// client shows a heal or a damage-over-time's damage through the effect's tick, so an
        /// instant heal still wants the effect there for the tick to land on - briefly.
        /// </summary>
        private const int InstantMarkerSeconds = 2;

        private void ResolveTimedEffect(MapChannel mapChannel, Client client, Manifestation player, ActionInfo actionInfo, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            switch (actionInfo.Module)
            {
                case "abilities.medpack":
                    var medpack = NewEffect(mapChannel, player, info, MedpackTypeId, info.Get(AbilityProperty.Duration, 5));
                    medpack.TickHealMin = info.Get(AbilityProperty.HealAmountMin);
                    medpack.TickHealMax = Math.Max(medpack.TickHealMin,
                        info.Get(AbilityProperty.HealAmountMax, medpack.TickHealMin));
                    medpack.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000;
                    medpack.NextTickTick = Environment.TickCount64;
                    medpack.TickScaleType = info.Get(AbilityProperty.ConsumableScaleType);
                    medpack.AllowDetach = true;
                    medpack.Tooltip["healMin"] = Scale(player.Level, medpack.TickHealMin, medpack.TickScaleType);
                    medpack.Tooltip["healMax"] = Scale(player.Level, medpack.TickHealMax, medpack.TickScaleType);
                    medpack.Tooltip["interval"] = medpack.TickIntervalMs / 1000;
                    GameEffectManager.Instance.Attach(mapChannel, player, medpack);
                    Hit(recovery, player);
                    break;

                case "abilities.rage":
                    AttachRage(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.resistance":
                    AttachResistance(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.sacrifice":
                    AttachSacrifice(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.decay":
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    // Died or despawned during the windup: performed, paid for, hit nothing - as
                    // direct damage does.
                    if (target != null && IsHostile(player, target))
                    {
                        AttachRuin(mapChannel, player, target, info);
                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

                case "abilities.scourge":
                    AttachScourge(mapChannel, player, info);
                    ManifestationManager.Instance.EnterCombat(client);
                    Hit(recovery, player);
                    break;

                case "abilities.reconstruction":
                    recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.EffectAttach);
                    Reconstruct(mapChannel, client, player, info, recovery);
                    break;

                case "abilities.regenerationwave":
                    foreach (var member in SquadWithin(mapChannel, player, info.Get(AbilityProperty.RadiusAroundSource, 25)))
                    {
                        var wave = NewEffect(mapChannel, player, info, RegenerationWaveTypeId, info.Get(AbilityProperty.Duration, 120));
                        wave.RegenPercent = info.Get(AbilityProperty.AttributePercent, 400);
                        wave.AllowDetach = true;
                        GameEffectManager.Instance.Attach(mapChannel, member, wave);
                        Hit(recovery, member);
                    }

                    break;

                case "abilities.basewave":
                    foreach (var member in SquadWithin(mapChannel, player, info.Get(AbilityProperty.RadiusAroundSource, 25)))
                    {
                        var wave = NewEffect(mapChannel, player, info, BaseWaveTypeId, info.Get(AbilityProperty.Duration, 120));
                        wave.ResistModifier = info.Get(AbilityProperty.ResistModifier, 25);
                        wave.ArmorRegenPercent = info.Get(AbilityProperty.EffectArmorRegenModifier, 500);
                        wave.AllowDetach = true;
                        GameEffectManager.Instance.Attach(mapChannel, member, wave);
                        Hit(recovery, member);
                    }

                    break;
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        /// <summary>
        /// An effect of the ability's: the performer is its source, it is at the ability's level,
        /// it runs for the given seconds (null: until detached), and the ability's recovery will
        /// announce it.
        /// </summary>
        private static GameEffect NewEffect(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, int typeId, int? durationSeconds)
        {
            return new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = info.Level,
                ActionId = info.ActionId,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                ExpiresTick = durationSeconds.HasValue ? Environment.TickCount64 + durationSeconds.Value * 1000L : long.MaxValue,
                AnnounceOnAttach = false
            };
        }

        private static void Hit(AbilityRecoveryPacket recovery, Actor actor, int effectTypeId = 0)
        {
            recovery.Hits.Add(new AbilityHit { EntityId = actor.EntityId, EffectTypeId = effectTypeId });
        }

        /// <summary>
        /// Rage: DAMAGE_PERCENT more damage from everything the performer does for DURATION,
        /// RESIST_MODIFIER at the pumps that have it, and at the pumps with a
        /// RADIUS_AROUND_SOURCE the same for the squad within it - as an aura, re-read every
        /// INTERVAL seconds, so a squad mate who walks in gets it and one who walks out loses
        /// it. The performer carries RAGESOURCE and the squad RAGE; the client's
        /// RageSourceEffect.OnTick announces the squad's copies from the ids in the tick.
        /// Costs power and adrenaline both, and the toggle to turn it off early is the client's
        /// RequestDetachGameEffect.
        /// </summary>
        private static void AttachRage(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, RageSourceTypeId, info.Get(AbilityProperty.Duration, 20));

            effect.DamageDealtPercent = info.Get(AbilityProperty.DamagePercentMin);
            effect.ResistModifier = info.Get(AbilityProperty.ResistModifier);
            effect.AllowDetach = true;
            effect.Tooltip["dmgMod"] = effect.DamageDealtPercent;
            effect.Tooltip["resistMod"] = effect.ResistModifier;

            if (info.Has(AbilityProperty.RadiusAroundSource))
            {
                effect.AuraRadius = info.Get(AbilityProperty.RadiusAroundSource);
                effect.AuraChildTypeId = RageTypeId;
                effect.AuraTickAnnounces = true;
                effect.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000;
                effect.NextTickTick = Environment.TickCount64;    // the squad gets it on the next pass, not in five seconds
            }

            GameEffectManager.Instance.Attach(mapChannel, player, effect);
        }

        /// <summary>
        /// Resistance: RESIST_MODIFIER to everything for DURATION, on the performer and, as an
        /// aura every INTERVAL seconds, on the squad within RADIUS_AROUND_SOURCE. The client's
        /// ResistanceAbility names no effect to announce, so the attach announces itself.
        /// </summary>
        private static void AttachResistance(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, ResistanceTypeId, info.Get(AbilityProperty.Duration, 45));

            effect.ResistModifier = info.Get(AbilityProperty.ResistModifier, 10);
            effect.AllowDetach = true;
            effect.AnnounceOnAttach = true;
            effect.Tooltip["resistMod"] = effect.ResistModifier;
            effect.AuraRadius = info.Get(AbilityProperty.RadiusAroundSource, 20);
            effect.AuraChildTypeId = ResistanceTypeId;
            effect.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000;
            effect.NextTickTick = Environment.TickCount64;

            GameEffectManager.Instance.Attach(mapChannel, player, effect);
        }

        /// <summary>
        /// Sacrifice: OFFENSIVE_DAMAGE_MODIFIER to the damage the performer deals and
        /// RESIST_MODIFIER to what they take, one bought with the other - the odd pumps give up
        /// damage for resistance, the even pumps the reverse - until turned off. No duration in
        /// the data, so none here. THREAT_MODIFIER_PERCENT is shown but not applied: creatures
        /// have no threat table yet. The client's SacrificeAbility is a toggle that names no
        /// effect, so the attach announces itself and a second press is what turns it off.
        /// </summary>
        private static void AttachSacrifice(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, SacrificeTypeId, null);

            effect.DamageDealtPercent = info.Get(AbilityProperty.OffensiveDamageModifier);
            effect.ResistModifier = info.Get(AbilityProperty.ResistModifier);
            effect.AllowDetach = true;
            effect.AnnounceOnAttach = true;
            effect.Tooltip["dmgMod"] = effect.DamageDealtPercent;
            effect.Tooltip["resistMod"] = effect.ResistModifier;
            effect.Tooltip["threatMod"] = info.Get(AbilityProperty.ThreatModifierPercent);

            GameEffectManager.Instance.Attach(mapChannel, player, effect);
        }

        /// <summary>
        /// Ruin: DAMAGE_AMOUNT_MIN..MAX of DAMAGE_TYPE to one enemy every INTERVAL seconds for
        /// DURATION, scaled to the performer's level like any ability damage. The first tick is
        /// an interval in, as the tooltip's "every N seconds" reads. Pump 5's
        /// EFFECT_MOVEMENT_MODIFIER (a slow) is not applied: creatures do not move by
        /// MovementSpeed yet.
        /// </summary>
        private static void AttachRuin(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            var interval = Math.Max(1, info.Get(AbilityProperty.Interval, 1));
            var effect = NewEffect(mapChannel, player, info, DecayTypeId, info.Get(AbilityProperty.Duration, 10));

            effect.IsBuff = false;
            effect.TickDamageMin = info.Get(AbilityProperty.DamageAmountMin);
            effect.TickDamageMax = Math.Max(effect.TickDamageMin, info.Get(AbilityProperty.DamageAmountMax, effect.TickDamageMin));
            effect.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Virulent);
            effect.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            effect.TickIntervalMs = interval * 1000;
            effect.NextTickTick = Environment.TickCount64 + effect.TickIntervalMs;
            effect.Tooltip["dmgMin"] = Scale(player.Level, effect.TickDamageMin, effect.TickScaleType);
            effect.Tooltip["dmgMax"] = Scale(player.Level, effect.TickDamageMax, effect.TickScaleType);
            effect.Tooltip["interval"] = interval;

            GameEffectManager.Instance.Attach(mapChannel, target, effect);
        }

        /// <summary>
        /// Scourge: every second for DURATION, DAMAGE_AMOUNT_MIN..MAX to every enemy within
        /// EFFECT_RADIUS of the performer. The data gives no interval; a second is the reading
        /// that makes pump 1 (23-30 a tick, 15 ticks) worth a Shrapnel or two, which is what a
        /// level-15 ability costing 30 power should be. The client's ScourgeEffect ticks on the
        /// damage it announces, so the tick and the numbers arrive together.
        /// </summary>
        private static void AttachScourge(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, ScourgeTypeId, info.Get(AbilityProperty.Duration, 15));

            effect.TickRadius = info.Get(AbilityProperty.EffectRadius, 6);
            effect.TickDamageMin = info.Get(AbilityProperty.DamageAmountMin);
            effect.TickDamageMax = Math.Max(effect.TickDamageMin, info.Get(AbilityProperty.DamageAmountMax, effect.TickDamageMin));
            effect.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            effect.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            effect.TickIntervalMs = 1000;
            effect.NextTickTick = Environment.TickCount64 + 1000;

            GameEffectManager.Instance.Attach(mapChannel, player, effect);
        }

        /// <summary>
        /// Reconstruction: the squad within RADIUS_AROUND_SOURCE is helped and the enemies within
        /// it harmed, in the shape the pump's properties describe:
        ///
        ///  - an INTERVAL (pumps 2 and 4): a heal over time for the squad (HEAL_AMOUNT per tick,
        ///    ADRENALINE_INCREASE adrenaline at pump 4) and a damage over time for the enemies;
        ///  - an EFFECT_MODIFIER and no amounts (pump 5): that percent on the squad's maximum
        ///    health and off the enemies', for DURATION;
        ///  - otherwise (pumps 1 and 3): HEAL_AMOUNT to the squad and DAMAGE_AMOUNT to the enemies
        ///    at once. Pump 3's ATTRIBUTE_MAX_CHANGE (Spirit up for the squad, down for the
        ///    enemies) is not applied; its tooltip says 0.
        ///
        /// Each recipient gets the pump's effect - HELP or HARM, or the POOL pair - and the
        /// recovery's hitdata names it, so ReconstructionAction.DoAbility announces the right
        /// one on each. The instant numbers ride on the effect's tick, which the client's
        /// HealOverTime and DamageOverTime float; the effect itself stays a moment for that.
        /// </summary>
        private void Reconstruct(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, AbilityRecoveryPacket recovery)
        {
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 15);
            var allies = SquadWithin(mapChannel, player, radius);
            var enemies = HostilesWithin(mapChannel, player, player.Position, radius);
            var interval = info.Get(AbilityProperty.Interval);
            var scaleType = info.Get(AbilityProperty.DamageScaleType);
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Virulent);
            var healMin = info.Get(AbilityProperty.HealAmountMin);
            var healMax = Math.Max(healMin, info.Get(AbilityProperty.HealAmountMax, healMin));
            var damageMin = info.Get(AbilityProperty.DamageAmountMin);
            var damageMax = Math.Max(damageMin, info.Get(AbilityProperty.DamageAmountMax, damageMin));
            var adrenaline = info.Get(AbilityProperty.AdrenalineIncreaseMin);

            if (enemies.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);

            // Pump 5: the health pools.
            if (info.Has(AbilityProperty.EffectModifier) && healMax == 0 && damageMax == 0)
            {
                var percent = info.Get(AbilityProperty.EffectModifier, 30);
                var duration = info.Get(AbilityProperty.Duration, 60);

                foreach (var ally in allies)
                {
                    var pool = NewEffect(mapChannel, player, info, ReconstructionHelpPoolTypeId, duration);
                    pool.MaxHealthPercent = percent;
                    pool.AllowDetach = true;
                    pool.Tooltip["healthPoolMod"] = percent;
                    GameEffectManager.Instance.Attach(mapChannel, ally, pool);
                    Hit(recovery, ally, ReconstructionHelpPoolTypeId);
                }

                foreach (var enemy in enemies)
                {
                    var pool = NewEffect(mapChannel, player, info, ReconstructionHarmPoolTypeId, duration);
                    pool.IsBuff = false;
                    pool.MaxHealthPercent = -percent;
                    pool.Tooltip["healthPoolMod"] = percent;
                    GameEffectManager.Instance.Attach(mapChannel, enemy, pool);
                    Hit(recovery, enemy, ReconstructionHarmPoolTypeId);
                }

                return;
            }

            var instant = interval <= 0;
            var effectSeconds = instant ? InstantMarkerSeconds : info.Get(AbilityProperty.Duration, 15);
            var tickMs = instant ? 0 : Math.Max(1, interval) * 1000;

            foreach (var ally in allies)
            {
                var help = NewEffect(mapChannel, player, info, ReconstructionHelpTypeId, effectSeconds);
                help.AllowDetach = true;
                help.TickScaleType = scaleType;
                help.Tooltip["spiritBuff"] = 0;
                help.Tooltip["healMin"] = Scale(player.Level, healMin, scaleType);
                help.Tooltip["healMax"] = Scale(player.Level, healMax, scaleType);
                help.Tooltip["adrenalineMin"] = adrenaline;
                help.Tooltip["adrenalineMax"] = adrenaline;
                help.Tooltip["interval"] = instant ? effectSeconds : interval;

                if (!instant)
                {
                    help.TickHealMin = healMin;
                    help.TickHealMax = healMax;
                    help.TickAdrenaline = adrenaline;
                    help.TickIntervalMs = tickMs;
                    help.NextTickTick = Environment.TickCount64 + tickMs;
                }

                GameEffectManager.Instance.Attach(mapChannel, ally, help);
                Hit(recovery, ally, ReconstructionHelpTypeId);

                if (instant && healMax > 0)
                {
                    var healed = ActorManager.Instance.Heal(ally, Scale(player.Level, _random.Next(healMin, healMax + 1), scaleType), player.EntityId);
                    var tick = new GameEffectTickPacket(help.EffectId, GameEffectTickPacket.TickKind.Heal);

                    if (healed > 0)
                        tick.Entries.Add(new TickEntry { EntityId = ally.EntityId, Amount = healed });

                    CellManager.Instance.CellCallMethod(mapChannel, ally, tick);
                }
            }

            foreach (var enemy in enemies)
            {
                var harm = NewEffect(mapChannel, player, info, ReconstructionHarmTypeId, effectSeconds);
                harm.IsBuff = false;
                harm.TickDamageType = damageType;
                harm.TickScaleType = scaleType;
                harm.Tooltip["spiritMod"] = 0;
                harm.Tooltip["dmgMin"] = Scale(player.Level, damageMin, scaleType);
                harm.Tooltip["dmgMax"] = Scale(player.Level, damageMax, scaleType);
                harm.Tooltip["adrenalineMin"] = 0;
                harm.Tooltip["adrenalineMax"] = 0;
                harm.Tooltip["interval"] = instant ? effectSeconds : interval;

                if (!instant)
                {
                    harm.TickDamageMin = damageMin;
                    harm.TickDamageMax = damageMax;
                    harm.TickIntervalMs = tickMs;
                    harm.NextTickTick = Environment.TickCount64 + tickMs;
                }

                GameEffectManager.Instance.Attach(mapChannel, enemy, harm);
                Hit(recovery, enemy, ReconstructionHarmTypeId);

                if (instant && damageMax > 0)
                {
                    var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, _random.Next(damageMin, damageMax + 1), scaleType));
                    var amount = GameEffectManager.ApplyResist(enemy, rolled, out var resisted);
                    var taken = ActorManager.Instance.Damage(mapChannel, enemy, amount, player);
                    var tick = new GameEffectTickPacket(harm.EffectId, GameEffectTickPacket.TickKind.Damage);

                    tick.Entries.Add(new TickEntry
                    {
                        EntityId = enemy.EntityId,
                        Amount = amount,
                        Resisted = resisted,
                        DamageType = damageType,
                        DeathBlow = taken > 0 && enemy.Attributes[Attributes.Health].Current <= 0
                    });

                    CellManager.Instance.CellCallMethod(mapChannel, enemy, tick);
                }
            }
        }
    }
}
