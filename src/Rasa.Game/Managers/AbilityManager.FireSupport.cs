using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Fire Support (abilities.firesupport), by what each pump's data carries (client
    /// actions/abilities/firesupport.py):
    ///
    ///  - Air strike (P1, P3: INTERVAL_MS and no EFFECT_DURATION_MS), at a location: a beacon
    ///    (entity class 26582, Ability_Carpet_Bomb_Beacon, which the client preloads) goes down
    ///    where the player aimed, carrying FIRE_SUPPORT_AIR_STRIKE_EFFECT 393, a BombEffect.
    ///    DELAY_TIME_MS (2.5 s) later every hostile within EFFECT_RADIUS (15 m) takes
    ///    DAMAGE_AMOUNT (270-540, scaled), sent as the effect's DoExplosion.
    ///  - Ion strike (P2, P4: EFFECT_DURATION_MS and no INTERVAL_MS), on an enemy - the client
    ///    makes those two pumps TARGET_NON_FRIENDLY: FIRE_SUPPORT_ION_STRIKE_EFFECT 395 on it;
    ///    DELAY_TIME_MS (5 s) later everything hostile within EFFECT_RADIUS (2 / 4 m) of it takes
    ///    DAMAGE_AMOUNT (150-180, scaled) and is stunned for EFFECT_DURATION_MS (4 / 6 s).
    ///  - Napalm (P5: both), at a location: the beacon carries FIRE_SUPPORT_NAPALM_BOMB_EFFECT
    ///    396 until DELAY_TIME_MS (2.5 s), then FIRE_SUPPORT_NAPALM_POOL_EFFECT 394, a
    ///    DamageOverTime, for EFFECT_DURATION_MS (10 s), every INTERVAL_MS (0.5 s) dealing
    ///    DAMAGE_AMOUNT (42-60, scaled) to every hostile within EFFECT_RADIUS (10 m).
    ///
    /// The data has no damage types: physical for the strikes, fire for the napalm. Strikes do
    /// not crit. The beacon is a dynamic object, not an actor, so its effects are sent to the
    /// clients directly and timed here (FireSupportWorker) rather than by GameEffectManager.
    /// </summary>
    public partial class AbilityManager
    {
        private const int AirStrikeTypeId = 393;        // FIRE_SUPPORT_AIR_STRIKE_EFFECT
        private const int IonStrikeTypeId = 395;        // FIRE_SUPPORT_ION_STRIKE_EFFECT
        private const int NapalmPoolTypeId = 394;       // FIRE_SUPPORT_NAPALM_POOL_EFFECT
        private const int NapalmBombTypeId = 396;       // FIRE_SUPPORT_NAPALM_BOMB_EFFECT

        /// <summary>Ability_Carpet_Bomb_Beacon, the marker the strikes land on.</summary>
        private const EntityClasses BeaconClass = (EntityClasses)26582;

        /// <summary>How long the beacon stays after its last blast, so the explosion FX can play on it.</summary>
        private const int BeaconLingerMs = 2000;

        private sealed class Strike
        {
            public MapChannel MapChannel;
            public Manifestation Player;
            public DynamicObject Beacon;
            public int EffectId;
            public bool Napalm;
            public float Radius;
            public int DamageMin, DamageMax, ScaleType;
            public long ExplodeAt;
            public long PoolUntil;
            public long NextPoolTick;
            public int PoolIntervalMs;
            public int PoolDurationMs;
            public long RemoveAt;
            public bool Exploded;
            public int Level;
        }

        private static readonly List<Strike> Strikes = new List<Strike>();
        private static readonly object StrikesLock = new object();

        private void CallFireSupport(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action, AbilityRecoveryPacket recovery)
        {
            var delayMs = Math.Max(0, info.Get(AbilityProperty.DelayTimeMs, 2500));
            var radius = (float)info.Get(AbilityProperty.EffectRadius, 10);
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));
            var scaleType = info.Get(AbilityProperty.DamageScaleType);
            var hasInterval = info.Has(AbilityProperty.IntervalMs);
            var hasDuration = info.Has(AbilityProperty.EffectDurationMs);

            // P2 / P4: the ion strike on a target.
            if (hasDuration && !hasInterval)
            {
                var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

                if (target == null || !IsHostile(player, target))
                    return;

                var stunMs = info.Get(AbilityProperty.EffectDurationMs);
                var ion = NewEffect(mapChannel, player, info, IonStrikeTypeId, null);

                ion.IsBuff = false;
                ion.AnnounceOnAttach = true;
                ion.TickRadius = radius;
                ion.TickIntervalMs = Math.Max(500, delayMs);
                ion.NextTickTick = Environment.TickCount64 + ion.TickIntervalMs;
                ion.ExpiresTick = ion.NextTickTick + 2000;
                ion.OnTick = (m, holder, effect) => IonBlast(m, holder, effect, min, max, scaleType, stunMs);

                GameEffectManager.Instance.Attach(mapChannel, target, ion);
                ManifestationManager.Instance.EnterCombat(client);
                return;
            }

            // P1 / P3 / P5: a beacon where the player aimed.
            var centre = action.TargetLocation ?? (action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId)?.Position : null) ?? player.Position;

            var beacon = new DynamicObject
            {
                EntityClassId = BeaconClass,
                Position = NavMeshManager.SnapToGround(mapChannel, centre),
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, beacon);

            var now = Environment.TickCount64;
            var strike = new Strike
            {
                MapChannel = mapChannel,
                Player = player,
                Beacon = beacon,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                Napalm = hasInterval && hasDuration,
                Radius = radius,
                DamageMin = min,
                DamageMax = max,
                ScaleType = scaleType,
                ExplodeAt = now + delayMs,
                PoolIntervalMs = Math.Max(250, info.Get(AbilityProperty.IntervalMs, 500)),
                PoolDurationMs = info.Get(AbilityProperty.EffectDurationMs),
                Level = (int)info.Level
            };

            SendBeaconEffect(strike, strike.Napalm ? NapalmBombTypeId : AirStrikeTypeId, (int)info.Level, strike.Napalm ? DamageType.Fire : DamageType.Physical);

            lock (StrikesLock)
                Strikes.Add(strike);
        }

        private static void SendBeaconEffect(Strike strike, int typeId, int level, DamageType damageType)
        {
            CellManager.Instance.CellCallMethod(strike.Beacon, new GameEffectAttachedPacket
            {
                EffectTypeId = typeId,
                EffectId = strike.EffectId,
                EffectLevel = (uint)Math.Max(1, level),
                SourceId = strike.Player.EntityId,
                Announced = true,
                Duration = null,
                DamageType = (int)damageType,
                AttrId = 1,
                IsActive = true,
                IsBuff = false,
                IsDebuff = true,
                IsNegativeEffect = true,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            });
        }

        /// <summary>The ion strike's blast around the effect's holder: damage, then the stun on whoever still stands.</summary>
        private void IonBlast(MapChannel mapChannel, Actor holder, GameEffect ion, int min, int max, int scaleType, int stunMs)
        {
            if (!(ion.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId || !(holder is Creature target))
            {
                GameEffectManager.Instance.DettachEffect(mapChannel, holder, ion);
                return;
            }

            var victims = HostilesWithin(mapChannel, player, target.Position, ion.TickRadius);

            if (!victims.Contains(target) && IsHostile(player, target))
                victims.Insert(0, target);

            var blast = new GameEffectAnnounceDamagePacket(ion.EffectId, "DoExplosion");

            foreach (var victim in victims)
            {
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(ion.SourceLevel, BombRandom.Next(min, max + 1), scaleType));
                blast.Hits.Add(DealDamage(mapChannel, player, victim, rolled, DamageType.Physical));
            }

            CellManager.Instance.CellCallMethod(mapChannel, target, blast);
            GameEffectManager.Instance.DettachEffect(mapChannel, target, ion);

            foreach (var victim in victims)
                if (victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    Stuns.Apply(mapChannel, victim, player, Stuns.StunTypeId, stunMs, DamageType.Physical);
        }

        /// <summary>Runs the beacon strikes on this map: explosions, napalm ticks, and taking the beacons away.</summary>
        internal void FireSupportWorker(MapChannel mapChannel)
        {
            List<Strike> mine;

            lock (StrikesLock)
                mine = Strikes.Where(s => s.MapChannel == mapChannel).ToList();

            if (mine.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var strike in mine)
            {
                try
                {
                    StepStrike(strike, now);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Fire Support strike on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                    strike.RemoveAt = now;
                }

                if (strike.RemoveAt != 0 && now >= strike.RemoveAt)
                {
                    RemoveStrike(strike);

                    lock (StrikesLock)
                        Strikes.Remove(strike);
                }
            }
        }

        private void StepStrike(Strike strike, long now)
        {
            var mapChannel = strike.MapChannel;
            var player = strike.Player;

            // The caller has left: the strike goes with them.
            if (player == null || player.MapContextId != mapChannel.MapInfo.MapContextId)
            {
                strike.RemoveAt = now;
                return;
            }

            if (strike.RemoveAt != 0)
                return;

            if (!strike.Exploded)
            {
                if (now < strike.ExplodeAt)
                    return;

                strike.Exploded = true;

                if (!strike.Napalm)
                {
                    var blast = new GameEffectAnnounceDamagePacket(strike.EffectId, "DoExplosion");

                    foreach (var victim in HostilesWithin(mapChannel, player, strike.Beacon.Position, strike.Radius))
                        blast.Hits.Add(StrikeHit(strike, victim, DamageType.Physical));

                    CellManager.Instance.CellCallMethod(strike.Beacon, blast);
                    strike.RemoveAt = now + BeaconLingerMs;
                    return;
                }

                // The napalm bomb lands: its effect gives way to the burning pool.
                CellManager.Instance.CellCallMethod(strike.Beacon, new GameEffectDetachedPacket { EffectId = strike.EffectId });
                strike.EffectId = GameEffectManager.Instance.NextEffectId(mapChannel);
                SendBeaconEffect(strike, NapalmPoolTypeId, strike.Level, DamageType.Fire);

                strike.PoolUntil = now + strike.PoolDurationMs;
                strike.NextPoolTick = now + strike.PoolIntervalMs;
                return;
            }

            if (now >= strike.PoolUntil)
            {
                strike.RemoveAt = now;
                return;
            }

            if (now < strike.NextPoolTick)
                return;

            strike.NextPoolTick += strike.PoolIntervalMs;

            if (strike.NextPoolTick <= now)
                strike.NextPoolTick = now + strike.PoolIntervalMs;

            var victims = HostilesWithin(mapChannel, player, strike.Beacon.Position, strike.Radius);

            if (victims.Count == 0)
                return;

            var tick = new GameEffectTickPacket(strike.EffectId, GameEffectTickPacket.TickKind.Damage);

            foreach (var victim in victims)
                tick.Entries.Add(StrikeHit(strike, victim, DamageType.Fire));

            CellManager.Instance.CellCallMethod(strike.Beacon, tick);
        }

        private TickEntry StrikeHit(Strike strike, Creature victim, DamageType damageType)
        {
            var rolled = GameEffectManager.ApplyDamageDealt(strike.Player, Scale(strike.Player.Level, BombRandom.Next(strike.DamageMin, strike.DamageMax + 1), strike.ScaleType));

            return DealDamage(strike.MapChannel, strike.Player, victim, rolled, damageType);
        }

        private static void RemoveStrike(Strike strike)
        {
            CellManager.Instance.CellCallMethod(strike.Beacon, new GameEffectDetachedPacket { EffectId = strike.EffectId });
            CellManager.Instance.RemoveFromWorld(strike.MapChannel, strike.Beacon);
        }
    }
}
