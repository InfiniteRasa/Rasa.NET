using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The creature actions a creature performs on itself or its own side rather than at the
    /// player it is fighting:
    ///
    ///  - Rage (abilities.rage, CR_THRAX_RAGE 454): the player Soldier's Rage in a Thrax boss's
    ///    hands. RAGESOURCE 236 on the Thrax - the class's targetGameEffect, TARGET_SELF, so the
    ///    recovery listing the Thrax announces it - raising the damage it deals by
    ///    DAMAGE_PERCENT_MIN for DURATION, and as an aura every INTERVAL seconds RAGE 235 on the
    ///    Bane of its own side within RADIUS_AROUND_SOURCE, which RageSourceEffect.OnTick
    ///    announces on the ids each tick names. The argument's DAMAGE_PERCENT is 140 where the
    ///    player's is 30-50, read the same way: +140%.
    ///  - Scourge (abilities.scourge, CR_THRAX_SCOURGE 455): the Commando's Scourge.
    ///    SCOURGE_EFFECT 256 on the Thrax - the class's sourceGameEffect, announced by a recovery
    ///    with a hit - dealing the creature_action row's damage every second for DURATION to every
    ///    player within EFFECT_RADIUS, shown through the effect's AnnounceDamage as a player's is.
    ///  - Warcry (AttaWarcryAbility, CR_ATTA_HARVESTER_WARCRY 477): CALL_FOR_HELP_NUM_CREATURES
    ///    of its own side within RADIUS_AROUND_SOURCE (80 m, or as far as the cells around the
    ///    harvester reach, about 64 m) that are not already fighting join the harvester's fight,
    ///    nearest first. Once a fight (WarcryRearmMs): it is a call for help,
    ///    not a way to pull a whole field.
    ///  - Channel (LinkerChannelAbility, CR_LINKER_CHANNEL 410): the strategy guide - "One of the
    ///    Linker's more dangerous abilities is its ability to draw energy from other nearby
    ///    Linkers, doubling or tripling the power of their attacks!" A Linker winds up its
    ///    channel (5.7 s, the class's own animation and FX, TARGET_FRIENDLY) at another Linker in
    ///    the same fight within the argument's range; when it is done that Linker's attacks do
    ///    ChannelBoostPercent more for ChannelBoostMs - double with one Linker feeding it, triple
    ///    with two (MaxChannelers). The class shows nothing on the one fed (its DoAbility is
    ///    empty) and has no effect of its own, so the boost is the server's alone (ServerOnly).
    ///    How long it lasts is ours; the argument's DAMAGE_AMOUNT (10-20) is not used.
    ///  - Chaff (ChaffAction, CR_FOREAN_CHAFF 204): "Creates a cloud of flying metallic foil that
    ///    interferes with enemy targeting, making you and nearby allies harder to hit."
    ///    CHAFFSOURCE 90 on the Forean - the class's targetGameEffect, TARGET_SELF - for DURATION
    ///    (60 s), and as an aura every INTERVAL (5 s) CHAFF 91 on its own side within
    ///    RADIUS_AROUND_SOURCE (6 m), which ChaffSourceEffect.OnTick announces on the ids each
    ///    tick names. On all of them DEFENSIVE_TOHIT_MODIFIER (50) is the share of shots at them
    ///    that go wide (GameEffect.MissPercent, MissileManager.MissesForChaff): the server has no
    ///    to-hit roll of its own for the modifier to move, so it is read as a miss chance.
    ///
    /// Each is used only when it would do something - not while its effect is still on, not a
    /// warcry with nobody to hear it, not a channel with no Linker to feed - and otherwise the
    /// fighting loop goes on to the next action.
    /// </summary>
    public static class CreatureBuffs
    {
        public const ActionId ThraxRage = (ActionId)454;
        public const ActionId ThraxScourge = (ActionId)455;
        public const ActionId HarvesterWarcry = (ActionId)477;
        public const ActionId LinkerChannel = (ActionId)410;
        public const ActionId ForeanChaff = (ActionId)204;

        /// <summary>The guide's "doubling or tripling": what each Linker feeding another adds to its attacks, and how many may.</summary>
        public const int ChannelBoostPercent = 100;
        public const int MaxChannelers = 2;

        /// <summary>Ours: how long a channel's boost lasts.</summary>
        public const long ChannelBoostMs = 20000;

        private sealed class Channeling
        {
            public MapChannel MapChannel;
            public Creature Channeler;
            public Creature Fed;
            public CreatureAction Action;
            public long LandsAt;
        }

        private static readonly List<Channeling> Channels = new List<Channeling>();
        private static readonly Dictionary<Creature, Dictionary<Creature, long>> Links = new Dictionary<Creature, Dictionary<Creature, long>>();
        private static readonly object ChannelLock = new object();

        public const int RageTypeId = 235;          // RAGE
        public const int RageSourceTypeId = 236;    // RAGESOURCE
        public const int ScourgeTypeId = 256;       // SCOURGE_EFFECT
        public const int ChaffSourceTypeId = 90;    // CHAFFSOURCE
        public const int ChaffTypeId = 91;          // CHAFF

        /// <summary>Ours: how long before a creature's warcry can call again.</summary>
        public const long WarcryRearmMs = 60000;

        private static readonly Dictionary<ulong, long> WarcryAt = new Dictionary<ulong, long>();
        private static readonly object WarcryLock = new object();

        /// <summary>
        /// A creature leaving the world: its warcry clock goes with it, rather than staying in the
        /// table for good and being inherited by the next creature given its entity id.
        /// </summary>
        public static void Forget(Creature creature)
        {
            if (creature == null)
                return;

            lock (WarcryLock)
                WarcryAt.Remove(creature.EntityId);
        }

        public static bool Is(CreatureAction action) =>
            action != null && (action.ActionId == ThraxRage || action.ActionId == ThraxScourge || action.ActionId == HarvesterWarcry
                || action.ActionId == LinkerChannel || action.ActionId == ForeanChaff);

        /// <summary>Uses the action if it would do something now; whether it did.</summary>
        public static bool Perform(MapChannel mapChannel, Creature creature, CreatureAction action, Actor target)
        {
            if (mapChannel == null || creature == null || action == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return false;

            switch (action.ActionId)
            {
                case ThraxRage:
                    return Rage(mapChannel, creature, action, info);
                case ThraxScourge:
                    return Scourge(mapChannel, creature, action, info);
                case HarvesterWarcry:
                    return Warcry(mapChannel, creature, action, info, target);
                case LinkerChannel:
                    return Channel(mapChannel, creature, action, info);
                case ForeanChaff:
                    return Chaff(mapChannel, creature, action, info);
                default:
                    return false;
            }
        }

        private static bool Has(Actor actor, int typeId) => actor.ActiveEffects.Values.Any(e => e.TypeId == typeId && !e.IsExpired);

        private static GameEffect NewEffect(MapChannel mapChannel, Creature creature, ActionLevelInfo info, int typeId, long durationMs)
        {
            return new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = info.Level,
                ActionId = info.ActionId,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                ExpiresTick = Environment.TickCount64 + durationMs,
                AnnounceOnAttach = false
            };
        }

        /// <summary>
        /// The windup now, and when it is done (CreatureWindups.After) what the action does:
        /// resolve returns who it reached, which the recovery lists.
        /// </summary>
        private static void WindUp(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info, Func<IEnumerable<Actor>> resolve)
        {
            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, creature.EntityId));

            CreatureWindups.After(mapChannel, creature, CreatureWindups.WindupMsOf(action, info),
                () => Recover(mapChannel, creature, action, resolve()), action);
        }

        /// <summary>The recovery, which lists who the action reached.</summary>
        private static void Recover(MapChannel mapChannel, Creature creature, CreatureAction action, IEnumerable<Actor> hits)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            foreach (var hit in hits)
                recovery.Hits.Add(new AbilityHit { EntityId = hit.EntityId });

            CellManager.Instance.CellCallMethod(mapChannel, creature, recovery);
        }

        private static bool Rage(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info)
        {
            if (Has(creature, RageSourceTypeId))
                return false;

            WindUp(mapChannel, creature, action, info, () => { EnRage(mapChannel, creature, info); return new[] { creature }; });

            return true;
        }

        private static void EnRage(MapChannel mapChannel, Creature creature, ActionLevelInfo info)
        {
            var rage = NewEffect(mapChannel, creature, info, RageSourceTypeId, info.Get(AbilityProperty.Duration, 30) * 1000L);

            rage.DamageDealtPercent = info.Get(AbilityProperty.DamagePercentMin);
            rage.AllowDetach = true;
            rage.Tooltip["dmgMod"] = rage.DamageDealtPercent;
            rage.Tooltip["resistMod"] = 0;

            if (info.Get(AbilityProperty.RadiusAroundSource) > 0)
            {
                rage.AuraRadius = info.Get(AbilityProperty.RadiusAroundSource);
                rage.AuraChildTypeId = RageTypeId;
                rage.AuraTickAnnounces = true;
                rage.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000;
                rage.NextTickTick = Environment.TickCount64;
            }

            GameEffectManager.Instance.Attach(mapChannel, creature, rage);
        }

        private static bool Chaff(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info)
        {
            if (Has(creature, ChaffSourceTypeId))
                return false;

            WindUp(mapChannel, creature, action, info, () => { StartChaff(mapChannel, creature, info); return new[] { creature }; });

            return true;
        }

        /// <summary>The cloud: CHAFFSOURCE on the Forean, CHAFF on its side around it, and shots at any of them going wide.</summary>
        public static GameEffect StartChaff(MapChannel mapChannel, Creature creature, ActionLevelInfo info)
        {
            var chaff = NewEffect(mapChannel, creature, info, ChaffSourceTypeId, Math.Max(1, info.Get(AbilityProperty.Duration, 60)) * 1000L);

            chaff.MissPercent = Math.Max(0, Math.Min(100, info.Get(AbilityProperty.DefensiveTohitModifier)));
            chaff.AllowDetach = true;

            if (info.Get(AbilityProperty.RadiusAroundSource) > 0)
            {
                chaff.AuraRadius = info.Get(AbilityProperty.RadiusAroundSource);
                chaff.AuraChildTypeId = ChaffTypeId;
                chaff.AuraTickAnnounces = true;
                chaff.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000;
                chaff.NextTickTick = Environment.TickCount64;
            }

            if (mapChannel != null)
                GameEffectManager.Instance.Attach(mapChannel, creature, chaff);

            return chaff;
        }

        private static bool Scourge(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info)
        {
            if (Has(creature, ScourgeTypeId) || action.MaxDamage == 0)
                return false;

            WindUp(mapChannel, creature, action, info, () => { StartScourge(mapChannel, creature, action, info); return new[] { creature }; });

            return true;
        }

        private static void StartScourge(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info)
        {
            var scourge = NewEffect(mapChannel, creature, info, ScourgeTypeId, info.Get(AbilityProperty.Duration, 15) * 1000L);

            scourge.TickRadius = info.Get(AbilityProperty.EffectRadius, 6);
            scourge.TickDamageMin = (int)action.MinDamage;
            scourge.TickDamageMax = (int)Math.Max(action.MinDamage, action.MaxDamage);
            scourge.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            scourge.TickScaleType = 0;     // the row's numbers are already the creature's
            scourge.TickIntervalMs = 1000;
            scourge.NextTickTick = Environment.TickCount64 + 1000;

            GameEffectManager.Instance.Attach(mapChannel, creature, scourge);
        }

        private static bool Warcry(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info, Actor target)
        {
            if (target == null)
                return false;

            var now = Environment.TickCount64;

            lock (WarcryLock)
                if (WarcryAt.TryGetValue(creature.EntityId, out var at) && now - at < WarcryRearmMs)
                    return false;

            if (Hears(mapChannel, creature, info).Count == 0)
                return false;

            lock (WarcryLock)
                WarcryAt[creature.EntityId] = now;

            // Who hears it is who is about when the cry goes up, at the end of the windup.
            WindUp(mapChannel, creature, action, info, () =>
            {
                var heard = Hears(mapChannel, creature, info);

                foreach (var ally in heard)
                {
                    ally.Hate.Ensure(target.EntityId, 1);
                    BehaviorManager.Instance.SetActionFighting(ally, target.EntityId);
                }

                return heard;
            });

            return true;
        }

        /// <summary>The allies a warcry reaches: CALL_FOR_HELP_NUM_CREATURES of those within RADIUS_AROUND_SOURCE not already fighting, nearest first.</summary>
        private static List<Creature> Hears(MapChannel mapChannel, Creature creature, ActionLevelInfo info)
        {
            var count = Math.Max(1, info.Get(AbilityProperty.CallForHelpNumCreatures, 1));
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 20);

            return AlliesWithin(mapChannel, creature, creature.Position, radius)
                .Where(a => a.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting && !BehaviorManager.IsReturning(a))
                .OrderBy(a => Vector3.DistanceSquared(a.Position, creature.Position))
                .Take(count)
                .ToList();
        }

        /// <summary>How many Linkers are feeding this one now.</summary>
        public static int ChannelersOf(Creature fed)
        {
            var now = Environment.TickCount64;

            lock (ChannelLock)
                return Links.TryGetValue(fed, out var links) ? links.Count(l => l.Value > now) : 0;
        }

        /// <summary>What a Linker fed by this many does more: ChannelBoostPercent each, MaxChannelers at most.</summary>
        public static int ChannelBoostFor(int channelers) => Math.Max(0, Math.Min(MaxChannelers, channelers)) * ChannelBoostPercent;

        private static bool Channel(MapChannel mapChannel, Creature linker, CreatureAction action, ActionLevelInfo info)
        {
            var now = Environment.TickCount64;
            var range = Math.Max(1, info.MaxRange);

            bool Feeding(Creature fed)
            {
                lock (ChannelLock)
                    return Links.TryGetValue(fed, out var links) && links.TryGetValue(linker, out var until) && until > now
                        || Channels.Any(c => c.Channeler == linker || c.Fed == fed && c.Channeler == linker);
            }

            var fed = AlliesWithin(mapChannel, linker, linker.Position, range)
                .Where(a => a.EntityClass == linker.EntityClass
                    && a.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
                    && ChannelersOf(a) < MaxChannelers && !Feeding(a))
                .OrderBy(a => Vector3.DistanceSquared(a.Position, linker.Position))
                .FirstOrDefault();

            if (fed == null)
                return false;

            var windupMs = CreatureWindups.WindupMsOf(action, info);

            linker.Controller.WindupUntil = now + windupMs;
            linker.Controller.Path.Clear();
            BehaviorManager.Instance.StopMoving(linker);

            CellManager.Instance.CellCallMethod(mapChannel, linker,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, fed.EntityId));

            lock (ChannelLock)
                Channels.Add(new Channeling { MapChannel = mapChannel, Channeler = linker, Fed = fed, Action = action, LandsAt = now + windupMs });

            return true;
        }

        /// <summary>The channels whose windup is done feed their Linker; the boosts that have run out end. Run every map tick.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;
            List<Channeling> due;
            List<Creature> fedHere;

            lock (ChannelLock)
            {
                due = Channels.Where(c => c.MapChannel == mapChannel && now >= c.LandsAt).ToList();

                foreach (var channel in due)
                    Channels.Remove(channel);

                fedHere = Links.Keys.Where(k => k.MapContextId == mapChannel.MapInfo.MapContextId).ToList();
            }

            foreach (var channel in due)
            {
                var linker = channel.Channeler;
                var fed = channel.Fed;

                // Killed, stunned or knocked out of it, or nothing left to feed: it comes to nothing,
                // and a Linker still standing is told so (an interrupt; a dead one is done anyway).
                if (!Standing(linker) || Stuns.IsStunned(linker) || !Standing(fed))
                {
                    CreatureWindups.Interrupt(mapChannel, linker, channel.Action.ActionId, channel.Action.ActionArgId);
                    continue;
                }

                var recovery = new AbilityRecoveryPacket(channel.Action.ActionId, channel.Action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);
                recovery.Hits.Add(new AbilityHit { EntityId = fed.EntityId });
                CellManager.Instance.CellCallMethod(mapChannel, linker, recovery);

                lock (ChannelLock)
                {
                    if (!Links.TryGetValue(fed, out var links))
                        Links[fed] = links = new Dictionary<Creature, long>();

                    links[linker] = now + ChannelBoostMs;
                }

                if (!fedHere.Contains(fed))
                    fedHere.Add(fed);
            }

            foreach (var fed in fedHere)
                Refresh(mapChannel, fed, now);
        }

        private static bool Standing(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying
            && creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;

        /// <summary>The boost on a fed Linker brought up to date with who is feeding it: gone when nobody is.</summary>
        private static void Refresh(MapChannel mapChannel, Creature fed, long now)
        {
            int count;
            long until;

            lock (ChannelLock)
            {
                if (!Links.TryGetValue(fed, out var links))
                    return;

                foreach (var gone in links.Where(l => l.Value <= now || !Standing(l.Key)).Select(l => l.Key).ToList())
                    links.Remove(gone);

                count = links.Count;
                until = count > 0 ? links.Values.Max() : 0;

                if (count == 0 || !Standing(fed))
                    Links.Remove(fed);
            }

            var boost = fed.ActiveEffects.Values.FirstOrDefault(e => e.ServerOnly && e.ActionId == LinkerChannel);

            if (count == 0 || !Standing(fed))
            {
                if (boost != null)
                    GameEffectManager.Instance.DettachEffect(mapChannel, fed, boost);

                return;
            }

            if (boost == null)
            {
                boost = new GameEffect
                {
                    TypeId = 0,
                    EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                    EffectLevel = 1,
                    ActionId = LinkerChannel,
                    SourceId = fed.EntityId,
                    Source = fed,
                    SourceLevel = (int)fed.Level,
                    IsBuff = true,
                    ServerOnly = true
                };

                GameEffectManager.Instance.Attach(mapChannel, fed, boost);
            }

            boost.DamageDealtPercent = ChannelBoostFor(count);
            boost.ExpiresTick = until;
        }

        /// <summary>
        /// The creatures of the same side as this one within radius of a point: alive, on the map,
        /// not it. Looked for in the cells around the creature, as everything near an actor is.
        /// </summary>
        public static List<Creature> AlliesWithin(MapChannel mapChannel, Creature creature, Vector3 centre, float radius)
        {
            var all = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
                all.AddRange(cell.CreatureList);

            return all
                .Distinct()
                .Where(c => c != null && c != creature && c.MapContextId == mapChannel.MapInfo.MapContextId
                    && c.TargetCategory == creature.TargetCategory
                    && c.State != CharacterState.Dead && c.State != CharacterState.Dying
                    && c.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                    && Vector3.DistanceSquared(c.Position, centre) <= radius * radius)
                .ToList();
        }
    }
}
