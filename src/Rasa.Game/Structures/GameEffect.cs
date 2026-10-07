using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    /// <summary>
    /// A buff, debuff or looping gesture on an actor. TypeId is the client's gameeffectdata id
    /// (which picks the client-side effect class and its visuals), EffectId identifies this
    /// instance to the client the way an entity id identifies an entity, and EffectLevel is the
    /// pump level it was applied at. Timing is in Environment.TickCount64 so it does not depend
    /// on how often the effect worker happens to run.
    ///
    /// What an effect does is the sum of the fields below that are set. While it is on, the
    /// modifiers (damage dealt, critical chance, resistance, regeneration, maximum health, movement) are read by
    /// whoever computes the thing they modify. On each tick it may drain adrenaline, damage or
    /// heal its holder, damage everything hostile around its holder, or, as an aura, keep a copy
    /// of itself on the squad within reach. GameEffectManager runs all of it.
    /// </summary>
    public class GameEffect
    {
        public int TypeId { get; set; }
        public int EffectId { get; set; }
        public uint EffectLevel { get; set; }

        /// <summary>The action that attached it, if any; what is looked up for its numbers.</summary>
        public ActionId ActionId { get; set; }

        /// <summary>Who attached it; the actor itself for self buffs.</summary>
        public ulong SourceId { get; set; }

        /// <summary>
        /// The actor behind SourceId, for ticks that deal damage: they are credited with a kill
        /// and are what a surviving creature turns on. May have left the map by the time a tick
        /// runs; the manager checks.
        /// </summary>
        public Actor Source { get; set; }

        /// <summary>The source's level when the effect was attached; tick amounts scale to it.</summary>
        public int SourceLevel { get; set; } = 1;

        /// <summary>The actor it is on. Set by Attach.</summary>
        public Actor Holder { get; set; }

        /// <summary>Environment.TickCount64 at which it wears off; long.MaxValue for "until detached".</summary>
        public long ExpiresTick { get; set; } = long.MaxValue;

        /// <summary>Milliseconds between ticks, for effects that do something on a schedule; 0 for none.</summary>
        public int TickIntervalMs { get; set; }

        /// <summary>Environment.TickCount64 of the next tick.</summary>
        public long NextTickTick { get; set; }

        /// <summary>
        /// Whether the client is given the time left (the tooltip's 'duration', which is what puts
        /// a timer on the icon and in the tooltip). False for an effect whose end the server keeps
        /// pushing out while a condition holds - the Shield Drone's shield lasts as long as the
        /// Bane stays under it - which would otherwise show a timer running out on something that
        /// is not ending: the client's icon takes its timer from the attach and never re-reads it.
        /// </summary>
        public bool ShowsDuration { get; set; } = true;

        /// <summary>Buff or debuff, for the client's icon colouring and right-click rules.</summary>
        public bool IsBuff { get; set; } = true;

        /// <summary>
        /// The world's doing and nobody's: lava (Managers.LavaDamage). What keeps debuffs off an
        /// actor does not keep this off (GameEffectManager.Attach), and what takes debuffs away
        /// does not take it (AbilityManager.DebuffsOn): only getting out of it does.
        /// </summary>
        public bool Environmental { get; set; }

        /// <summary>
        /// Whether the attach packet itself announces the effect (plays its attach FX and posts
        /// the status icon). An ability's effects are attached quietly and announced by the
        /// ability's own recovery on the client - PerformRecovery names the entities hit and the
        /// client calls AnnounceGameEffectAttach on each - so the visuals land with the action.
        /// Anything not announced by an action says so here.
        /// </summary>
        public bool AnnounceOnAttach { get; set; } = true;

        /// <summary>
        /// Whether a weapon hit that puts this effect on a creature may announce it in place of
        /// the attach (Managers.HitEffects): attached quietly, named in the hit's
        /// targetEffectIds, and seen when the hit is. No for an effect something is about to be
        /// done with that needs its visuals there already - a tick that draws from its FX.
        /// </summary>
        public bool AnnounceWithHit { get; set; } = true;

        /// <summary>
        /// Whether a client meeting the holder after the attach has the effect announced to it
        /// (GameEffectManager.ShowEffectsTo). Most want it whatever AnnounceOnAttach says: an
        /// effect attached quietly was announced by its ability's recovery, which a latecomer
        /// never saw, and the announce is what starts its visuals - an aura's glow, a shield, a
        /// morph's mesh. What an announce plays only on arrival - a summon's spawn flash, a
        /// corpse rising - says no here.
        /// </summary>
        public bool AnnounceToNewcomers { get; set; } = true;

        /// <summary>
        /// The client may ask for this effect to be removed (RequestDetachGameEffect) - a toggle
        /// like sprint, or any buff the player right-clicks away. Debuffs stay.
        /// </summary>
        public bool AllowDetach { get; set; }

        /// <summary>
        /// A weapon skill's standing effect (ManifestationManager.SyncSkillPassives): there for as
        /// long as the player has the skill, one per skill rather than one per type, and seen by
        /// the player's own client alone - it is how that client learns what its heat meter and
        /// reload bar should do, and nobody else's client has any use for it.
        /// </summary>
        public bool IsSkillPassive { get; set; }

        /// <summary>
        /// Told to the holder's own client and nobody else's, like a skill's standing effect but
        /// one of a kind: an effect whose client class changes the viewer's own windows whoever it
        /// is attached to (LockWageredItemEffect puts the padlock on the prestige window of every
        /// client that hears of it).
        /// </summary>
        public bool OwnerOnly { get; set; }

        /// <summary>
        /// Kept on the server and told to no client: an effect the server needs whose type the
        /// client has no class for (a grub's cocoon, CreatureSummons). Its attach, detach and
        /// its introduction to newcomers are not sent.
        /// </summary>
        public bool ServerOnly { get; set; }

        /// <summary>
        /// Values for the client's tooltip beyond the fixed ones (duration, damage type, buff
        /// flags), keyed as the effect's tooltip format string names them - dmgMod, resistMod,
        /// healMin and so on. Every key the string uses has to be here or the client's %
        /// formatting throws.
        /// </summary>
        public Dictionary<string, object> Tooltip { get; } = new Dictionary<string, object>();

        #region Modifiers while on

        /// <summary>Movement speed as a percent of normal while the effect is on: 120 is a fifth faster. 0 means no change.</summary>
        public int MovementModifierPercent { get; set; }

        /// <summary>Percent added to the damage the holder deals with weapons and abilities; negative reduces it.</summary>
        public int DamageDealtPercent { get; set; }

        /// <summary>Added to the holder's resistance - to everything, or to ResistDamageType alone; see GameEffectManager.ResistMultiplier. Negative makes it a vulnerability.</summary>
        public int ResistModifier { get; set; }

        /// <summary>The one damage type ResistModifier applies to (Polarity Field); 0 for all of them.</summary>
        public DamageType ResistDamageType { get; set; }

        /// <summary>The holder cannot be healed while it is on (Disease P5, "All Healing: Disabled").</summary>
        public bool BlocksHealing { get; set; }

        /// <summary>
        /// Percent added to the threat the holder generates - "Perceived Threat: X%"
        /// (Sacrifice's THREAT_MODIFIER_PERCENT: +30 / +75 / +300); negative draws less.
        /// </summary>
        public int ThreatModifierPercent { get; set; }

        /// <summary>No debuff may be attached to the holder while it is on (Cure P4, "Protect from Debuffs").</summary>
        public bool BlocksDebuffs { get; set; }

        #region Seeing and being seen

        /// <summary>
        /// The holder cannot be seen while it is on (Cloak Wave): creatures do not notice them,
        /// lose them if they were fighting them, and players outside their squad are not told
        /// they are there. Managers.Detection keeps it.
        /// </summary>
        public bool Hides { get; set; }

        /// <summary>
        /// The holder cannot see while it is on (Tactical Evasion's mag flash): a creature drops
        /// whatever it was fighting and notices nobody until it wears off.
        /// </summary>
        public bool Blinds { get; set; }

        /// <summary>
        /// Percent taken off ranged damage landing on the holder (Tactical Evasion's smoke
        /// screen, "Incoming ranged damage reduced by X%"); melee is unaffected.
        /// </summary>
        public int IncomingRangedPercent { get; set; }

        /// <summary>
        /// Percent of the shots aimed at the holder that miss it (Chaff's
        /// DEFENSIVE_TOHIT_MODIFIER, "harder to hit"); melee is unaffected. The strongest on the
        /// holder counts.
        /// </summary>
        public int MissPercent { get; set; }

        #endregion

        /// <summary>
        /// Percent of the holder's cover that still counts against ranged hits (Target Painting's
        /// EFFECT_COVER_MODIFIER, "Reduced Cover: 50%" .. 0%); null for no change. See Managers.Cover.
        /// </summary>
        public int? CoverCountsPercent { get; set; }

        /// <summary>
        /// Mind Control's pump on the creature holding it (1 Frighten .. 5 Infectious), 0 for any
        /// other effect. BehaviorManager.MayFight reads it: see AbilityManager.MindControl.
        /// </summary>
        public int MindControlPump { get; set; }

        /// <summary>
        /// The player holding it may not attack - no weapon fire, no hostile ability - while it is
        /// on: Mind Control P2-P5 on an enemy player (Pvp).
        /// </summary>
        public bool NoAttack { get; set; }

        /// <summary>
        /// The player holding it may not help anyone else - no ability aimed at another player, no
        /// squad wave - while it is on: Mind Control P4-P5 on an enemy player (Pvp).
        /// </summary>
        public bool NoAssist { get; set; }

        /// <summary>
        /// The player holding it may not hurt its Source or the Source's side while it is on, and
        /// it comes off when the Source hurts them: Traitor on an enemy player (Pvp.Restrained).
        /// </summary>
        public bool Restrains { get; set; }

        /// <summary>
        /// Comes off the creature holding it as soon as it takes damage: Hack's hold on an enemy
        /// player's machine (AbilityManager.HackPet).
        /// </summary>
        public bool BreaksOnDamage { get; set; }

        /// <summary>
        /// Percent change to Body, Mind and Spirit together while on - Rez Trauma (REZ_SICKNESS)'s
        /// penalty after a death (PlayerDeath). Players only, as AttributePercent is.
        /// </summary>
        public int PrimaryAttributesPercent { get; set; }

        /// <summary>How many deaths a Rez Trauma stands for (REZ_SICKNESS_MAX_STACK at most).</summary>
        public int Stacks { get; set; }

        /// <summary>Percent chance that a stun or knockback on the holder does not land (Graviton Armor). See Managers.PlayerCrowdControl.</summary>
        public int KnockbackStunResistPercent { get; set; }

        /// <summary>Percent of every hit on the holder that goes past its armour straight to health (Target Painting).</summary>
        public int ArmorPiercePercent { get; set; }

        /// <summary>Percent added to the holder's health and power regeneration: 400 is five times the rate.</summary>
        public int RegenPercent { get; set; }

        /// <summary>Percent added to the holder's armour regeneration.</summary>
        public int ArmorRegenPercent { get; set; }

        /// <summary>Percent added to the holder's health regeneration alone (Bio Armor).</summary>
        public int HealthRegenPercent { get; set; }

        /// <summary>Percent added to the holder's power regeneration alone (Mech Armor).</summary>
        public int PowerRegenPercent { get; set; }

        /// <summary>Percent of the damage the holder takes from an attacker that is reflected back at it (Reflective Armor, Reflection).</summary>
        public int ReflectPercent { get; set; }

        /// <summary>
        /// The damage types ReflectPercent answers (Reflection, which reflects one more type per
        /// pump); empty for an effect that reflects everything, as Reflective Armor does.
        /// </summary>
        public List<DamageType> ReflectTypes { get; } = new List<DamageType>();

        /// <summary>
        /// Percent of the damage the holder takes that is healed onto their squad around them
        /// (Conversion); 0 for none. HealRadius is how far that reaches.
        /// </summary>
        public int HealPercentOfDamage { get; set; }
        public float HealRadius { get; set; }

        /// <summary>An attribute raised (or lowered) by AttributePercent while on - Bio Augmentation's Health, Power, Body, Mind or Spirit.</summary>
        public Attributes? AttributeId { get; set; }

        /// <summary>Percent change to AttributeId; players only, applied in ManifestationManager.UpdateStatsValues.</summary>
        public int AttributePercent { get; set; }

        /// <summary>
        /// Percent of each incoming hit the effect takes out of AbsorbPool instead of the holder:
        /// 100 for Shield Wave, 15-60 for Shield Extender. The effect ends when the pool is empty.
        /// </summary>
        public int AbsorbPercent { get; set; }

        /// <summary>What the shield has left to absorb. Shared by an aura and its copies - one bubble, one pool.</summary>
        public AbsorbPool AbsorbPool { get; set; }

        /// <summary>While on, the holder takes nothing from any hit and every one shows "Immune" (Managers.DamageImmunity).</summary>
        public bool ImmuneToAllDamage { get; set; }

        /// <summary>While on, a hit of one of these types takes nothing from the holder and shows "Immune".</summary>
        public List<DamageType> ImmuneDamageTypes { get; } = new List<DamageType>();

        /// <summary>
        /// Shredder Ammo: extra damage the holder's weapon hits do, at most once per
        /// WeaponBonusIntervalMs, rolled WeaponBonusMin..Max and scaled like ability damage.
        /// </summary>
        public int WeaponBonusMin { get; set; }
        public int WeaponBonusMax { get; set; }
        public DamageType WeaponBonusType { get; set; } = DamageType.Physical;
        public int WeaponBonusIntervalMs { get; set; }

        /// <summary>Environment.TickCount64 before which the weapon bonus does not fire again.</summary>
        public long WeaponBonusReadyAt { get; set; }

        /// <summary>Viral Conversion: the type the holder's virulent weapon damage becomes; 0 for none.</summary>
        public DamageType ConvertVirulentTo { get; set; }

        /// <summary>The attribute the client's tooltip names for "%(attrId)s"; 1 (Body) when there is none.</summary>
        public int TooltipAttrId { get; set; } = 1;

        /// <summary>Percent added to the holder's chance of a critical hit (Crit Wave); see CriticalHits.</summary>
        public int CritChancePercent { get; set; }

        /// <summary>A stun: while it is on, a creature neither moves nor attacks; see Stuns.</summary>
        public bool IsStun { get; set; }

        /// <summary>A freeze: while it is on, a creature cannot move but can still attack; see CrowdControl.</summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// What it does to a player that their item modules can resist, when the effect does not
        /// say so otherwise: DamageType.Root for a creature's web or net, DamageType.Blind for
        /// its flash. 0 for most; a root (IsRoot), a blinding (Blinds) and a slow
        /// (MovementModifierPercent under 100) are known without it. See
        /// ItemModuleBonuses.ControlKindOf.
        /// </summary>
        public DamageType ControlKind { get; set; }

        /// <summary>EMP crit's Armor Suppression: while on, the holder's armour stops nothing and every hit goes to health.</summary>
        public bool SuppressesArmor { get; set; }

        /// <summary>Percent added to the damage of the holder's ranged attacks; negative reduces it (Laser crit).</summary>
        public int RangedDamagePercent { get; set; }

        /// <summary>Whether it changes any regeneration rate, which the holder's client has to be told.</summary>
        public bool ChangesRegen => RegenPercent != 0 || ArmorRegenPercent != 0 || HealthRegenPercent != 0 || PowerRegenPercent != 0;

        /// <summary>Percent change to the holder's maximum health while on; negative lowers it.</summary>
        public int MaxHealthPercent { get; set; }

        /// <summary>The points of maximum health actually added (or taken) when it was attached, so exactly that is put back.</summary>
        public int MaxHealthApplied { get; set; }

        /// <summary>
        /// On an actor that is not a player: the points AttributePercent actually moved
        /// AttributeId's maximum by when it was attached (Disease's -70% Body), so exactly that
        /// is put back. A player's attributes are worked out in UpdateStatsValues instead.
        /// </summary>
        public int AttributeApplied { get; set; }

        #endregion

        #region Ticks

        /// <summary>Percent of the actor's maximum chi (adrenaline) taken per second while the effect is on; the effect ends when the bar is empty.</summary>
        public double AdrenalineDrainPercentPerSecond { get; set; }

        /// <summary>The fraction of a point of drain carried to the next tick, so 1.5 a second takes 3 every two seconds and not 2.</summary>
        public double DrainCarry { get; set; }

        /// <summary>Damage rolled on each tick, before level scaling; 0 for no damage tick.</summary>
        public int TickDamageMin { get; set; }
        public int TickDamageMax { get; set; }
        public DamageType TickDamageType { get; set; } = DamageType.Physical;

        /// <summary>DAMAGE_SCALE_TYPE for the tick's damage and healing; see AbilityManager.Scale.</summary>
        public int TickScaleType { get; set; }

        /// <summary>Metres around the holder the tick's damage reaches; 0 means the holder is what is damaged.</summary>
        public float TickRadius { get; set; }

        /// <summary>
        /// A TickRadius effect whose client class's OnTick takes the damage list (a Stalker's
        /// egg charge, StalkerEggChargeEffect): the hits go out as its tick rather than through
        /// AnnounceDamage, which is for a class whose OnTick takes nothing (Scourge).
        /// </summary>
        public bool TickRadiusAsTick { get; set; }

        /// <summary>Healing on each tick, before level scaling; 0 for none.</summary>
        public int TickHealMin { get; set; }
        public int TickHealMax { get; set; }

        /// <summary>Adrenaline given to the holder on each tick; 0 for none.</summary>
        public int TickAdrenaline { get; set; }

        #endregion

        #region Aura

        /// <summary>
        /// Metres around the holder within which squad members carry a copy of this effect. The
        /// copies (Children) come and go with the squad as it moves, on the effect's tick, and go
        /// with the effect when it ends.
        /// </summary>
        public float AuraRadius { get; set; }

        /// <summary>The gameeffectdata id the copies use; the client often has a separate class for the aura's recipients.</summary>
        public int AuraChildTypeId { get; set; }

        /// <summary>
        /// Whether a new copy is announced through this effect's tick (GameEffectTick with the
        /// recipients' ids, as RageSourceEffect.OnTick expects) rather than by its own attach.
        /// </summary>
        public bool AuraTickAnnounces { get; set; }

        /// <summary>The aura this is a copy of, or null.</summary>
        public GameEffect Parent { get; set; }

        /// <summary>The copies this aura has out.</summary>
        public List<GameEffect> Children { get; } = new List<GameEffect>();

        #endregion

        #region Critical Death

        /// <summary>
        /// Run by GameEffectManager after the effect has been taken off because its time ran out -
        /// not when something else detaches it. The Critical Death window uses it to kill the
        /// creature when nobody finished it, and the finisher's death animation to hand out the
        /// kill once it has played.
        /// </summary>
        public Action<MapChannel, Actor, GameEffect> OnExpired { get; set; }

        /// <summary>
        /// Called once it has come off, however it ended - expired, detached on request, replaced,
        /// or cleared as its holder left the map (mapChannel null then if there was none).
        /// </summary>
        public Action<MapChannel, Actor, GameEffect> OnDetached { get; set; }

        /// <summary>The arguments it was attached with (the client effect's OnAttach), for a client that meets its holder later.</summary>
        public List<object> AttachArgs { get; set; } = new List<object>();

        /// <summary>The player whose finisher is winding up on this CRIT_PREDEATH_EFFECT; 0 while nobody has claimed it.</summary>
        public ulong FinisherId { get; set; }

        /// <summary>Whether a finisher's claim has already held this window open past its time.</summary>
        public bool FinisherExtended { get; set; }

        #endregion

        #region Explosive Nanites

        /// <summary>
        /// Damage done to the holder each time it takes damage, rolled OnDamagedMin..Max (scaled
        /// like ability damage) of OnDamagedType, at most OnDamagedCharges times and no more
        /// often than every OnDamagedIntervalMs. The effect ends with its last charge.
        /// </summary>
        public int OnDamagedMin { get; set; }
        public int OnDamagedMax { get; set; }
        public DamageType OnDamagedType { get; set; }
        public int OnDamagedCharges { get; set; }
        public int OnDamagedIntervalMs { get; set; }
        public long OnDamagedReadyAt { get; set; }

        /// <summary>Self Destruct: where the holder is sent back to when it detonates.</summary>
        public System.Numerics.Vector3? ReturnTo { get; set; }

        #region Called Shot and Feedback

        /// <summary>
        /// Run the next time the holder takes damage, from anyone, and then cleared: Called Shot's
        /// aim, which lies on its target until something wounds it. The effect is taken off before
        /// this runs, so what it does cannot set it off again.
        /// </summary>
        public Action<MapChannel, Actor, GameEffect> OnDamaged { get; set; }

        /// <summary>
        /// What a creature's action cooldowns are multiplied by while this is on it (Called Shot:
        /// Arm, 2.0 for half the attack rate); 0 for no change.
        /// </summary>
        public double AttackRateModifier { get; set; }

        /// <summary>
        /// Whether the holder's own hostile actions, or its friendly ones, set this effect off
        /// (Feedback); the damage is the Tick fields, without a TickIntervalMs to tick on.
        /// </summary>
        public bool ActsOnHostile { get; set; }
        public bool ActsOnFriendly { get; set; }

        #endregion

        /// <summary>
        /// A tick of the effect's own, run by GameEffectManager in place of the standard ones when
        /// set - for an effect whose tick has a shape of its own (Lightning's storm).
        /// </summary>
        public Action<MapChannel, Actor, GameEffect> OnTick { get; set; }

        #endregion

        #region Paused

        /// <summary>
        /// Its clock is stopped (GameEffectManager.Pause): it does not run out and does not tick,
        /// and the client's tooltip says "Paused" in place of the timer. What it changes while it
        /// is on stays changed.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>While paused: the milliseconds it had left when it was stopped; -1 for an effect with no end.</summary>
        public long PausedRemainingMs { get; private set; }

        /// <summary>While paused: the milliseconds to its next tick when it was stopped.</summary>
        public long PausedNextTickMs { get; private set; }

        /// <summary>Stops the clock at now. False if it was already stopped.</summary>
        public bool Freeze(long now)
        {
            if (IsPaused)
                return false;

            PausedRemainingMs = HasDuration ? Math.Max(0, ExpiresTick - now) : -1;
            PausedNextTickMs = TickIntervalMs > 0 ? Math.Max(0, NextTickTick - now) : 0;
            IsPaused = true;

            return true;
        }

        /// <summary>Starts the clock again at now with the time it had left. False if it was not stopped.</summary>
        public bool Thaw(long now)
        {
            if (!IsPaused)
                return false;

            if (PausedRemainingMs >= 0)
                ExpiresTick = now + PausedRemainingMs;

            if (TickIntervalMs > 0)
                NextTickTick = now + PausedNextTickMs;

            IsPaused = false;

            return true;
        }

        #endregion

        public bool IsExpired => !IsPaused && Environment.TickCount64 >= ExpiresTick;

        public bool TickDue => !IsPaused && TickIntervalMs > 0 && Environment.TickCount64 >= NextTickTick;

        public bool HasDuration => ExpiresTick != long.MaxValue;

        /// <summary>Milliseconds left - held while paused; 0 for an effect with no end.</summary>
        public long RemainingMs => !HasDuration ? 0 : IsPaused ? PausedRemainingMs : Math.Max(0, ExpiresTick - Environment.TickCount64);

        /// <summary>Whole seconds left, for the client's tooltip; 0 for an effect with no end.</summary>
        public int RemainingSeconds => (int)(RemainingMs / 1000);

        /// <summary>Whether a tick of this effect does anything besides announce itself.</summary>
        public bool TicksDoWork => OnTick != null || AdrenalineDrainPercentPerSecond > 0 || TickDamageMax > 0 || TickHealMax > 0 || TickAdrenaline > 0 || AuraRadius > 0;
    }

    /// <summary>A shield's remaining absorption, shared by everything the one shield covers.</summary>
    public class AbsorbPool
    {
        public int Remaining { get; set; }
    }
}
