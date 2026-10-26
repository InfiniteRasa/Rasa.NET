using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    /// <summary>Arrival and individual beats for Landing Zone's three ordinary garrison pools.</summary>
    internal static class LandingZoneGarrison
    {
        private sealed class Delivery
        {
            internal SpawnPool Pool;
            internal List<Creature> Creatures;
            internal long Due;
        }
        private sealed class State
        {
            internal long Elapsed, NextGunship;
            internal readonly List<Delivery> Pending = new();
        }
        private static readonly ConditionalWeakTable<MapChannel, State> States = new();
        internal static bool IsGroup(SpawnPool pool) => pool?.DbId is >= LandingZoneGarrisonContent.MechPool and <= LandingZoneGarrisonContent.InfantryB;
        private static bool Public(MapChannel map) => map?.MapInfo?.MapContextId == LandingZoneGarrisonContent.Map && !map.IsCopy;
        private static bool Available(SpawnPool pool) => pool.IsGarrison && !pool.Suspended && !pool.Quieted;

        internal static void Worker(MapChannel map, long elapsed)
        {
            if (!Public(map)) return;
            var state = States.GetValue(map, _ => new State());
            state.Elapsed += Math.Max(0, elapsed);
            foreach (var pool in map.SpawnPools.Where(IsGroup))
                pool.Delivered = creatures => Patrol(map, pool, creatures);
            if (!map.SpawnPools.Where(IsGroup).Any(Available)) state.NextGunship = state.Elapsed;
            foreach (var delivery in state.Pending.ToArray())
            {
                if (Available(delivery.Pool) && state.Elapsed < delivery.Due) continue;
                state.Pending.Remove(delivery);
                try
                {
                    if (!Available(delivery.Pool)) continue;
                    if (delivery.Pool.DbId == LandingZoneGarrisonContent.MechPool)
                    {
                        var arrived = SpawnPoolManager.Instance.SpawnCreatures(delivery.Pool, delivery.Creatures);
                        foreach (var creature in arrived)
                            CellManager.Instance.CellCallMethod(map, creature, new TeleportArrivalPacket());
                    }
                    else
                        SpawnPoolManager.Instance.EnqueueDropship(map, delivery.Pool, delivery.Creatures, Arrival());
                }
                finally
                {
                    SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(delivery.Pool, delivery.Creatures.Count);
                    SpawnPoolManager.Instance.DecreaseQueueCount(delivery.Pool);
                }
            }
            // A takeover can happen while a ship is inbound. It never delivers AFS into Bane ownership.
            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values
                .Where(s => ReferenceEquals(s.RuntimeMapChannel, map) && IsGroup(s.SpawnPool) && !Available(s.SpawnPool)).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, ship);
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(ship.SpawnPool, ship.SpawnPool.QueuedCreatures);
                SpawnPoolManager.Instance.DecreaseQueueCount(ship.SpawnPool);
            }
        }
        private static SpawnPoolArrivalEntry Arrival() => new()
        {
            Kind = SpawnPoolArrivalEntry.KindDropship,
            PosX = LandingZoneGarrisonContent.Gunship.X,
            PosY = LandingZoneGarrisonContent.Gunship.Y,
            PosZ = LandingZoneGarrisonContent.Gunship.Z,
            Rotation = LandingZoneGarrisonContent.GunshipRotation
        };

        internal static bool TryDispatch(MapChannel map, SpawnPool pool, List<Creature> creatures)
        {
            if (!Public(map) || !IsGroup(pool)) return false;
            if (!Available(pool)) return true;
            Queue(map, pool, creatures, pool.DbId != LandingZoneGarrisonContent.MechPool);
            return true;
        }
        internal static void DispatchReinforcements(MapChannel map, SpawnPool pool, List<Creature> creatures) =>
            Queue(map, pool, creatures, true);

        private static void Queue(MapChannel map, SpawnPool pool, List<Creature> creatures, bool gunship)
        {
            var state = States.GetValue(map, _ => new State());
            var due = state.Elapsed + 700;
            if (gunship)
            {
                // Permanent garrison replacements and assault relief share one landing pad.
                due = Math.Max(state.Elapsed, state.NextGunship);
                state.NextGunship = due + 20000;
            }
            pool.HasSpawned = true;
            SpawnPoolManager.Instance.IncreaseQueueCount(pool);
            SpawnPoolManager.Instance.IncreaseQueuedCreatureCount(pool, creatures.Count);
            state.Pending.Add(new Delivery { Pool = pool, Creatures = creatures, Due = due });
        }
        internal static void CancelReinforcements(MapChannel map, IReadOnlyCollection<SpawnPool> pools)
        {
            if (!States.TryGetValue(map, out var state)) return;
            foreach (var delivery in state.Pending.Where(d => pools.Contains(d.Pool)).ToArray())
            {
                state.Pending.Remove(delivery);
                SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(delivery.Pool, delivery.Creatures.Count);
                SpawnPoolManager.Instance.DecreaseQueueCount(delivery.Pool);
            }
        }
        internal static bool TryPlace(MapChannel map, SpawnPool pool, Creature creature, int place)
        {
            if (!Public(map) || !IsGroup(pool)) return false;
            var offset = pool.DbId == LandingZoneGarrisonContent.MechPool ? 4 * place : place == 0 ? 0 : place == 1 ? -2 : 2;
            var raw = pool.Position + new Vector3(offset, 0, 0);
            var at = map.NavMesh?.Nearest(raw) ?? raw;
            CreatureManager.Instance.SetLocation(creature, at, pool.Rotation, pool.MapContextId);
            return true;
        }
        private static void Patrol(MapChannel map, SpawnPool pool, IReadOnlyList<Creature> creatures)
        {
            for (var i = 0; i < creatures.Count; i++)
            {
                var creature = creatures[i];
                var mech = pool.DbId == LandingZoneGarrisonContent.MechPool;
                var route = mech ? LandingZoneGarrisonContent.MechPatrol.Select(p => p + new Vector3(0, 0, -3 * i)).ToArray()
                    : LandingZoneGarrisonContent.Catwalks[i % 3].ToArray();
                if (!mech && pool.DbId == LandingZoneGarrisonContent.InfantryB) Array.Reverse(route);
                creature.Patrol = Route(map, route, mech ? 1.6555f : null, mech ? 0.9376f : null);
                creature.AggroRange = mech ? 22 : 35;
                creature.FightFromPatrol = !mech;
                BehaviorManager.Instance.WalkIn(creature, creature.Patrol[0].Position);
            }
        }
        // Patrols walk their authored heights. Sample the floor and inset polygon-edge corners
        // so the return trip does not raycast against the very edge the funnel routed around.
        private static Vector3 Surface(MapChannel map, Vector3 raw)
        {
            var at = map.NavMesh.Nearest(raw) ?? throw new InvalidOperationException($"No Landing Zone floor at {raw}.");
            if (map.NavMesh.DistanceToWall(at, 0.3f) >= 0.15f) return at;
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Math.PI / 8;
                var probe = at + new Vector3((float)Math.Cos(angle) * 0.3f, 0, (float)Math.Sin(angle) * 0.3f);
                var inside = map.NavMesh.Nearest(probe);
                if (inside.HasValue && Vector3.DistanceSquared(inside.Value, probe) < 0.01f && map.NavMesh.DistanceToWall(inside.Value, 0.3f) >= 0.15f)
                    return inside.Value;
            }
            return at;
        }
        /// <summary>Expand every leg over the navmesh, then retrace it. No straight cuts through buildings or off catwalks.</summary>
        internal static List<PatrolStep> Route(MapChannel map, IReadOnlyList<Vector3> authored, float? firstFacing = null, float? lastFacing = null)
        {
            var points = new List<Vector3>();
            void Leg(Vector3 raw)
            {
                var to = map.NavMesh?.Nearest(raw) ?? throw new InvalidOperationException($"Landing Zone patrol has no walkable point at {raw}.");
                if (points.Count == 0) { points.Add(Surface(map, to)); return; }
                var path = NavMeshManager.FindPathBounded(map, points[^1], to, out var complete);
                if (!complete || path == null || path.Count == 0) throw new InvalidOperationException($"Landing Zone patrol cannot reach {raw}.");
                foreach (var corner in path)
                {
                    var from = points[^1];
                    var count = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(from, corner)));
                    for (var n = 1; n <= count; n++)
                    {
                        var at = Surface(map, Vector3.Lerp(from, corner, (float)n / count));
                        if (Vector3.DistanceSquared(points[^1], at) > 0.0001f) points.Add(at);
                    }
                }
            }
            foreach (var raw in authored) Leg(raw);
            var turn = points.Count - 1;
            // A funnel route can hug different polygon corners in each direction.
            for (var i = authored.Count - 2; i >= 0; i--) Leg(authored[i]);
            if (points.Count > 1 && Vector3.DistanceSquared(points[^1], points[0]) < 0.0001f) points.RemoveAt(points.Count - 1);
            var result = points.Select((p, i) => new PatrolStep(p, i == 0 ? firstFacing : i == turn ? lastFacing : null,
                i == 0 || i == turn ? 1000 : 0)).ToList();
            return result;
        }
    }
}
