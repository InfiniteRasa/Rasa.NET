using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    internal static partial class LandingZoneAssaults
    {
        /// <summary>One point's assault: its timing, its dropships and what each brings, and the AFS relief.</summary>
        private sealed class Plan
        {
            internal string Name;
            internal long FirstDelayMs, PeriodMs, WaveGapMs, LifetimeMs, ReliefDelayMs;
            internal int Waves;
            internal LandingZoneAssaultContent.Drop[] Drops;
            internal uint Boss;
            internal Func<int, int, int, List<SpawnPoolSlot>> Ship;
            internal Action<MapChannel, ControlPoints.Point, Assault> Reinforce;
        }

        /// <summary>The plan of a public point whose assaults are on, or null.</summary>
        private static Plan PlanFor(ControlPoints.Point point)
        {
            if (point == null) return null;
            var config = ControlPoints.Instance.Config;
            if (point.MapContextId == LandingZoneAssaultContent.MapId && point.Id == LandingZoneAssaultContent.PointId)
                return config.LandingZoneAssaultEnabled ? LandingZone(config) : null;
            if (!config.ControlPointAssaultEnabled) return null;
            var content = PlateauDynamicDropshipsContent.Assaults.FirstOrDefault(a => a.MapId == point.MapContextId && a.PointId == point.Id);
            return content == null ? null : FromContent(content);
        }

        private static Plan LandingZone(Config.ControlPointConfig config)
        {
            var period = Math.Max(1, config.LandingZoneAssaultPeriodSeconds) * 1000L;
            return new Plan
            {
                Name = "Landing Zone",
                FirstDelayMs = period,
                PeriodMs = period,
                WaveGapMs = Math.Max(1, config.LandingZoneAssaultWaveSeconds) * 1000L,
                LifetimeMs = config.LandingZoneAssaultLifetimeSeconds * 1000L,
                Waves = 3,
                Drops = LandingZoneAssaultContent.Drops,
                Boss = LandingZoneAssaultContent.Boss,
                Ship = (wave, ship, bossShip) =>
                {
                    var slots = new List<SpawnPoolSlot> {
                        new(wave == 1 ? LandingZoneAssaultContent.Trainee : LandingZoneAssaultContent.Private, 3, 5),
                        new(wave == 1 ? LandingZoneAssaultContent.Private : LandingZoneAssaultContent.Sergeant, 1, 1) };
                    if (wave == 1 || Random.Shared.Next(2) == 0) slots.Add(new(LandingZoneAssaultContent.Hunter, 1, 1));
                    else slots.Add(new(LandingZoneAssaultContent.Linker, 2, 2));
                    if (wave == 3 && ship == bossShip) slots.Add(new(LandingZoneAssaultContent.Boss, 1, 1));
                    return slots;
                },
                Reinforce = (map, _, state) => Reinforce(map, state)
            };
        }

        private static Plan FromContent(PlateauDynamicDropshipsContent.Assault content) => new()
        {
            FirstDelayMs = content.FirstDelaySeconds * 1000L,
            PeriodMs = content.PeriodSeconds * 1000L,
            WaveGapMs = content.WaveSeconds * 1000L,
            LifetimeMs = content.LifetimeSeconds * 1000L,
            ReliefDelayMs = content.ReliefSeconds * 1000L,
            Waves = content.Waves.Length,
            Drops = content.Drops,
            Boss = content.Boss,
            Ship = (wave, ship, bossShip) =>
            {
                var troops = content.Waves[wave - 1];
                var slots = troops[ship % troops.Length].Select(t => new SpawnPoolSlot(t.Creature, (short)t.Min, (short)t.Max)).ToList();
                if (wave == content.Waves.Length && ship == bossShip && content.Boss != 0) slots.Add(new(content.Boss, 1, 1));
                return slots;
            },
            Reinforce = (map, point, state) => Relieve(map, point, state, content)
        };

        /// <summary>
        /// The AFS relief of a point under attack: one ship after the last wave, onto the point's
        /// pad, "helping to wipe up remaining Bane forces" (zerias, 2008). Its troops take post by
        /// the gate and fight what is left.
        /// </summary>
        private static void Relieve(MapChannel map, ControlPoints.Point point, Assault state, PlateauDynamicDropshipsContent.Assault content)
        {
            if (content.Relief.Length == 0) return;
            var pool = new SpawnPool
            {
                MapContextId = point.MapContextId, RuntimeMapChannel = map,
                Position = content.ReliefPad.Position, Rotation = content.ReliefPad.Rotation,
                AnimType = 2, Mode = SpawnPoolManager.ModeScripted,
                SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                IsGarrison = true, IsControlPointReinforcement = true, HasSpawned = true, AssaultPointId = point.Id,
                SpawnSlot = content.Relief.Select(t => new SpawnPoolSlot(t.Creature, (short)t.Min, (short)t.Max)).ToList()
            };
            pool.Delivered = creatures =>
            {
                for (var i = 0; i < creatures.Count; i++)
                {
                    var creature = creatures[i];
                    var raw = content.ReliefPost + new Vector3((i - creatures.Count / 2) * 2.5f, 0, 0);
                    var destination = NavMeshManager.NearestWalkable(map, raw) ?? content.ReliefPost;
                    creature.AggroRange = 35;
                    creature.WalkSpeed = creature.RunSpeed;
                    BehaviorManager.Instance.WalkIn(creature, destination);
                }
            };
            state.Reinforcements.Add(pool);
            map.SpawnPools.Add(pool);
            SpawnPoolManager.Instance.EnqueueDropship(map, pool, SpawnPoolManager.Instance.CreateListOfCreatures(pool),
                new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                    PosX = content.ReliefPad.Position.X, PosY = content.ReliefPad.Position.Y, PosZ = content.ReliefPad.Position.Z,
                    Rotation = content.ReliefPad.Rotation });
        }
    }
}
