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
        private const int CritWaveTypeId = 202;                     // CRITWAVEEFFECT
        private const int ShieldExtenderSourceTypeId = 10000056;    // SHIELD_EXTENDER_SOURCE, on the target
        private const int ShieldExtenderShieldedTypeId = 10000055;  // SHIELD_EXTENDER_SHIELDED, on the squad near it
        private const int ShieldWaveTypeId = 201;                   // SHIELDWAVEEFFECT
        private const int BioAugmentationTypeId = 329;              // BIO_AUGMENTATION_EFFECT
        private const int WeaponEnhancementTypeId = 10000035;       // WEAPON_ENHANCEMENT (Shredder Ammo)
        private const int DamageConversionTypeId = 354;             // DAMAGE_CONVERSION (Viral Conversion)
        private const int DiseaseTypeId = 179;                      // DISEASE_EFFECT
        private const int PaintTargetTypeId = 10000045;             // PAINT_TARGET_EFFECT
        private const int PolarityFieldTypeId = 262;                // POLARITY_FIELD

        /// <summary>skilldata T4_SPY_POLARITY_FIELD: every pump of it the player owns adds PER_PUMP_MOD.</summary>
        private const int PolarityFieldSkillId = 161;

        /// <summary>skilldata T4_SNIPER_SHREDDER_AMMO; its pump shortens Shredder Ammo's interval.</summary>
        private const int ShredderAmmoSkillId = 150;
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

                case "abilities.disease":
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    if (target != null && IsHostile(player, target))
                    {
                        AttachDisease(mapChannel, player, target, info);
                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

                case TraitorModule:
                case HackModule:
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;
                    var turned = target != null && (actionInfo.Module == HackModule
                        ? AttachHack(mapChannel, player, target, info)
                        : AttachTraitor(mapChannel, player, target, info));

                    // Hit: the client's targetGameEffect announces TRAITOR_EFFECT / HACKED_EFFECT on it.
                    if (turned)
                    {
                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

                case MindControlModule:
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    // MindControlAction's DoAbility announces each (entityId, effectTypeId) hit as an attach.
                    if (target != null && AttachMindControl(mapChannel, player, target, info))
                    {
                        ManifestationManager.Instance.EnterCombat(client);
                        recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.EffectAttach);
                        Hit(recovery, target, MindControlTypeId);
                    }

                    break;
                }

                case "abilities.cloakwave":
                    CloakWave(mapChannel, player, info, recovery);
                    break;

                case "abilities.tacticalevasion":
                    TacticalEvasion(mapChannel, client, player, info, recovery);
                    break;

                case "abilities.cure":
                    recovery = Cure(mapChannel, player, action, info);
                    break;

                case "abilities.corpseexplode":
                {
                    // The one ability that wants a dead target: a biological body, which the
                    // client has already checked before asking.
                    var corpse = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    if (IsBiologicalCorpse(corpse))
                    {
                        ImmolateCorpse(mapChannel, player, corpse, info);
                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, corpse);
                    }

                    break;
                }

                case "abilities.reflection":
                    AttachReflection(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.conversion":
                    AttachConversion(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.calledshot":
                case "abilities.feedback":
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    if (target != null && IsHostile(player, target))
                    {
                        if (actionInfo.Module == "abilities.calledshot")
                            ArmCalledShot(mapChannel, client, player, target, info);
                        else
                            AttachFeedback(mapChannel, player, target, info);

                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

                case "abilities.selfdestruct":
                    ArmSelfDestruct(mapChannel, player, info);
                    Hit(recovery, player);
                    break;

                case "abilities.scatterbombs":
                    DropScatterbombs(mapChannel, client, player, info);
                    break;

                case "abilities.firesupport":
                    CallFireSupport(mapChannel, client, player, info, action, recovery);
                    break;

                case "abilities.controlledfission":
                case "abilities.explodingnanites":
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    if (target != null && IsHostile(player, target))
                    {
                        if (actionInfo.Module == "abilities.controlledfission")
                            AttachControlledFission(mapChannel, player, target, info);
                        else
                            AttachExplosiveNanites(mapChannel, player, target, info);

                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

                case "abilities.painttarget":
                case "abilities.polarityfield":
                {
                    var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                    if (target != null && IsHostile(player, target))
                    {
                        if (actionInfo.Module == "abilities.painttarget")
                            AttachTargetPainting(mapChannel, player, target, info);
                        else
                            AttachPolarityField(mapChannel, player, target, info);

                        ManifestationManager.Instance.EnterCombat(client);
                        Hit(recovery, target);
                    }

                    break;
                }

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

                case "abilities.shieldextender":
                {
                    var ally = FriendlyTarget(mapChannel, player, action);

                    if (ally != null)
                    {
                        AttachShieldExtender(mapChannel, player, ally, info);
                        Hit(recovery, ally);
                    }

                    break;
                }

                case "abilities.shieldwave":
                    foreach (var member in SquadWithin(mapChannel, player, info.Get(AbilityProperty.RadiusAroundSource, 25)))
                    {
                        var shield = NewEffect(mapChannel, player, info, ShieldWaveTypeId, info.Get(AbilityProperty.Duration, 120));
                        shield.AbsorbPercent = 100;
                        shield.AbsorbPool = new AbsorbPool { Remaining = Scale(player.Level, info.Get(AbilityProperty.EffectModifier, 300), info.Get(AbilityProperty.AttrScaleType)) };
                        shield.AllowDetach = true;
                        GameEffectManager.Instance.Attach(mapChannel, member, shield);
                        Hit(recovery, member);
                    }

                    break;

                case "abilities.bioaugmentation":
                {
                    var ally = FriendlyTarget(mapChannel, player, action);

                    if (ally != null)
                    {
                        AttachBioAugmentation(mapChannel, player, ally, info);
                        Hit(recovery, ally);
                    }

                    break;
                }

                case "abilities.weaponenhancement":
                {
                    var ally = FriendlyTarget(mapChannel, player, action);

                    if (ally != null)
                    {
                        AttachShredderAmmo(mapChannel, player, ally, info);
                        Hit(recovery, ally);
                    }

                    break;
                }

                case "abilities.damageconversion":
                {
                    // The client picks the tooltip by the effect's level - (354, damage type) -
                    // so the level is the damage type the virulent damage becomes.
                    var to = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
                    var conversion = NewEffect(mapChannel, player, info, DamageConversionTypeId, info.Get(AbilityProperty.Duration, 30));
                    conversion.EffectLevel = (uint)to;
                    conversion.ConvertVirulentTo = to;
                    conversion.AllowDetach = true;
                    GameEffectManager.Instance.Attach(mapChannel, player, conversion);
                    Hit(recovery, player);
                    break;
                }

                case "abilities.critwave":
                    foreach (var member in SquadWithin(mapChannel, player, info.Get(AbilityProperty.RadiusAroundSource, 25)))
                    {
                        var wave = NewEffect(mapChannel, player, info, CritWaveTypeId, info.Get(AbilityProperty.Duration, 120));
                        wave.CritChancePercent = info.Get(AbilityProperty.EffectModifier, 50);
                        wave.Tooltip["critAmt"] = wave.CritChancePercent;
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
        /// Who a friendly ability lands on: the player targeted, or the performer when the target
        /// is nobody, themselves, or not a player; null when the one targeted has died or left
        /// during the windup.
        /// </summary>
        private static Manifestation FriendlyTarget(MapChannel mapChannel, Manifestation player, ActionData action)
        {
            if (action.TargetId == 0 || action.TargetId == player.EntityId)
                return player;

            var target = ResolveTarget(mapChannel, action.TargetId);

            if (target == null)
                return null;

            if (!(target is Manifestation ally))
                return player;

            return ally.State == CharacterState.Dead ? null : ally;
        }

        /// <summary>An effect running for a number of milliseconds rather than seconds - the *_MS properties.</summary>
        private static void RunForMs(GameEffect effect, int milliseconds)
        {
            effect.ExpiresTick = Environment.TickCount64 + Math.Max(1000, milliseconds);
        }

        /// <summary>
        /// Shield Extender: a bubble on a friend and the squad within EFFECT_RADIUS of them, that
        /// takes EFFECT_MODIFIER percent of every hit until it has taken EFFECT_DAMAGE_MAX or
        /// EFFECT_DURATION_MS has passed. One pool for the whole bubble, shared by the target's
        /// SHIELD_EXTENDER_SOURCE and the copies (SHIELD_EXTENDER_SHIELDED) on those in range,
        /// re-read every second as they move.
        /// </summary>
        private static void AttachShieldExtender(MapChannel mapChannel, Manifestation player, Manifestation ally, ActionLevelInfo info)
        {
            var shield = NewEffect(mapChannel, player, info, ShieldExtenderSourceTypeId, null);

            RunForMs(shield, info.Get(AbilityProperty.EffectDurationMs, 45000));
            shield.AbsorbPercent = info.Get(AbilityProperty.EffectModifier, 15);
            shield.AbsorbPool = new AbsorbPool { Remaining = info.Get(AbilityProperty.EffectDamageMax, 120) };
            shield.AllowDetach = true;
            shield.AuraRadius = info.Get(AbilityProperty.EffectRadius, 6);
            shield.AuraChildTypeId = ShieldExtenderShieldedTypeId;
            shield.TickIntervalMs = 1000;
            shield.NextTickTick = Environment.TickCount64;

            GameEffectManager.Instance.Attach(mapChannel, ally, shield);
        }

        /// <summary>
        /// Bio Augmentation: EFFECT_MODIFIER percent more of ATTRIBUTE_ID (Health, Power, Body,
        /// Mind or Spirit by pump) on a friend for EFFECT_DURATION_MS - fifteen minutes. One at a
        /// time on a target; a second replaces the first. Applied through the target's stats, so
        /// +Body is also the health and armour Body brings.
        /// </summary>
        private static void AttachBioAugmentation(MapChannel mapChannel, Manifestation player, Manifestation ally, ActionLevelInfo info)
        {
            var attribute = (Attributes)info.Get(AbilityProperty.AttributeId, (int)Attributes.Health);
            var percent = info.Get(AbilityProperty.EffectModifier, 30);
            var augmentation = NewEffect(mapChannel, player, info, BioAugmentationTypeId, null);

            RunForMs(augmentation, info.Get(AbilityProperty.EffectDurationMs, 900000));
            augmentation.AttributeId = attribute;
            augmentation.AttributePercent = percent;
            augmentation.AllowDetach = true;
            augmentation.TooltipAttrId = (int)attribute;                // "Increases %(attrId)s by %(amount)s"
            augmentation.Tooltip["amount"] = $"{percent}%";

            GameEffectManager.Instance.Attach(mapChannel, ally, augmentation);
        }

        /// <summary>
        /// Shredder Ammo: a friend's weapon hits do DAMAGE_AMOUNT (scaled to the caster's level)
        /// of the pump's DAMAGE_TYPE extra, once per interval, for DURATION_MS. The client's text
        /// has the interval shorten with every pump the caster owns but gives no figures, and
        /// the data has none: this uses 5 s at pump 1 and a second less per pump, 1 s at pump 5.
        /// </summary>
        private static void AttachShredderAmmo(MapChannel mapChannel, Manifestation player, Manifestation ally, ActionLevelInfo info)
        {
            var pump = Math.Max(1, ManifestationManager.SkillPump(player, ShredderAmmoSkillId));
            var ammo = NewEffect(mapChannel, player, info, WeaponEnhancementTypeId, null);

            RunForMs(ammo, info.Get(AbilityProperty.DurationMs, 30000));
            ammo.WeaponBonusMin = info.Get(AbilityProperty.DamageAmountMin);
            ammo.WeaponBonusMax = Math.Max(ammo.WeaponBonusMin, info.Get(AbilityProperty.DamageAmountMax, ammo.WeaponBonusMin));
            ammo.WeaponBonusType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            ammo.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            ammo.WeaponBonusIntervalMs = ShredderIntervalMs(pump);
            ammo.AllowDetach = true;

            GameEffectManager.Instance.Attach(mapChannel, ally, ammo);
        }

        /// <summary>Shredder Ammo's interval for a caster with this many pumps in the skill; see AttachShredderAmmo.</summary>
        public static int ShredderIntervalMs(int pump)
        {
            return Math.Max(1000, 5000 - 1000 * (Math.Max(1, pump) - 1));
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
        /// the data, so none here. THREAT_MODIFIER_PERCENT raises the threat the Grenadier draws
        /// from every creature they hurt (Managers.Threat). The client's SacrificeAbility is a toggle that names no
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
            effect.ThreatModifierPercent = info.Get(AbilityProperty.ThreatModifierPercent);
            effect.Tooltip["threatMod"] = effect.ThreatModifierPercent;

            GameEffectManager.Instance.Attach(mapChannel, player, effect);
        }

        /// <summary>
        /// Disease: DISEASE_EFFECT on an enemy for DURATION (20 s), what it does by pump (the
        /// effect's per-level tooltips): P1-P3 ATTRIBUTE_ID (Spirit, Body, Mind) -ATTRIBUTE_MAX_CHANGE
        /// (70) percent, "Spirit: %(modifier)s%%"; P4 EFFECT_HEALTH_REGEN_MODIFIER and
        /// EFFECT_POWER_REGEN_MODIFIER 0, "Health Regen: Disabled / Power Regen: Disabled"; P5
        /// HEALING_MODIFIER 0, "All Healing: Disabled". All three are real on a creature as on a
        /// player: the attribute's maximum and value are cut (GameEffectManager.ApplyAttribute,
        /// put back when it ends), RegenAmount gives 0 health and power regeneration, and
        /// ActorManager.Heal (GameEffectManager.HealingBlocked) refuses every heal. Whatever
        /// reads a creature's attributes, regenerates it or heals it picks them up.
        /// </summary>
        private static void AttachDisease(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, DiseaseTypeId, info.Get(AbilityProperty.Duration, 20));

            effect.IsBuff = false;

            if (info.Has(AbilityProperty.AttributeId))
            {
                var percent = -info.Get(AbilityProperty.AttributeMaxChange, 70);

                effect.AttributeId = (Attributes)info.Get(AbilityProperty.AttributeId);
                effect.AttributePercent = percent;
                effect.TooltipAttrId = info.Get(AbilityProperty.AttributeId);
                effect.Tooltip["modifier"] = percent;
            }

            if (info.Has(AbilityProperty.EffectHealthRegenModifier))
                effect.HealthRegenPercent = info.Get(AbilityProperty.EffectHealthRegenModifier) - 100;

            if (info.Has(AbilityProperty.EffectPowerRegenModifier))
                effect.PowerRegenPercent = info.Get(AbilityProperty.EffectPowerRegenModifier) - 100;

            if (info.Has(AbilityProperty.HealingModifier) && info.Get(AbilityProperty.HealingModifier) == 0)
                effect.BlocksHealing = true;

            GameEffectManager.Instance.Attach(mapChannel, target, effect);
        }

        /// <summary>
        /// Target Painting: PAINT_TARGET_EFFECT on an enemy for DURATION (15 s).
        /// EFFECT_ARMOR_PIERCE_PERCENT (0 / 10 / 20 / 30 / 40) of every hit on it goes past its
        /// armour to health, whoever hits it. The tooltip ("Reduced Cover: %(coverMod)s%%, Armor
        /// Recharge: %(armorMod)s%%, Armor Piercing: +%(pierceMod)s%%") also shows
        /// EFFECT_COVER_MODIFIER (50 / 40 / 30 / 20 / 0), the percent of the target's cover that
        /// still counts against ranged hits (Managers.Cover), and EFFECT_ARMOR_REGEN_MODIFIER
        /// (100 / 100 / 0 / 0 / 0), the percent of its normal armour regeneration the target keeps
        /// (CreatureArmor): from pump 3 its armour does not come back while painted.
        /// </summary>
        private static void AttachTargetPainting(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            var effect = NewEffect(mapChannel, player, info, PaintTargetTypeId, info.Get(AbilityProperty.Duration, 15));

            effect.IsBuff = false;
            effect.ArmorPiercePercent = info.Get(AbilityProperty.EffectArmorPiercePercent);
            effect.CoverCountsPercent = info.Get(AbilityProperty.EffectCoverModifier);
            effect.Tooltip["coverMod"] = effect.CoverCountsPercent.Value;
            effect.Tooltip["armorMod"] = info.Get(AbilityProperty.EffectArmorRegenModifier, 100);
            effect.ArmorRegenPercent = info.Get(AbilityProperty.EffectArmorRegenModifier, 100) - 100;
            effect.Tooltip["pierceMod"] = effect.ArmorPiercePercent;

            GameEffectManager.Instance.Attach(mapChannel, target, effect);
        }

        /// <summary>
        /// Polarity Field: POLARITY_FIELD on an enemy for DURATION_MS (30 s), lowering its
        /// resistance to the pump's DAMAGE_TYPE (electric, photonic, physical, incendiary,
        /// cryogenic for pumps 1-5) by PER_PUMP_MOD (-10) for every pump of the skill the player
        /// owns - the abilities' "Proficiency: ... per pump" reading. Below zero it is a
        /// vulnerability: -30 takes (100 + 30)% damage of that type (ResistMultiplier). Only hits
        /// of that type are affected.
        /// </summary>
        private static void AttachPolarityField(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            var durationMs = info.Get(AbilityProperty.DurationMs, 30000);
            var effect = NewEffect(mapChannel, player, info, PolarityFieldTypeId, null);
            var pumps = Math.Max((int)info.Level, ManifestationManager.SkillPump(player, PolarityFieldSkillId));

            effect.IsBuff = false;
            effect.ExpiresTick = Environment.TickCount64 + durationMs;
            effect.ResistDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Electrical);
            effect.ResistModifier = info.Get(AbilityProperty.PerPumpMod, -10) * pumps;

            GameEffectManager.Instance.Attach(mapChannel, target, effect);
        }

        /// <summary>
        /// Ruin: DAMAGE_AMOUNT_MIN..MAX of DAMAGE_TYPE to one enemy every INTERVAL seconds for
        /// DURATION, scaled to the performer's level like any ability damage. The first tick is
        /// an interval in, as the tooltip's "every N seconds" reads. Pump 5's
        /// EFFECT_MOVEMENT_MODIFIER is a slow - its tooltip reads "Movement: -20%" - so the creature
        /// moves at 80% of its speed while Ruin is on it.
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

            if (info.Has(AbilityProperty.EffectMovementModifier))
                effect.MovementModifierPercent = Math.Max(1, 100 - info.Get(AbilityProperty.EffectMovementModifier));

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
        ///    at once. Pump 3 also has ATTRIBUTE_MAX_CHANGE (30) for DURATION (20 s): the squad's
        ///    Spirit +30% ("Spirit: +%(spiritBuff)s%%" on HELP) through UpdateStatsValues, as Bio
        ///    Augmentation does, and the enemies' "Spirit: -%(spiritMod)s%%" on HARM. A creature's
        ///    Spirit feeds nothing on the server, so that half is shown and has no effect.
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
            var spiritPercent = info.Get(AbilityProperty.AttributeMaxChange);

            // Pump 3's Spirit change lasts DURATION; the effect carrying it stays that long.
            var effectSeconds = instant
                ? (spiritPercent != 0 ? info.Get(AbilityProperty.Duration, 20) : InstantMarkerSeconds)
                : info.Get(AbilityProperty.Duration, 15);
            var tickMs = instant ? 0 : Math.Max(1, interval) * 1000;

            foreach (var ally in allies)
            {
                var help = NewEffect(mapChannel, player, info, ReconstructionHelpTypeId, effectSeconds);
                help.AllowDetach = true;
                help.TickScaleType = scaleType;
                help.Tooltip["spiritBuff"] = spiritPercent;

                if (spiritPercent != 0)
                {
                    help.AttributeId = Attributes.Spirit;
                    help.AttributePercent = spiritPercent;
                    help.TooltipAttrId = (int)Attributes.Spirit;
                }
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
                harm.Tooltip["spiritMod"] = spiritPercent;
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
                    var crit = CriticalHits.Resolve(player, enemy, false, CriticalHits.AttackerChance(player, false), ref rolled);
                    var amount = GameEffectManager.ApplyResist(enemy, rolled, out var resisted, damageType);
                    var taken = ActorManager.Instance.Damage(mapChannel, enemy, amount, player, damageType);
                    var tick = new GameEffectTickPacket(harm.EffectId, GameEffectTickPacket.TickKind.Damage);

                    tick.Entries.Add(new TickEntry
                    {
                        EntityId = enemy.EntityId,
                        Amount = amount,
                        Resisted = resisted,
                        DamageType = damageType,
                        IsCritical = crit,
                        DeathBlow = taken > 0 && enemy.Attributes[Attributes.Health].Current <= 0
                    });

                    CellManager.Instance.CellCallMethod(mapChannel, enemy, tick);
                }
            }
        }
    }
}
