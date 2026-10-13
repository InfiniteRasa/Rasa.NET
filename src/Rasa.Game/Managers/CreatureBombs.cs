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
    /// The creature attacks that explode, from their client classes:
    ///
    ///  - Death blast (WardenBotDeathAbility, CR_WARDEN_BOT_DEATH 480): when the Warden dies, a
    ///    recovery of the action listing every player within RADIUS_AROUND_SOURCE with its damage,
    ///    as the bare rawInfo the class's DoAbility reads - the shape of a crab mine's
    ///    CR_CRAB_MINE_DEATH, which the class is a copy of.
    ///  - Death bomb (HowlerDeathAbility 514, PredatorDeathAbility 407): a BombEffect on the dying
    ///    creature - HOWLER_DEATH, PREDATOR_DEATH_EXPLOSION - that goes off DELAY_TIME_MS later
    ///    (the Howler at once, the Predator 3 s on): CallGameEffectMethod DoExplosion(damageData),
    ///    which plays the blast where the creature lies and floats every (entityId, rawInfo) in it.
    ///    The Predator's class names the effect as its targetGameEffect on TARGET_SELF, so a
    ///    recovery listing the Predator itself announces it; the Howler's names none, so its
    ///    effect is announced as it attaches. HowlerDeathEffect has removeTarget: the client takes
    ///    the Howler away when it goes off - there is no corpse to loot.
    ///  - Self-destruct (FithikSelfDestructAbility 180): a Fithik brought down to
    ///    SelfDestructHealthPercent of its health stops, winds up (4.3 s, the client's own
    ///    animation), and blows up: a recovery of the action with every player within
    ///    RADIUS_AROUND_SOURCE, bare rawInfo, and the Fithik dies of it, the kill its target's. One
    ///    killed during the windup does not go off. When to do it is ours: the data has the blast,
    ///    not the reason.
    ///  - Ground blast (LinkerGroundBlastAbility 264): a Linker's attack is a bomb on its target -
    ///    LINKER_GROUND_BLAST, the class's targetGameEffect, announced by the attack's recovery -
    ///    going off at once on everyone within EFFECT_RADIUS of that player
    ///    (CreatureEffectAttacks puts it on; the attack itself does no damage).
    ///  - Egg (StalkerOvulateAbility 441, StalkerEggDropAbility 442): a Stalker ovulates -
    ///    STALKER_EGG_CHARGE 304 on itself, the class's sourceGameEffect, for EFFECT_DURATION_MS
    ///    (20 s), every EFFECT_INTERVAL_MS (2 s) burning every player within EFFECT_RADIUS (20 m)
    ///    for the ovulate row's damage, the tick StalkerEggChargeEffect.OnTick floats - and when
    ///    the charge is done it drops the egg: its windup (3.7 s, standing still), then
    ///    STALKER_EGG_DROP_EXPLOSION 305 on itself going off DELAY_TIME_MS later on every player
    ///    within the drop's EFFECT_RADIUS (60 m) for the egg row's damage, EMP, knocking them
    ///    KNOCKBACK_DISTANCE (10 m) back. One killed before the egg drops does not drop it. When
    ///    to ovulate is ours: in a fight, with a player in reach, not already charging.
    ///  - Missile (PredatorMissileAbility 426): the same on the player a Predator's missile hits -
    ///    PREDATOR_MISSILE_EXPLOSION, a BombEffect whose removeTarget is off, the class's
    ///    targetGameEffect - going off at once within EFFECT_RADIUS (10 m).
    ///  - Necromite (NecromiteSelfDestructAbility 489, "Causes a Necromite to self-destruct and
    ///    damage nearby hostiles"): the same on the player a Necromite reaches -
    ///    NECROMITE_SELF_DESTRUCT 382, "AoE dmg when a necromite blows up on a player", the
    ///    class's targetGameEffect - going off DELAY_TIME_MS (0.5 s) on within EFFECT_RADIUS
    ///    (5 m). The Necromite is spent on it (Spend): it dies as its bomb goes on, the kill its
    ///    target's, as a Fithik's self-destruct is. A Necromite blows up on whatever it is
    ///    fighting - a player, or a creature (a Forean, an AFS turret) - and its blast takes
    ///    everything around that it may fight, players and creatures (HitsCreatures).
    ///  - Corpse (NecromiteCorpseExplosionAbility 490, "Causes a Necromite to explode on a
    ///    corpse", canTargetDead): a Necromite in a fight with a creature's body within its reach
    ///    (the argument's range, 5 m) and a player it may fight within EFFECT_RADIUS (10 m) of
    ///    that body winds up at it (0.8 s), and NECROMITE_CORPSE_EXPLOSION 380 - the class's
    ///    targetGameEffect, "AoE dmg when a necromite blows up a corpse" - goes on the body,
    ///    going off DELAY seconds (2) later on every player within EFFECT_RADIUS of it. The body
    ///    is claimed from the moment the Necromite winds up at it (IsCorpseClaimed): no revive,
    ///    no Reanimation, Cadaver Immolation or Hortimonculus, no second Necromite. The client's
    ///    BombEffect takes the body away when it goes off (removeTarget), and the server gives it
    ///    up for despawn then, as Cadaver Immolation's. As there, the effect is sent to the
    ///    clients directly: the effect worker clears everything but a death bomb off a dead actor. A body
    ///    destroyed by a finishing move, a scripted object or one the client has taken away (a
    ///    Howler's) is not a body to use. The Necromite is not spent on a corpse.
    ///
    /// The damage is the creature_action row's, resisted as the argument's DAMAGE_TYPE (physical
    /// when it gives none), and only players take it. Death actions sit on the creature's row with
    /// a range of 0, which the fighting loop never uses; they are found on the creature's actions
    /// when it dies.
    /// </summary>
    public static class CreatureBombs
    {
        public enum Kind { None, DeathBlast, DeathBomb, SelfDestruct, GroundBlast }

        public const ActionId WardenBotDeath = (ActionId)480;
        public const ActionId HowlerDeath = (ActionId)514;
        public const ActionId PredatorDeath = (ActionId)407;
        public const ActionId FithikSelfDestruct = (ActionId)180;
        public const ActionId LinkerGroundBlast = (ActionId)264;
        public const ActionId PredatorMissile = (ActionId)426;
        public const ActionId StalkerOvulate = (ActionId)441;
        public const ActionId StalkerEggDrop = (ActionId)442;
        public const ActionId NecromiteSelfDestruct = (ActionId)489;
        public const ActionId NecromiteCorpseExplosion = (ActionId)490;

        public const int HowlerDeathTypeId = 461;           // HOWLER_DEATH

        /// <summary>
        /// The level HOWLER_DEATH is attached at. BombEffect.Recv_DoExplosion plays the blast from
        /// gameeffectdata.specialFX[(typeId, level)], and the Howler's has one entry, at 13.
        /// </summary>
        public const int HowlerDeathFxLevel = 13;
        public const int PredatorDeathTypeId = 286;         // PREDATOR_DEATH_EXPLOSION
        public const int LinkerGroundBlastTypeId = 298;     // LINKER_GROUND_BLAST
        public const int PredatorMissileTypeId = 284;       // PREDATOR_MISSILE_EXPLOSION
        public const int StalkerEggChargeTypeId = 304;      // STALKER_EGG_CHARGE
        public const int StalkerEggDropTypeId = 305;        // STALKER_EGG_DROP_EXPLOSION
        public const int NecromiteSelfDestructTypeId = 382; // NECROMITE_SELF_DESTRUCT
        public const int NecromiteCorpseExplosionTypeId = 380; // NECROMITE_CORPSE_EXPLOSION

        /// <summary>How long a Necromite's claim on a body lasts past its windup, if the windup never lands (the Necromite killed).</summary>
        public const long CorpseClaimSlackMs = 2000;

        /// <summary>Ours: the share of its health at which a Fithik starts its self-destruct.</summary>
        public const int SelfDestructHealthPercent = 20;

        /// <summary>The blast's radius when the argument gives none.</summary>
        public const float DefaultRadius = 10f;

        public static Kind KindOf(ActionId actionId)
        {
            switch (actionId)
            {
                case WardenBotDeath: return Kind.DeathBlast;
                case HowlerDeath:
                case PredatorDeath: return Kind.DeathBomb;
                case FithikSelfDestruct: return Kind.SelfDestruct;
                case LinkerGroundBlast:
                case PredatorMissile: return Kind.GroundBlast;
                default: return Kind.None;
            }
        }

        public static bool IsDeathAction(CreatureAction action) => action != null && (KindOf(action.ActionId) == Kind.DeathBlast || KindOf(action.ActionId) == Kind.DeathBomb);

        /// <summary>
        /// The bomb a Howler or a Predator puts on its own body as it dies (OnDeath). It goes off
        /// from the corpse, so the effect worker leaves it there until it does.
        /// </summary>
        public static bool IsDeathBomb(GameEffect effect) => effect != null && (effect.TypeId == HowlerDeathTypeId || effect.TypeId == PredatorDeathTypeId);

        public static bool IsSelfDestruct(CreatureAction action) => action != null && action.ActionId == FithikSelfDestruct;

        public static bool IsOvulate(CreatureAction action) => action != null && action.ActionId == StalkerOvulate;

        public static bool IsEggDrop(CreatureAction action) => action != null && action.ActionId == StalkerEggDrop;

        public static bool IsCorpseExplosion(CreatureAction action) => action != null && action.ActionId == NecromiteCorpseExplosion;

        /// <summary>Whether a Fithik at this health should start its self-destruct.</summary>
        public static bool ShouldSelfDestruct(int health, int maxHealth) => maxHealth > 0 && health > 0 && health * 100 <= maxHealth * SelfDestructHealthPercent;

        /// <summary>The blast's radius from the argument: RADIUS_AROUND_SOURCE, else EFFECT_RADIUS, else DefaultRadius.</summary>
        public static float RadiusOf(ActionLevelInfo info)
        {
            if (info == null)
                return DefaultRadius;

            if (info.Get(AbilityProperty.RadiusAroundSource) > 0)
                return info.Get(AbilityProperty.RadiusAroundSource);

            return info.Get(AbilityProperty.EffectRadius) > 0 ? info.Get(AbilityProperty.EffectRadius) : DefaultRadius;
        }

        private sealed class Pending
        {
            public MapChannel MapChannel;
            public Creature Source;
            public Actor Holder;
            public CreatureAction Action;
            public GameEffect Effect;          // null for a self-destruct or an egg's windup, which are recoveries
            public bool EggDrop;               // the windup is a Stalker's egg drop, not a self-destruct
            public bool HitsCreatures;         // the blast takes the creatures its source may fight, not only players (a Necromite's)
            public float Radius;
            public DamageType DamageType;
            public long GoesOffAt;
        }

        private static readonly List<Pending> Bombs = new List<Pending>();
        private static readonly object BombsLock = new object();
        private static readonly Random Random = new Random();

        /// <summary>Whether the creature is winding up its self-destruct, or a Stalker its egg drop: it does nothing else.</summary>
        public static bool IsSelfDestructing(Creature creature)
        {
            // A loop, not Any(): this is asked of every creature on every think, and Any with a
            // lambda that captures the creature allocates a closure and a delegate each time.
            lock (BombsLock)
            {
                foreach (var bomb in Bombs)
                    if (bomb.Source == creature && bomb.Effect == null)
                        return true;

                return false;
            }
        }

        private static ActionLevelInfo LevelOf(CreatureAction action)
        {
            if (AbilityManager.Instance == null || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return null;

            return info;
        }

        private static DamageType TypeOf(ActionLevelInfo info) => (DamageType)(info?.Get(AbilityProperty.DamageType, (int)DamageType.Physical) ?? (int)DamageType.Physical);

        /// <summary>A creature has just died: its death action, if it has one, goes off.</summary>
        public static void OnDeath(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature?.Actions == null)
                return;

            // A self-destruct cut short by a kill does not go off.
            lock (BombsLock)
                Bombs.RemoveAll(b => b.Source == creature && b.Effect == null);

            var action = creature.Actions.FirstOrDefault(IsDeathAction);

            if (action == null)
                return;

            var info = LevelOf(action);

            if (info == null)
                return;

            if (KindOf(action.ActionId) == Kind.DeathBlast)
            {
                Blast(mapChannel, creature, action, RadiusOf(info), TypeOf(info));
                return;
            }

            var predator = action.ActionId == PredatorDeath;
            var delayMs = Math.Max(0, info.Get(AbilityProperty.DelayTimeMs));
            var bomb = NewBomb(mapChannel, creature, creature, info, predator ? PredatorDeathTypeId : HowlerDeathTypeId, delayMs, announce: !predator);

            GameEffectManager.Instance.Attach(mapChannel, creature, bomb);

            // The Predator's class announces its bomb on the hits of the recovery; the Howler's
            // recovery is its death animation.
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            if (predator)
                recovery.Hits.Add(new AbilityHit { EntityId = creature.EntityId });

            CellManager.Instance.CellCallMethod(mapChannel, creature, recovery);

            Arm(mapChannel, creature, creature, action, bomb, RadiusOf(info), TypeOf(info), delayMs);
        }

        /// <summary>The bomb an attack that lands as one puts on the player it hits: a Predator's missile's, a Necromite's, else a Linker's ground blast.</summary>
        public static int GroundBlastTypeOf(ActionId actionId) =>
            actionId == PredatorMissile ? PredatorMissileTypeId
            : actionId == NecromiteSelfDestruct ? NecromiteSelfDestructTypeId
            : LinkerGroundBlastTypeId;

        /// <summary>How long a bomb on a player waits: a Necromite's its DELAY_TIME_MS, the rest none.</summary>
        public static long GroundBlastDelayOf(ActionId actionId, ActionLevelInfo info) =>
            actionId == NecromiteSelfDestruct ? Math.Max(0, info?.Get(AbilityProperty.DelayTimeMs) ?? 0) : 0;

        /// <summary>Whether a blast of this action takes creatures as well as players: a Necromite's does.</summary>
        public static bool BlastHitsCreatures(ActionId actionId) => actionId == NecromiteSelfDestruct;

        /// <summary>
        /// A Linker's ground blast, a Predator's missile or a Necromite has hit someone: the bomb
        /// on them, going off when its delay is up. A player, or - a Necromite's - a creature.
        /// </summary>
        public static GameEffect GroundBlast(MapChannel mapChannel, Creature linker, Actor player, CreatureAction action, ActionLevelInfo info)
        {
            if (mapChannel == null || linker == null || player == null || action == null || info == null)
                return null;

            var delayMs = GroundBlastDelayOf(action.ActionId, info);
            var bomb = NewBomb(mapChannel, linker, player, info, GroundBlastTypeOf(action.ActionId), delayMs, announce: false);

            bomb.IsBuff = false;
            GameEffectManager.Instance.Attach(mapChannel, player, bomb);

            if (!player.ActiveEffects.ContainsKey(bomb.EffectId))
                return null;    // turned away (Cure's guard)

            // The recovery that announces it goes out first; the blast follows on the next tick.
            Arm(mapChannel, linker, player, action, bomb, RadiusOf(info), TypeOf(info), delayMs, BlastHitsCreatures(action.ActionId));

            return bomb;
        }

        private sealed class CorpseBomb
        {
            public MapChannel MapChannel;
            public Creature Source;
            public Creature Corpse;
            public CreatureAction Action;
            public int EffectId;
            public float Radius;
            public DamageType DamageType;
            public long GoesOffAt;
        }

        private static readonly List<CorpseBomb> CorpseBombs = new List<CorpseBomb>();
        private static readonly Dictionary<Creature, long> CorpseClaims = new Dictionary<Creature, long>();

        /// <summary>Whether a Necromite has this body: winding up at it, or its bomb on it.</summary>
        public static bool IsCorpseClaimed(Creature corpse)
        {
            if (corpse == null)
                return false;

            lock (BombsLock)
                return CorpseBombs.Any(c => c.Corpse == corpse)
                    || CorpseClaims.TryGetValue(corpse, out var until) && Environment.TickCount64 < until;
        }

        /// <summary>Whether a Necromite may blow this body up: a creature's corpse, not a scripted object, not destroyed by a finishing move or taken away by its client, and nobody else's.</summary>
        public static bool IsBlastableCorpse(Creature necromite, Creature corpse)
        {
            if (corpse == null || corpse == necromite || corpse.State != CharacterState.Dead || corpse.IsScripted || corpse.CritKilled)
                return false;

            if (corpse.Actions != null && corpse.Actions.Any(a => KindOf(a.ActionId) == Kind.DeathBomb))
                return false;

            return !IsCorpseClaimed(corpse) && !AbilityManager.IsCorpseInUse(corpse);
        }

        /// <summary>The body nearest the Necromite within reach that has a player it may fight within radius of it; null for none.</summary>
        private static Creature CorpseFor(MapChannel mapChannel, Creature necromite, float reach, float radius)
        {
            var bodies = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, necromite.Cells))
                foreach (var creature in cell.CreatureList)
                    if (Vector3.DistanceSquared(creature.Position, necromite.Position) <= reach * reach && IsBlastableCorpse(necromite, creature))
                        bodies.Add(creature);

            return bodies.Distinct()
                .OrderBy(c => Vector3.DistanceSquared(c.Position, necromite.Position))
                .FirstOrDefault(c => Caught(mapChannel, necromite, c.Position, radius).Count > 0);
        }

        /// <summary>
        /// A Necromite in a fight: if there is a body in reach with a player near it, it winds up
        /// at it and, when the windup is done, its bomb goes on the body. Whether it did.
        /// </summary>
        public static bool StartCorpseExplosion(MapChannel mapChannel, Creature necromite, CreatureAction action)
        {
            if (mapChannel == null || necromite == null || !IsCorpseExplosion(action))
                return false;

            var info = LevelOf(action);

            if (info == null)
                return false;

            var reach = Math.Max(1f, info.MaxRange);
            var radius = RadiusOf(info);
            var corpse = CorpseFor(mapChannel, necromite, reach, radius);

            if (corpse == null)
                return false;

            var windupMs = CreatureWindups.WindupMsOf(action, info);

            lock (BombsLock)
                CorpseClaims[corpse] = Environment.TickCount64 + windupMs + CorpseClaimSlackMs;

            CellManager.Instance.CellCallMethod(mapChannel, necromite,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, corpse.EntityId));

            CreatureWindups.After(mapChannel, necromite, windupMs, () => PlantOnCorpse(mapChannel, necromite, corpse, action, info, radius), action);

            return true;
        }

        private static void PlantOnCorpse(MapChannel mapChannel, Creature necromite, Creature corpse, CreatureAction action, ActionLevelInfo info, float radius)
        {
            lock (BombsLock)
                CorpseClaims.Remove(corpse);

            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            // Cleared away, or taken by something else, during the windup: it performs at nothing.
            if (EntityManager.Instance.GetCreature(corpse.EntityId) != corpse || corpse.MapContextId != mapChannel.MapInfo.MapContextId
                || !IsBlastableCorpse(necromite, corpse))
            {
                CellManager.Instance.CellCallMethod(mapChannel, necromite, recovery);
                return;
            }

            var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);
            var type = TypeOf(info);

            CellManager.Instance.CellCallMethod(mapChannel, corpse, new GameEffectAttachedPacket
            {
                EffectTypeId = NecromiteCorpseExplosionTypeId,
                EffectId = effectId,
                EffectLevel = Math.Max(1u, info.Level),
                SourceId = necromite.EntityId,
                Announced = true,
                Duration = null,
                DamageType = (int)type,
                AttrId = 1,
                IsActive = true,
                IsBuff = false,
                IsDebuff = true,
                IsNegativeEffect = true,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            });

            recovery.Hits.Add(new AbilityHit { EntityId = corpse.EntityId });
            CellManager.Instance.CellCallMethod(mapChannel, necromite, recovery);

            lock (BombsLock)
                CorpseBombs.Add(new CorpseBomb
                {
                    MapChannel = mapChannel,
                    Source = necromite,
                    Corpse = corpse,
                    Action = action,
                    EffectId = effectId,
                    Radius = radius,
                    DamageType = type,
                    GoesOffAt = Environment.TickCount64 + Math.Max(0, info.Get(AbilityProperty.Delay, 2)) * 1000L
                });
        }

        /// <summary>A body's bomb goes off: everyone around it hit, DoExplosion on the body, and the body given up.</summary>
        private static void BlowCorpse(MapChannel mapChannel, CorpseBomb bomb)
        {
            var corpse = bomb.Corpse;

            if (EntityManager.Instance.GetCreature(corpse.EntityId) != corpse || corpse.MapContextId != mapChannel.MapInfo.MapContextId)
                return;

            var blast = new GameEffectAnnounceDamagePacket(bomb.EffectId, "DoExplosion");

            foreach (var victim in Caught(mapChannel, bomb.Source, corpse.Position, bomb.Radius))
            {
                var (amount, resisted, crit) = Roll(bomb.Source, victim, bomb.Action, bomb.DamageType);

                ActorManager.Instance.Damage(mapChannel, victim, amount, bomb.Source, out var outcome, bomb.DamageType);
                Reflection.Reflect(mapChannel, victim, bomb.Source, outcome.Delivered, bomb.DamageType);

                blast.Hits.Add(new TickEntry { EntityId = victim.EntityId, Amount = outcome.Delivered, Absorbed = outcome.Absorbed, WasImmune = outcome.Immune, Resisted = resisted, DamageType = bomb.DamageType, IsCritical = crit });
            }

            CellManager.Instance.CellCallMethod(mapChannel, corpse, blast);

            // The clients have taken the body away with the blast; the server gives it up too, on
            // the deletion path that tidies its loot dispenser.
            if (corpse.Controller != null)
                corpse.Controller.DeadTime = long.MaxValue / 2;
        }

        /// <summary>
        /// A Necromite has put its bomb on the one it reached: it is spent, and dies of it - the
        /// kill its target's, as a Fithik's self-destruct is. A summon, it leaves nothing to loot.
        /// </summary>
        public static void Spend(MapChannel mapChannel, Creature necromite, Actor target)
        {
            if (mapChannel == null || necromite == null || necromite.State == CharacterState.Dead || necromite.State == CharacterState.Dying
                || !necromite.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return;

            health.Current = 0;
            CellManager.Instance.CellCallMethod(mapChannel, necromite, new UpdateHealthPacket(health, necromite.EntityId));

            CreatureManager.Instance.HandleCreatureKill(mapChannel, necromite, target ?? necromite);
        }

        /// <summary>
        /// A Stalker ovulates, if it would do something: not already charging or dropping an egg,
        /// with a player in the charge's reach. The charge burns around it, and when it is done
        /// the egg drops (StartEggDrop). Whether it did.
        /// </summary>
        public static bool Ovulate(MapChannel mapChannel, Creature stalker, CreatureAction action)
        {
            if (mapChannel == null || stalker == null || !IsOvulate(action) || IsSelfDestructing(stalker) || IsCharging(stalker))
                return false;

            var info = LevelOf(action);

            if (info == null)
                return false;

            var radius = Math.Max(1, info.Get(AbilityProperty.EffectRadius, 20));

            if (Caught(mapChannel, stalker, stalker.Position, radius).Count == 0)
                return false;

            var interval = Math.Max(250, info.Get(AbilityProperty.EffectIntervalMs, 2000));
            var now = Environment.TickCount64;
            var egg = stalker.Actions.FirstOrDefault(IsEggDrop);

            var charge = new GameEffect
            {
                TypeId = StalkerEggChargeTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = info.Level,
                ActionId = info.ActionId,
                SourceId = stalker.EntityId,
                Source = stalker,
                SourceLevel = (int)stalker.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = false,       // the recovery announces it, the class's sourceGameEffect
                ExpiresTick = now + Math.Max(interval, info.Get(AbilityProperty.EffectDurationMs, 20000)) + 250,
                TickDamageMin = (int)action.MinDamage,
                TickDamageMax = (int)Math.Max(action.MinDamage, action.MaxDamage),
                TickDamageType = TypeOf(info),
                TickScaleType = 0,              // the row's numbers are already the creature's
                TickRadius = radius,
                TickRadiusAsTick = true,
                TickIntervalMs = interval,
                NextTickTick = now + interval
            };

            // Charged - run its course, not cut short by a death or a leash - the egg drops, if the
            // Stalker is still standing to drop it.
            if (egg != null)
                charge.OnExpired = (map, actor, effect) =>
                {
                    if (map != null && actor is Creature dropper && dropper.State != CharacterState.Dead && dropper.State != CharacterState.Dying
                        && dropper.Attributes[Attributes.Health].Current > 0)
                        StartEggDrop(map, dropper, egg);
                };

            GameEffectManager.Instance.Attach(mapChannel, stalker, charge);

            CellManager.Instance.CellCallMethod(mapChannel, stalker, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
            CellManager.Instance.CellCallMethod(mapChannel, stalker, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));

            return true;
        }

        /// <summary>Whether a Stalker is charging an egg.</summary>
        public static bool IsCharging(Creature creature) =>
            creature.ActiveEffects.Values.Any(e => e.TypeId == StalkerEggChargeTypeId && !e.IsExpired);

        /// <summary>A Stalker's egg is charged: it stops, winds the drop up, and drops it when that is done.</summary>
        public static void StartEggDrop(MapChannel mapChannel, Creature stalker, CreatureAction action)
        {
            if (mapChannel == null || stalker == null || action == null || IsSelfDestructing(stalker))
                return;

            var info = LevelOf(action);

            if (info == null)
                return;

            BehaviorManager.Instance.StopMoving(stalker);
            stalker.Controller.Path.Clear();

            CellManager.Instance.CellCallMethod(mapChannel, stalker, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

            lock (BombsLock)
                Bombs.Add(new Pending
                {
                    MapChannel = mapChannel,
                    Source = stalker,
                    Holder = stalker,
                    Action = action,
                    EggDrop = true,
                    Radius = RadiusOf(info),
                    DamageType = TypeOf(info),
                    GoesOffAt = Environment.TickCount64 + Math.Max(0, info.WindupMs)
                });
        }

        /// <summary>The egg drop's windup is up: the bomb on the Stalker, announced by the recovery, going off DELAY_TIME_MS later.</summary>
        private static void DropEgg(MapChannel mapChannel, Pending windup)
        {
            var stalker = windup.Source;

            if (stalker.State == CharacterState.Dead || stalker.State == CharacterState.Dying || stalker.MapContextId != mapChannel.MapInfo.MapContextId
                || !stalker.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return;

            var info = LevelOf(windup.Action);

            if (info == null)
                return;

            var delayMs = Math.Max(0, info.Get(AbilityProperty.DelayTimeMs));
            var bomb = NewBomb(mapChannel, stalker, stalker, info, StalkerEggDropTypeId, delayMs, announce: false);

            GameEffectManager.Instance.Attach(mapChannel, stalker, bomb);

            // TARGET_NONE with a sourceGameEffect: the recovery announces it on the Stalker.
            CellManager.Instance.CellCallMethod(mapChannel, stalker, new AbilityRecoveryPacket(windup.Action.ActionId, windup.Action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));

            Arm(mapChannel, stalker, stalker, windup.Action, bomb, windup.Radius, windup.DamageType, delayMs);
        }

        /// <summary>A Fithik starts its self-destruct: the windup to everyone who can see it, and the blast when it is up.</summary>
        public static void StartSelfDestruct(MapChannel mapChannel, Creature fithik, CreatureAction action)
        {
            if (mapChannel == null || fithik == null || action == null || IsSelfDestructing(fithik))
                return;

            var info = LevelOf(action);
            var windupMs = info?.WindupMs ?? 0;

            BehaviorManager.Instance.StopMoving(fithik);
            fithik.Controller.Path.Clear();

            CellManager.Instance.CellCallMethod(mapChannel, fithik, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

            Arm(mapChannel, fithik, fithik, action, null, RadiusOf(info), TypeOf(info), windupMs);
        }

        private static GameEffect NewBomb(MapChannel mapChannel, Creature source, Actor holder, ActionLevelInfo info, int typeId, long delayMs, bool announce)
        {
            return new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = typeId == HowlerDeathTypeId ? HowlerDeathFxLevel : info.Level,
                ActionId = info.ActionId,
                SourceId = source.EntityId,
                Source = source,
                SourceLevel = (int)source.Level,
                IsBuff = true,
                ExpiresTick = Environment.TickCount64 + delayMs + 5000,     // a backstop; the blast takes it off
                AnnounceOnAttach = announce
            };
        }

        private static void Arm(MapChannel mapChannel, Creature source, Actor holder, CreatureAction action, GameEffect effect, float radius, DamageType type, long delayMs, bool hitsCreatures = false)
        {
            lock (BombsLock)
                Bombs.Add(new Pending
                {
                    MapChannel = mapChannel,
                    Source = source,
                    Holder = holder,
                    Action = action,
                    Effect = effect,
                    Radius = radius,
                    DamageType = type,
                    HitsCreatures = hitsCreatures,
                    GoesOffAt = Environment.TickCount64 + delayMs
                });
        }

        /// <summary>The creatures a blast around a point reaches: alive, on the map, within radius, and ones the source may fight.</summary>
        public static List<Creature> CaughtCreatures(MapChannel mapChannel, Creature source, Vector3 centre, float radius)
        {
            var found = new List<Creature>();

            if (mapChannel == null || source == null)
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, source.Cells))
                foreach (var creature in cell.CreatureList)
                    if (creature != source && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying
                        && creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                        && Vector3.DistanceSquared(creature.Position, centre) <= radius * radius
                        && BehaviorManager.MayFight(source, creature.EntityId))
                        found.Add(creature);

            return found.Distinct().ToList();
        }

        /// <summary>Sets off the bombs on this map whose time has come. Run every map tick.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Pending> due;
            List<CorpseBomb> bodies;
            var now = Environment.TickCount64;

            lock (BombsLock)
            {
                due = Bombs.Where(b => b.MapChannel == mapChannel && now >= b.GoesOffAt).ToList();

                foreach (var bomb in due)
                    Bombs.Remove(bomb);

                bodies = CorpseBombs.Where(b => b.MapChannel == mapChannel && now >= b.GoesOffAt).ToList();

                foreach (var body in bodies)
                    CorpseBombs.Remove(body);

                foreach (var lapsed in CorpseClaims.Where(c => now >= c.Value).Select(c => c.Key).ToList())
                    CorpseClaims.Remove(lapsed);
            }

            foreach (var body in bodies)
                BlowCorpse(mapChannel, body);

            foreach (var bomb in due)
            {
                if (bomb.Effect == null && bomb.EggDrop)
                    DropEgg(mapChannel, bomb);
                else if (bomb.Effect == null)
                    SelfDestruct(mapChannel, bomb);
                else
                    Explode(mapChannel, bomb);
            }
        }

        /// <summary>The players a blast around a point reaches: alive, on the map, within radius, and ones the creature may fight.</summary>
        public static List<Manifestation> Caught(MapChannel mapChannel, Creature source, Vector3 centre, float radius)
        {
            return mapChannel.ClientList
                .Select(c => c?.Player)
                .Where(p => p != null && p.State != CharacterState.Dead && p.State != CharacterState.Dying
                    && p.MapContextId == mapChannel.MapInfo.MapContextId
                    && p.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                    && Vector3.DistanceSquared(p.Position, centre) <= radius * radius
                    && TargetCategories.MayFightPlayer(source.TargetCategory, p.CombatCategory))
                .ToList();
        }

        private static (int Amount, int Resisted, bool Crit) Roll(Creature source, Actor victim, CreatureAction action, DamageType type)
        {
            int rolled;

            lock (Random)
                rolled = Random.Next((int)action.MinDamage, (int)Math.Max(action.MinDamage, action.MaxDamage) + 1);

            var crit = CriticalHits.Resolve(source, victim, false, CriticalHits.AttackerChance(source, false), ref rolled);
            var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, type);

            return (amount, resisted, crit);
        }

        /// <summary>A BombEffect goes off: damage to everyone caught, then DoExplosion on the holder to show it, and the effect ends.</summary>
        private static void Explode(MapChannel mapChannel, Pending bomb)
        {
            var holder = bomb.Holder;

            if (holder == null || holder.MapContextId != mapChannel.MapInfo.MapContextId || !holder.ActiveEffects.ContainsKey(bomb.Effect.EffectId))
                return;

            var blast = new GameEffectAnnounceDamagePacket(bomb.Effect.EffectId, "DoExplosion");
            var knockback = LevelOf(bomb.Action)?.Get(AbilityProperty.KnockbackDistance) ?? 0;

            foreach (var victim in Caught(mapChannel, bomb.Source, holder.Position, bomb.Radius))
            {
                var (amount, resisted, crit) = Roll(bomb.Source, victim, bomb.Action, bomb.DamageType);

                ActorManager.Instance.Damage(mapChannel, victim, amount, bomb.Source, out var outcome, bomb.DamageType);
                Reflection.Reflect(mapChannel, victim, bomb.Source, outcome.Delivered, bomb.DamageType);

                blast.Hits.Add(new TickEntry { EntityId = victim.EntityId, Amount = outcome.Delivered, Absorbed = outcome.Absorbed, WasImmune = outcome.Immune, Resisted = resisted, DamageType = bomb.DamageType, IsCritical = crit });

                // KNOCKBACK_DISTANCE, where the blast has one (a Stalker's egg): away from where it went off.
                if (knockback > 0 && victim.State != CharacterState.Dead && victim.Attributes[Attributes.Health].Current > 0)
                    PlayerCrowdControl.Knockback(mapChannel, victim, holder, knockback);
            }

            // A Necromite's takes the creatures it may fight too.
            if (bomb.HitsCreatures)
                foreach (var victim in CaughtCreatures(mapChannel, bomb.Source, holder.Position, bomb.Radius))
                {
                    var (amount, resisted, crit) = Roll(bomb.Source, victim, bomb.Action, bomb.DamageType);
                    var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, bomb.Source, out var outcome, bomb.DamageType);

                    blast.Hits.Add(new TickEntry
                    {
                        EntityId = victim.EntityId,
                        Amount = outcome.Delivered,
                        Absorbed = outcome.Absorbed,
                        WasImmune = outcome.Immune,
                        Resisted = resisted,
                        DamageType = bomb.DamageType,
                        IsCritical = crit,
                        DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                    });
                }

            CellManager.Instance.CellCallMethod(mapChannel, holder, blast);
            GameEffectManager.Instance.DettachEffect(mapChannel, holder, bomb.Effect);
        }

        /// <summary>A death blast: the action's recovery on the dead creature, a hit on everyone around it.</summary>
        private static void Blast(MapChannel mapChannel, Creature source, CreatureAction action, float radius, DamageType type)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.RawInfo);

            foreach (var victim in Caught(mapChannel, source, source.Position, radius))
            {
                var (amount, resisted, crit) = Roll(source, victim, action, type);

                ActorManager.Instance.Damage(mapChannel, victim, amount, source, out var outcome, type);
                Reflection.Reflect(mapChannel, victim, source, outcome.Delivered, type);

                recovery.Hits.Add(new AbilityHit { EntityId = victim.EntityId, Amount = outcome.Delivered, Absorbed = outcome.Absorbed, WasImmune = outcome.Immune, Resisted = resisted, DamageType = type, IsCritical = crit });
            }

            CellManager.Instance.CellCallMethod(mapChannel, source, recovery);
        }

        /// <summary>The self-destruct's windup is up: the Fithik blows, and dies of it - the kill its target's.</summary>
        private static void SelfDestruct(MapChannel mapChannel, Pending bomb)
        {
            var fithik = bomb.Source;

            if (fithik.State == CharacterState.Dead || fithik.State == CharacterState.Dying || fithik.MapContextId != mapChannel.MapInfo.MapContextId
                || !fithik.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return;

            var killer = EntityManager.Instance.GetActor(fithik.Controller.ActionFighting.TargetEntityId)
                ?? fithik.Hate.Ranked().Select(h => EntityManager.Instance.GetActor(h.Key)).FirstOrDefault(a => a != null);

            Blast(mapChannel, fithik, bomb.Action, bomb.Radius, bomb.DamageType);

            health.Current = 0;
            CellManager.Instance.CellCallMethod(mapChannel, fithik, new UpdateHealthPacket(health, fithik.EntityId));

            if (killer != null)
                CreatureManager.Instance.HandleCreatureKill(mapChannel, fithik, killer);
        }
    }
}
