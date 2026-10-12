using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// What a player's critical hit does to a creature besides the extra damage, by the hit's
    /// damage type - the classes in the client's gameeffects/criteffects.py:
    ///
    ///  - Ice: CRIT_ICE 3, a StunEffect, "Frozen" (Stuns).
    ///  - Sonic: CRIT_SONIC 7, a KnockbackEffect, "Stunned" (CrowdControl).
    ///  - Virulent: CRIT_VIRULENT 10000018, "Crippled: %(snareMod)s%% Movement" (CrowdControl).
    ///  - Fire: CRIT_FIRE 2, a DamageOverTime, "Fire Damage: %(dmgAmt)s every %(interval)s sec." -
    ///    FireDotPercent of the crit's damage as fire, every FireDotIntervalMs for FireDotMs.
    ///  - EMP: CRIT_EMP 417, "Armor Suppression" - for EmpSuppressMs the creature's armour stops
    ///    nothing: every hit goes straight to its health (GameEffect.SuppressesArmor).
    ///  - Laser: CRIT_LIGHT 6, "Ranged Damage: %(dmgMod)s%%" - the creature's ranged attacks do
    ///    LaserRangedDamagePercent less for LaserMs.
    ///  - Electric: CRIT_ELECTRIC 418, whose OnTick(target, arcTargets) draws its arcs from the
    ///    creature to the ids it is given. Up to ElectricArcTargets other hostile creatures within
    ///    ElectricArcRadius metres of it take ElectricArcPercent of the crit's damage as
    ///    electrical, floated through the effect's AnnounceDamage - once the hit that set it off
    ///    has been played, where the hit announces the effect (Arc).
    ///  - Physical: CRIT_PHYSICAL 131's client class is an empty DamageEffect - no tooltip, no
    ///    icon, no behaviour - so a Physical crit is its extra damage and nothing more.
    ///
    /// Each effect goes on quietly and is named in the hit that caused it, where the hit is one
    /// that can name it (HitEffects), so the client shows it as the hit lands.
    ///
    /// The client has the effects and their tooltips but none of the numbers; every figure
    /// below is chosen here.
    /// </summary>
    public static class CritEffects
    {
        public const int CritFireTypeId = 2;        // CRIT_FIRE
        public const int CritLightTypeId = 6;       // CRIT_LIGHT
        public const int CritEmpTypeId = 417;       // CRIT_EMP

        public const int FireDotPercent = 10;
        public const int FireDotIntervalMs = 1000;
        public const int FireDotMs = 5000;

        public const int EmpSuppressMs = 5000;

        public const int LaserRangedDamagePercent = 25;
        public const int LaserMs = 6000;

        public const int CritElectricTypeId = 418;  // CRIT_ELECTRIC
        public const int ElectricArcTargets = 1;
        public const float ElectricArcRadius = 10f;
        public const int ElectricArcPercent = 50;

        /// <summary>How long the CRIT_ELECTRIC effect stays on for its arc FX to play out.</summary>
        private const int ElectricEffectMs = 1500;

        /// <summary>An arc's damage: ElectricArcPercent of the crit's, at least 1.</summary>
        public static int ArcDamage(int critDamage) => Math.Max(1, critDamage * ElectricArcPercent / 100);

        /// <summary>A Fire crit's burn per tick: FireDotPercent of the crit's damage, at least 1.</summary>
        public static int FireTick(int critDamage) => Math.Max(1, critDamage * FireDotPercent / 100);

        /// <summary>
        /// A critical hit's side effect on a creature, or on an enemy player across a wargame (Pvp):
        /// the same effects, an Ice crit's freeze and a Sonic crit's knockback made with the
        /// player's own stun and knockback (PlayerCrowdControl), for PVP_EFFECT_DURATION_MODIFIER
        /// of the time. <paramref name="damage"/> is what the crit did, for the Fire burn.
        /// </summary>
        public static void OnCritical(MapChannel mapChannel, Actor target, Actor source, DamageType damageType, int damage)
        {
            if (target == null || source == null || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return;

            if (target is Manifestation player && (Pvp.IsSafe(player) || !target.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0))
                return;

            // And what a creature shouts at one (Battlecries), and a player says (PlayerVoices).
            if (target is Creature struck)
                Battlecries.Crit(mapChannel, struck);
            else if (target is Manifestation critted)
                PlayerVoices.Critical(critted);

            switch (damageType)
            {
                case DamageType.Ice when target is Creature frozen:
                    Stuns.Apply(mapChannel, frozen, source, Stuns.CritIceTypeId, Stuns.CritStunMs, damageType);
                    break;
                case DamageType.Ice when target is Manifestation frozenPlayer:
                    PlayerCrowdControl.Stun(mapChannel, frozenPlayer, source, Pvp.ScaleDuration(source, frozenPlayer, Stuns.CritStunMs));
                    break;
                case DamageType.Sonic when target is Creature knocked:
                    CrowdControl.Knockback(mapChannel, knocked, source, CrowdControl.DefaultKnockbackDistance, CrowdControl.CritSonicTypeId, damageType);
                    break;
                case DamageType.Sonic when target is Manifestation knockedPlayer:
                    PlayerCrowdControl.Knockback(mapChannel, knockedPlayer, source, CrowdControl.DefaultKnockbackDistance);
                    break;
                case DamageType.Virulent:
                    CrowdControl.Slow(mapChannel, target, source, CrowdControl.CritVirulentTypeId, CrowdControl.VirulentCrippleSlowPercent, CrowdControl.VirulentCrippleMs, "snareMod");
                    break;
                case DamageType.Fire:
                    Burn(mapChannel, target, source, damage);
                    break;
                case DamageType.EMP:
                    SuppressArmor(mapChannel, target, source);
                    break;
                case DamageType.Laser:
                    WeakenRanged(mapChannel, target, source);
                    break;
                case DamageType.Electrical:
                    Arc(mapChannel, target, source, damage);
                    break;
            }
        }

        private static GameEffect NewDebuff(MapChannel mapChannel, Actor source, int typeId, int durationMs)
        {
            return new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source.EntityId,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? 1,
                IsBuff = false,
                ExpiresTick = Environment.TickCount64 + durationMs
            };
        }

        /// <summary>
        /// Fire: a burn of FireTick(damage) fire damage every interval, the first an interval in.
        /// Not scaled again - it is a share of damage already scaled - and not a crit itself. A
        /// second Fire crit replaces the burn with a fresh one.
        /// </summary>
        public static void Burn(MapChannel mapChannel, Actor target, Actor source, int damage)
        {
            var tick = FireTick(damage);
            var burn = NewDebuff(mapChannel, source, CritFireTypeId, FireDotMs);

            burn.TickDamageMin = tick;
            burn.TickDamageMax = tick;
            burn.TickDamageType = DamageType.Fire;
            burn.TickScaleType = 0;
            burn.TickIntervalMs = FireDotIntervalMs;
            burn.NextTickTick = Environment.TickCount64 + FireDotIntervalMs;
            burn.Tooltip["dmgAmt"] = tick;
            burn.Tooltip["interval"] = FireDotIntervalMs / 1000;

            GameEffectManager.Instance.Attach(mapChannel, target, burn);
        }

        /// <summary>EMP: for EmpSuppressMs every hit on the creature ignores its armour.</summary>
        public static void SuppressArmor(MapChannel mapChannel, Actor target, Actor source)
        {
            var suppression = NewDebuff(mapChannel, source, CritEmpTypeId, EmpSuppressMs);

            suppression.SuppressesArmor = true;

            GameEffectManager.Instance.Attach(mapChannel, target, suppression);
        }

        /// <summary>
        /// Electric: arcs from the creature to the nearest ElectricArcTargets other hostiles
        /// within ElectricArcRadius, each taking ArcDamage(damage) as electrical damage (resisted,
        /// not a crit, a kill for the player). The effect goes on, then one tick draws the arcs
        /// from its FX and an AnnounceDamage floats the numbers.
        ///
        /// The FX is only there once the effect has been announced. Announced by its attach, the
        /// arcs go at once. Named in the hit that set it off (HitEffects), it is announced when
        /// the client plays that hit - up to the hit's strike time after it arrives - so the arcs,
        /// and their damage, wait that long and <see cref="ArcMarginMs"/> more, as the effect's
        /// tick; the effect stays on for ElectricEffectMs after them.
        /// </summary>
        public static void Arc(MapChannel mapChannel, Actor target, Actor source, int damage)
        {
            if (!(source is Manifestation))
                return;

            var strikeMs = HitEffects.StrikeMs(target);
            var arc = NewDebuff(mapChannel, source, CritElectricTypeId, ElectricEffectMs);

            GameEffectManager.Instance.Attach(mapChannel, target, arc);

            // Turned away (immune), or announced by its attach: the arcs go at once, as they did.
            if (!target.ActiveEffects.ContainsKey(arc.EffectId) || arc.AnnounceOnAttach)
            {
                DrawArcs(mapChannel, target, arc, damage);
                return;
            }

            var waitMs = strikeMs + ArcMarginMs;
            var now = Environment.TickCount64;

            arc.TickIntervalMs = waitMs;
            arc.NextTickTick = now + waitMs;
            arc.ExpiresTick = now + waitMs + ElectricEffectMs;
            arc.OnTick = (map, holder, effect) =>
            {
                // Once: the arcs are the effect's only tick.
                effect.TickIntervalMs = 0;
                effect.OnTick = null;
                DrawArcs(map, holder, effect, damage);
            };
        }

        /// <summary>How long after the hit is played, at most, an Electric crit's arcs wait for the announce they are drawn from.</summary>
        public const int ArcMarginMs = 250;

        /// <summary>
        /// The arcs of an Electric crit on <paramref name="target"/>: chosen around it as they go,
        /// drawn from the effect's FX by its tick, and floated by its AnnounceDamage.
        /// </summary>
        private static void DrawArcs(MapChannel mapChannel, Actor target, GameEffect arc, int damage)
        {
            if (!(arc.Source is Manifestation player) || mapChannel == null || target == null
                || player.MapContextId != mapChannel.MapInfo.MapContextId)
                return;

            var arcTo = AbilityManager.VictimsWithin(mapChannel, player, target.Position, ElectricArcRadius)
                .Where(c => c != target)
                .OrderBy(c => System.Numerics.Vector3.DistanceSquared(c.Position, target.Position))
                .Take(ElectricArcTargets)
                .ToList();

            if (arcTo.Count == 0)
                return;

            var draw = new GameEffectTickPacket(arc.EffectId, GameEffectTickPacket.TickKind.EntityIds);
            var announce = new GameEffectAnnounceDamagePacket(arc.EffectId);
            var perArc = ArcDamage(damage);

            foreach (var other in arcTo)
                draw.Entries.Add(new TickEntry { EntityId = other.EntityId });

            CellManager.Instance.CellCallMethod(mapChannel, target, draw);

            foreach (var other in arcTo)
            {
                var amount = GameEffectManager.ApplyResist(other, perArc, out var resisted, DamageType.Electrical);
                var taken = ActorManager.Instance.Damage(mapChannel, other, amount, player, out var outcome, DamageType.Electrical);

                announce.Hits.Add(new TickEntry
                {
                    EntityId = other.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = DamageType.Electrical,
                    DeathBlow = taken > 0 && other.Attributes[Attributes.Health].Current <= 0
                });
            }

            CellManager.Instance.CellCallMethod(mapChannel, target, announce);
        }

        /// <summary>Laser: the creature's ranged attacks do LaserRangedDamagePercent less for LaserMs.</summary>
        public static void WeakenRanged(MapChannel mapChannel, Actor target, Actor source)
        {
            var weakened = NewDebuff(mapChannel, source, CritLightTypeId, LaserMs);

            weakened.RangedDamagePercent = -LaserRangedDamagePercent;
            weakened.Tooltip["dmgMod"] = -LaserRangedDamagePercent;

            GameEffectManager.Instance.Attach(mapChannel, target, weakened);
        }
    }
}
