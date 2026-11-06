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
    /// players, who take it. Live run 2026-11-05 (Plateau Northwest): the Bane garrison came back
    /// at full health under a squad that had cleared it. Now, while a sortie runs, a garrison it has
    /// cleared stays down (<see cref="HoldsGarrison"/>), so a player can take the point.
    /// Plans: <see cref="DynamicDropshipsContent"/>.
    /// </summary>
    internal static class AfsSorties
    {
        private sealed class Sortie
        {
            internal long Due, Started, Logged;
            internal SpawnPool Pool;
            /// <summary>For the Debug log: the garrison down at the last pass, the hold logged.</summary>
            internal bool GarrisonDown, HoldLogged;
            /// <summary>Whether its ship has set the squad down.</summary>
            internal bool Landed;
            internal readonly LandingZoneAssaults.FightWatch Watch = new();
        }

        /// <summary>How far past its point's farthest Bane garrison post a trooper may follow a fight before it comes back (ESTIMATE).</summary>
        internal const float LeashBeyondGarrison = 20f;

        private static readonly ConditionalWeakTable<MapChannel, Dictionary<uint, Sortie>> States = new();

        private static DynamicDropshipsContent.Assault PlanFor(MapChannel map, ControlPoints.Point point) =>
            map == null || map.IsCopy || point == null || point.IsBattleground || map.MapInfo?.MapContextId != point.MapContextId ||
            !ControlPoints.Instance.Config.ControlPointAssaultEnabled ? null
                : DynamicDropshipsContent.Assaults.FirstOrDefault(a => a.MapId == point.MapContextId && a.PointId == point.Id && a.Sortie.Length > 0);

        internal static IEnumerable<Creature> Squad(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var sortie) && sortie.Pool != null
                ? EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool == sortie.Pool && CreatureManager.IsLivingOnMap(map, c))
                : Enumerable.Empty<Creature>();

        /// <summary>Whether the sortie running on the point has cleared its Bane garrison (as last logged).</summary>
        internal static bool Cleared(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var sortie) && sortie.Pool != null && sortie.GarrisonDown;

        /// <summary>Whether a sortie runs on the point now (its ship on the way, or its squad set down).</summary>
        internal static bool IsRunning(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var sortie) && sortie.Pool != null;

        /// <summary>
        /// Whether the Bane garrison of a point that is down waits while an AFS sortie runs on the
        /// point (ControlPoints.Worker): it does not come back under the squad (live run 2026-11-05:
        /// the mortars were back at full health 5 min after the garrison fell, the squad on the point).
        /// </summary>
        internal static bool HoldsGarrison(MapChannel map, ControlPoints.Point point)
        {
            if (point == null || point.HeldByAfs || !States.TryGetValue(map, out var states) || !states.TryGetValue(point.Id, out var state) || state.Pool == null)
                return false;
            if (!state.HoldLogged)
            {
                state.HoldLogged = true;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): Bane garrison down; its return held back while the AFS sortie runs.");
            }
            return true;
        }

        /// <summary>The answer to a GM's ".cp &lt;id&gt; sortie".</summary>
        internal static string GmStart(MapChannel map, ControlPoints.Point point)
        {
            if (TryStart(map, point)) return $"AFS sortie against {point.Name} started.";
            return IsRunning(map, point) ? $"An AFS sortie against {point.Name} is already under way."
                : "Stand on the point's public map with the point held by the Bane and its sorties enabled.";
        }

        internal static void Worker(MapChannel map, ControlPoints.Point point)
        {
            var plan = PlanFor(map, point);
            var states = map == null ? null : States.GetValue(map, _ => new Dictionary<uint, Sortie>());
            if (plan == null || point.HeldByAfs)
            {
                if (point != null && states != null && states.Remove(point.Id, out var old))
                    Clear(map, point, old);
                return;
            }
            var now = LandingZoneAssaults.Now();
            if (!states.TryGetValue(point.Id, out var state))
                states[point.Id] = state = new Sortie { Due = now + plan.SortieFirstDelaySeconds * 1000L };
            // Overdue by a whole period or more: no worker ran (the map worker runs only while a
            // player is on the map). The squad comes as after a server start, not the moment a
            // player comes back.
            if (state.Pool == null && now - state.Due >= plan.SortiePeriodSeconds * 1000L)
            {
                state.Due = now + plan.SortieFirstDelaySeconds * 1000L;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): AFS sortie overdue (no worker ran); the next in {plan.SortieFirstDelaySeconds} s.");
            }
            if (state.Pool == null && now >= state.Due)
            {
                state.Due = now + plan.SortiePeriodSeconds * 1000L;
                state.Started = now;
                Launch(map, point, plan, state);
            }
            if (state.Pool == null) return;
            var garrison = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.BanePools.Contains(c.SpawnPool.DbId) &&
                CreatureManager.IsLivingOnMap(map, c)).ToArray();
            var squad = Squad(map, point).ToArray();
            // The garrison cleared: none of it alive, and no ship of it on the way. Not the pools'
            // counts (ControlPoints.GarrisonOf): live run 2026-11-05, Bane Assertion Camp, "cleared"
            // and "standing again" flapped every 10-20 s with nobody of the garrison alive and none
            // set down; the sortie holds its return back (HoldsGarrison), so a count that says
            // "on the way" brings nobody.
            var down = garrison.Length == 0 && !DynamicObjectManager.Instance.Dropships.Values.Any(s => s.SpawnPool != null && point.BanePools.Contains(s.SpawnPool.DbId));
            if (down != state.GarrisonDown)
            {
                state.GarrisonDown = down;
                Logger.WriteLog(LogType.Debug, down
                    ? $"Control point {point.Id} ({point.Name}): the AFS sortie cleared the Bane garrison at +{(now - state.Started) / 1000} s, {squad.Length} AFS of it stand: garrison down; a player can take the point."
                    : $"Control point {point.Id} ({point.Name}): Bane garrison standing again at sortie +{(now - state.Started) / 1000} s.");
            }
            // The whole squad dead, its ship gone: over, and the Bane garrison's hold with it.
            if (state.Landed && squad.Length == 0 && !DynamicObjectManager.Instance.Dropships.Values.Any(s => s.SpawnPool == state.Pool))
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): AFS sortie over at +{(now - state.Started) / 1000} s: none of the squad stands.");
                Clear(map, point, state);
                return;
            }
            var end = plan.SortieLifetimeSeconds * 1000L;
            if (now - state.Started >= end)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): AFS sortie over at +{(now - state.Started) / 1000} s, {squad.Length} of it standing.");
                Clear(map, point, state);
                return;
            }
            if (now - state.Logged >= 60000)
            {
                state.Logged = now;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): sortie +{(now - state.Started) / 1000} s, {garrison.Length} Bane of the garrison " +
                    $"and {squad.Length} AFS stand: " + string.Join("; ", squad.Select(c => $"{c.DbId} {Flat(c.Position, point.Position):0} m " +
                        $"{(c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting ? $"fighting {c.Controller.ActionFighting.TargetEntityId}" : "idle")} hp {c.Attributes[Attributes.Health].Current:0}")) + ".");
            }
            // Live run 2026-11-05: the mech, its riflemen dead, stood 77 m out until the sortie ended.
            // A trooper is leashed to the point, a little past its farthest garrison post - and in a
            // fight, a little past its target: Bane Assertion Camp has a garrison post 107 m out, and
            // troopers 110 m out stood "fighting" an NPC 44 m from the point inside that leash.
            var farthest = ControlPoints.Instance.PoolsOfSide(map, point, ControlPoints.Bane)
                .Select(p => Flat(p.Position, point.Position)).DefaultIfEmpty(0f).Max();
            foreach (var trooper in squad)
            {
                var from = Flat(trooper.Position, point.Position);
                var leashed = false;
                if (LandingZoneAssaults.FightsTheDown(map, trooper))
                    BehaviorManager.Instance.GiveUp(trooper);
                if (trooper.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                {
                    var foe = EntityManager.Instance.GetActor(trooper.Controller.ActionFighting.TargetEntityId);
                    var reach = LeashBeyondGarrison + Math.Min(farthest, foe == null ? 0f : Flat(foe.Position, point.Position));
                    // Only the garrison, a player or one fighting it (LandingZoneAssaults.FightsOffPlan),
                    // and not a fight that has got it nowhere (FightWatch).
                    var offPlan = LandingZoneAssaults.FightsOffPlan(trooper, garrison);
                    var stalled = !offPlan && state.Watch.Stalled(trooper, now);
                    if (from <= reach && !offPlan && !stalled) continue;
                    leashed = from > reach;
                    Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): sortie trooper {trooper.DbId} gives up its fight with " +
                        $"{trooper.Controller.ActionFighting.TargetEntityId} ({(offPlan ? "not the garrison" : stalled ? $"no harm done in {LandingZoneAssaults.NoHarmMs / 1000} s" : $"{from:0} m out, past its leash")}).");
                    BehaviorManager.Instance.GiveUp(trooper);
                }
                // Leashed back from a fight: to the point first, the next fight from there.
                var strayed = leashed || from > LeashBeyondGarrison + farthest;
                // The Bane turrets last (as LandingZoneAssaults.Ranked): their mounts are off the navmesh.
                var target = strayed ? null : garrison.Where(c => !state.Watch.Shuns(trooper, c, now))
                    .OrderBy(c => Emplacements.Is(c)).ThenBy(c => Vector3.DistanceSquared(c.Position, trooper.Position)).FirstOrDefault();
                if (target == null || !BehaviorManager.Instance.TrySetActionFighting(trooper, target.EntityId))
                    Approach(trooper, Destination(map, point));
            }
        }

        private static Vector3 Destination(MapChannel map, ControlPoints.Point point) =>
            NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position;

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        /// <summary>Walks the trooper to <paramref name="destination"/>, unless it is already on its way there.</summary>
        private static void Approach(Creature trooper, Vector3 destination)
        {
            var wander = trooper.Controller.ActionWander;
            if (trooper.Controller.CurrentAction == BehaviorManager.BehaviorActionWander && wander.Arriving &&
                Vector3.DistanceSquared(wander.WanderDestination, destination) < 1f)
                return;
            BehaviorManager.Instance.WalkIn(trooper, destination);
        }

        internal static bool TryStart(MapChannel map, ControlPoints.Point point)
        {
            var running = IsRunning(map, point);
            Worker(map, point);
            if (PlanFor(map, point) == null || point.HeldByAfs || !States.TryGetValue(map, out var states) ||
                !states.TryGetValue(point.Id, out var state))
                return false;
            // One that was due and that the worker has just started is the one asked for.
            if (state.Pool != null)
                return !running;
            state.Due = LandingZoneAssaults.Now();
            Worker(map, point);
            return true;
        }

        private static void Launch(MapChannel map, ControlPoints.Point point, DynamicDropshipsContent.Assault plan, Sortie state)
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
                state.Landed = true;
                foreach (var creature in creatures)
                {
                    creature.AggroRange = 35;
                    // It seeks out the point's Bane garrison only (Creature.SeeksOnly).
                    creature.SeeksOnly = target => target?.SpawnPool != null && point.BanePools.Contains(target.SpawnPool.DbId);
                    BehaviorManager.Instance.WalkIn(creature, NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position);
                    // Home is the point (WalkIn sets it to where it walks): a trooper that leashes runs back to it.
                    creature.HomePos.Position = point.Position;
                }
            };
            state.Pool = pool;
            state.HoldLogged = false;
            state.Landed = false;
            map.SpawnPools.Add(pool);
            var brings = SpawnPoolManager.Instance.CreateListOfCreatures(pool);
            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): AFS sortie: {brings.Count} AFS " +
                $"({string.Join(", ", brings.GroupBy(c => c.DbId).Select(g => $"{g.Count()}x {g.Key}"))}).");
            SpawnPoolManager.Instance.EnqueueDropship(map, pool, brings,
                new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                    PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
        }

        private static void Clear(MapChannel map, ControlPoints.Point point, Sortie state)
        {
            state.GarrisonDown = false;
            state.Landed = false;
            state.Watch.Clear();
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
            var left = LandingZoneAssaults.TakeOffLeftovers(map, pools);
            if (left > 0)
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): {left} of the AFS sortie still stood after it; taken off.");
            pool.Delivered = null;
            map.SpawnPools.Remove(pool);
        }
    }
}
