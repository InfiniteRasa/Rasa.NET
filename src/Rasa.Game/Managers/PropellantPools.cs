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
    /// The pool a propellant gun leaves on the ground: "Propellant Guns can also inflict an area
    /// damage effect for a moderate amount of time when fired at the ground for extended periods
    /// or on a semi-random basis. Any enemies who linger within the affected area will take
    /// damage over time equal to 1/7 of initial damage" (the Tabula Rasa wiki's Propellant Gun
    /// page, which is all that is left of the rule).
    ///
    /// The client's side is PROPELLANT_POOL_EFFECT 10000047, a DamageOverTime
    /// (actions/weapons/flamethrower.py PropellantPoolEffect): held by something at the place,
    /// drawn by its level - a damage type, with a pool for each of physical, fire, ice, virulent,
    /// EMP, laser, sonic and electric (gameeffectdata.specialFX, WEAPON_PROPELLANT_*_POOL) - and
    /// ticked with [(targetId, damage)], which it floats on each as damage from the shooter.
    /// What holds it here is a ParticleLocationProxy (1400), the invisible proxy the fireworks
    /// and the flare gun put their effects on (AbilityManager.LocationProxyClass); Fire
    /// Support's napalm, the other pool in the game, is on its beacon the same way.
    ///
    /// The numbers were weapon properties of the server's and are in nothing we have. What is
    /// taken from the wiki:
    ///  - a tick is a seventh of the pulse that left the pool: its damage as the shooter dealt
    ///    it, before any one target's range, crit or resistance (<see cref="DamageDivisor"/>);
    ///  - "on a semi-random basis": any pulse may leave one (<see cref="ChancePercent"/>);
    ///  - "for extended periods": a held trigger leaves one for certain on its
    ///    <see cref="SustainedPulses"/>th pulse since the last.
    /// And what is ours:
    ///  - "fired at the ground" is not read from where the shooter is looking: the pool lands
    ///    under one of the targets the pulse hit, or, with none, on the ground
    ///    <see cref="ReachShare"/> of the way along the gun's reach;
    ///  - four metres across the middle (<see cref="Radius"/>): the pools' own FX are the "4m"
    ///    ones (vfx_generic_fire_4m.pfx and its kin), where napalm's ten-metre pool is the "8m";
    ///  - seven ticks a second apart (<see cref="DurationMs"/>, <see cref="TickIntervalMs"/>):
    ///    an enemy who stands in it to the end takes the pulse once more;
    ///  - a shooter's pools do not overlap - a pulse that would put one on another feeds that
    ///    one instead - and there are at most <see cref="MaxPerShooter"/>, the oldest going.
    ///
    /// A tick is resisted by its own damage type and does not crit, as napalm's. It reaches
    /// what the shooter's fire reaches: creatures they may attack and enemy players across a
    /// wargame (AbilityManager.VictimsWithin), looked for in the cells around the shooter. The
    /// pools of a shooter who leaves the map go with them. PROPELLANT_PUMP_EFFECT 10000046 has
    /// no FX, no class of its own beyond a name and nothing said of it anywhere, and is not done.
    /// </summary>
    public static class PropellantPools
    {
        public const int PoolTypeId = 10000047;             // PROPELLANT_POOL_EFFECT

        /// <summary>"damage over time equal to 1/7 of initial damage".</summary>
        public const int DamageDivisor = 7;

        /// <summary>Ours: metres from the pool's middle that it burns.</summary>
        public const float Radius = 4f;

        /// <summary>Ours: how long a pool lasts and how often it ticks. Seven ticks.</summary>
        public const int DurationMs = 7000;
        public const int TickIntervalMs = 1000;

        /// <summary>Ours: the chance a pulse leaves a pool, and the pulse of a held trigger that leaves one for certain.</summary>
        public const int ChancePercent = 15;
        public const int SustainedPulses = 5;

        /// <summary>Ours: the most pools one shooter has burning.</summary>
        public const int MaxPerShooter = 3;

        /// <summary>Ours: how far along the gun's reach a pulse that hit nothing leaves its pool.</summary>
        public const float ReachShare = 0.6f;

        /// <summary>The proxy stays this long after the effect comes off, for its FX to fade (BaseGameEffect.fxFadeoutMs is 1000).</summary>
        public const int FadeMs = 2000;

        private sealed class Pool
        {
            public MapChannel MapChannel;
            public Manifestation Shooter;
            public DynamicObject Proxy;
            public int EffectId;
            public DamageType DamageType;
            public int TickDamage;
            public long NextTick;
            public long Until;
            public long RemoveAt;
        }

        private static readonly List<Pool> Pools = new List<Pool>();

        /// <summary>Pulses each shooter has fired since their last pool, or since they let go.</summary>
        private static readonly Dictionary<Manifestation, int> Sprays = new Dictionary<Manifestation, int>();
        private static readonly object PoolsLock = new object();

        /// <summary>The roll out of a hundred, and the pick of one of several targets; a test's to replace.</summary>
        internal static Func<int> RollPercent { get; set; } = () => Random.Shared.Next(100);
        internal static Func<int, int> Pick { get; set; } = count => Random.Shared.Next(count);

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Forgets every pool and count and puts the roll, the pick and the clock back; a test's.</summary>
        internal static void Reset()
        {
            lock (PoolsLock)
            {
                Pools.Clear();
                Sprays.Clear();
            }

            RollPercent = () => Random.Shared.Next(100);
            Pick = count => Random.Shared.Next(count);
            Now = () => Environment.TickCount64;
        }

        /// <summary>What a pool left by a pulse of this damage does a tick.</summary>
        public static int TickDamageOf(int pulseDamage) => pulseDamage <= 0 ? 0 : Math.Max(1, pulseDamage / DamageDivisor);

        /// <summary>The shooter's pools on the ground now: where, and what a tick of each does.</summary>
        public static List<(Vector3 Position, int TickDamage, DamageType DamageType)> Of(Manifestation shooter)
        {
            lock (PoolsLock)
                return Pools.Where(pool => pool.Shooter == shooter && pool.RemoveAt == 0)
                    .Select(pool => (pool.Proxy.Position, pool.TickDamage, pool.DamageType)).ToList();
        }

        /// <summary>
        /// A pulse of a propellant gun has been fired: it may leave a pool. damage is the pulse's
        /// before any target's own share of it, reach the gun's, and hit whoever it landed on.
        /// </summary>
        public static void OnPulse(MapChannel mapChannel, Manifestation shooter, int damage, DamageType damageType, float reach, IReadOnlyList<Actor> hit)
        {
            var tickDamage = TickDamageOf(damage);

            if (mapChannel == null || shooter == null || tickDamage <= 0)
                return;

            lock (PoolsLock)
            {
                Sprays.TryGetValue(shooter, out var pulses);
                pulses++;

                if (pulses < SustainedPulses && RollPercent() >= ChancePercent)
                {
                    Sprays[shooter] = pulses;
                    return;
                }

                Sprays[shooter] = 0;
            }

            var under = hit != null && hit.Count > 0 ? hit[Math.Clamp(Pick(hit.Count), 0, hit.Count - 1)] : null;
            var centre = NavMeshManager.SnapToGround(mapChannel,
                under?.Position ?? shooter.Position + AbilityManager.FacingOf(shooter) * (reach * ReachShare));
            var now = Now();

            Pool fed = null;
            Pool retired = null;

            lock (PoolsLock)
            {
                var burning = Pools.Where(pool => pool.Shooter == shooter && pool.MapChannel == mapChannel && pool.RemoveAt == 0).ToList();

                // On one of their own: that one burns on, as hot as the hotter of the two.
                fed = burning.Where(pool => pool.DamageType == damageType && Flat(pool.Proxy.Position, centre) < Radius * 2)
                    .OrderBy(pool => Flat(pool.Proxy.Position, centre)).FirstOrDefault();

                if (fed != null)
                {
                    fed.Until = now + DurationMs;
                    fed.TickDamage = Math.Max(fed.TickDamage, tickDamage);
                    return;
                }

                if (burning.Count >= MaxPerShooter)
                    retired = burning.OrderBy(pool => pool.Until).First();
            }

            if (retired != null)
                PutOut(retired, now);

            var proxy = new DynamicObject
            {
                EntityClassId = AbilityManager.LocationProxyClass,
                Position = centre,
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, proxy);

            var created = new Pool
            {
                MapChannel = mapChannel,
                Shooter = shooter,
                Proxy = proxy,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                DamageType = damageType,
                TickDamage = tickDamage,
                NextTick = now + TickIntervalMs,
                Until = now + DurationMs
            };

            CellManager.Instance.CellCallMethod(mapChannel, proxy, new GameEffectAttachedPacket
            {
                EffectTypeId = PoolTypeId,
                EffectId = created.EffectId,
                // The pool's FX: specialFX is keyed (typeId, level), the level a damage type.
                EffectLevel = (uint)damageType,
                SourceId = shooter.EntityId,
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

            lock (PoolsLock)
                Pools.Add(created);
        }

        /// <summary>The trigger is let go: "extended periods" are of one hold, and the next starts its count again.</summary>
        public static void Stopped(Manifestation shooter)
        {
            if (shooter == null)
                return;

            lock (PoolsLock)
                Sprays.Remove(shooter);
        }

        /// <summary>Runs the pools on this map: their ticks, and taking them away when they have burnt out.</summary>
        internal static void Worker(MapChannel mapChannel)
        {
            List<Pool> mine;

            lock (PoolsLock)
                mine = Pools.Where(pool => pool.MapChannel == mapChannel).ToList();

            if (mine.Count == 0)
                return;

            var now = Now();

            foreach (var pool in mine)
            {
                try
                {
                    Step(pool, now);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"A propellant pool on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                    PutOut(pool, now);
                    pool.RemoveAt = now;
                }

                if (pool.RemoveAt != 0 && now >= pool.RemoveAt)
                {
                    CellManager.Instance.RemoveFromWorld(mapChannel, pool.Proxy);

                    lock (PoolsLock)
                        Pools.Remove(pool);
                }
            }
        }

        private static void Step(Pool pool, long now)
        {
            if (pool.RemoveAt != 0)
                return;

            var mapChannel = pool.MapChannel;
            var shooter = pool.Shooter;

            // The shooter has left the map: nobody is left for the damage to be from.
            if (shooter.MapChannel != mapChannel || shooter.MapContextId != mapChannel.MapInfo.MapContextId)
            {
                PutOut(pool, now);
                pool.RemoveAt = now;
                Stopped(shooter);
                return;
            }

            // A tick that is due is dealt before the pool is put out, so the last one lands.
            if (now >= pool.NextTick && pool.NextTick <= pool.Until)
            {
                pool.NextTick += TickIntervalMs;

                if (pool.NextTick <= now)
                    pool.NextTick = now + TickIntervalMs;

                Burn(pool);
            }

            if (now >= pool.Until)
                PutOut(pool, now);
        }

        /// <summary>One tick: everyone of the shooter's enemies standing in the pool.</summary>
        private static void Burn(Pool pool)
        {
            var victims = AbilityManager.VictimsWithin(pool.MapChannel, pool.Shooter, pool.Proxy.Position, Radius);

            if (victims.Count == 0)
                return;

            var tick = new GameEffectTickPacket(pool.EffectId, GameEffectTickPacket.TickKind.Damage);

            foreach (var victim in victims)
            {
                var amount = GameEffectManager.ApplyResist(victim, pool.TickDamage, out var resisted, pool.DamageType);
                var taken = ActorManager.Instance.Damage(pool.MapChannel, victim, amount, pool.Shooter, out var outcome, pool.DamageType);

                tick.Entries.Add(new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = pool.DamageType,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                });
            }

            CellManager.Instance.CellCallMethod(pool.MapChannel, pool.Proxy, tick);
        }

        /// <summary>The effect comes off; the proxy follows once its FX has faded.</summary>
        private static void PutOut(Pool pool, long now)
        {
            if (pool.RemoveAt != 0)
                return;

            pool.RemoveAt = now + FadeMs;
            CellManager.Instance.CellCallMethod(pool.MapChannel, pool.Proxy, new GameEffectDetachedPacket { EffectId = pool.EffectId });
        }

        /// <summary>Metres between two points on the ground, their heights aside.</summary>
        private static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
    }
}
