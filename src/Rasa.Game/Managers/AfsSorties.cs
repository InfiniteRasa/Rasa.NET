using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rasa.Data;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    /// <summary>
    /// The AFS against a public point the Bane hold: now and then one AFS dropship sets a small
    /// squad down outside it, and the squad fights the Bane garrison. Retail: "A single AFS Mech and
    /// a fire team of 3-4 flunkies", "constantly depositing lonely squads with NO BACKUP" (IGN Vault
    /// / planettr, 2007-12), and "while the AFS attacks, we've yet to see the AI overrun one" (Ten
    /// Ton Hammer, Valverde Pools): a squad cannot take the point; it can thin the garrison for
    /// players, who take it. Plans: <see cref="PlateauDynamicDropshipsContent"/>.
    /// </summary>
    internal static class AfsSorties
    {
        private sealed class Sortie
        {
            internal long Due, Started;
            internal SpawnPool Pool;
        }

        private static readonly ConditionalWeakTable<MapChannel, Dictionary<uint, Sortie>> States = new();

        private static PlateauDynamicDropshipsContent.Assault PlanFor(MapChannel map, ControlPoints.Point point) =>
            map == null || map.IsCopy || point == null || point.IsBattleground || map.MapInfo?.MapContextId != point.MapContextId ||
            !ControlPoints.Instance.Config.ControlPointAssaultEnabled ? null
                : PlateauDynamicDropshipsContent.Assaults.FirstOrDefault(a => a.MapId == point.MapContextId && a.PointId == point.Id && a.Sortie.Length > 0);

        internal static IEnumerable<Creature> Squad(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var sortie) && sortie.Pool != null
                ? EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool == sortie.Pool && CreatureManager.IsLivingOnMap(map, c))
                : Enumerable.Empty<Creature>();

        internal static void Worker(MapChannel map, ControlPoints.Point point)
        {
            var plan = PlanFor(map, point);
            var states = map == null ? null : States.GetValue(map, _ => new Dictionary<uint, Sortie>());
            if (plan == null || point.HeldByAfs)
            {
                if (point != null && states != null && states.Remove(point.Id, out var old))
                    Clear(map, old);
                return;
            }
            var now = LandingZoneAssaults.Now();
            if (!states.TryGetValue(point.Id, out var state))
                states[point.Id] = state = new Sortie { Due = now + plan.SortieFirstDelaySeconds * 1000L };
            if (state.Pool == null && now >= state.Due)
            {
                state.Due = now + plan.SortiePeriodSeconds * 1000L;
                state.Started = now;
                Launch(map, point, plan, state);
            }
            if (state.Pool == null) return;
            if (now - state.Started >= plan.SortieLifetimeSeconds * 1000L)
            {
                Clear(map, state);
                return;
            }
            var garrison = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.BanePools.Contains(c.SpawnPool.DbId) &&
                CreatureManager.IsLivingOnMap(map, c)).ToArray();
            foreach (var trooper in Squad(map, point).ToArray())
            {
                if (trooper.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting) continue;
                var target = garrison.OrderBy(c => Vector3.DistanceSquared(c.Position, trooper.Position)).FirstOrDefault();
                if (target == null || !BehaviorManager.Instance.TrySetActionFighting(trooper, target.EntityId))
                    BehaviorManager.Instance.WalkIn(trooper, NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position);
            }
        }

        internal static bool TryStart(MapChannel map, ControlPoints.Point point)
        {
            Worker(map, point);
            if (PlanFor(map, point) == null || point.HeldByAfs || !States.TryGetValue(map, out var states) ||
                !states.TryGetValue(point.Id, out var state) || state.Pool != null)
                return false;
            state.Due = LandingZoneAssaults.Now();
            Worker(map, point);
            return true;
        }

        private static void Launch(MapChannel map, ControlPoints.Point point, PlateauDynamicDropshipsContent.Assault plan, Sortie state)
        {
            var drop = plan.Drops[Random.Shared.Next(plan.Drops.Length)];
            var pool = new SpawnPool
            {
                MapContextId = point.MapContextId, RuntimeMapChannel = map, Position = drop.Position, Rotation = drop.Rotation,
                AnimType = 2, Mode = SpawnPoolManager.ModeScripted, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                HasSpawned = true, IsControlPointReinforcement = true, AssaultPointId = point.Id,
                SpawnSlot = plan.Sortie.Select(t => new SpawnPoolSlot(t.Creature, (short)t.Min, (short)t.Max)).ToList()
            };
            pool.Delivered = creatures =>
            {
                foreach (var creature in creatures)
                {
                    creature.AggroRange = 35;
                    BehaviorManager.Instance.WalkIn(creature, NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position);
                }
            };
            state.Pool = pool;
            map.SpawnPools.Add(pool);
            SpawnPoolManager.Instance.EnqueueDropship(map, pool, SpawnPoolManager.Instance.CreateListOfCreatures(pool),
                new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                    PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
        }

        private static void Clear(MapChannel map, Sortie state)
        {
            var pool = state.Pool;
            state.Pool = null;
            if (pool == null) return;
            pool.Suspended = true;
            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool == pool).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, ship);
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                pool.QueuedCreatures = 0;
                pool.DropshipQueue = 0;
                pool.QueuedCreatureList = null;
            }
            var pools = new List<SpawnPool> { pool };
            LandingZoneAssaults.CountLiving(map, pools);
            ControlPoints.TakeOffLiving(map, pools);
            pool.Delivered = null;
            map.SpawnPools.Remove(pool);
        }
    }
}
