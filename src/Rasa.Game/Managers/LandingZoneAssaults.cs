using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rasa.Data;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Packets.MapChannel.Server;

namespace Rasa.Managers
{
    /// <summary>
    /// Timed Bane assaults on public control points the AFS holds: Wilderness Landing Zone, and
    /// each point with a plan in <see cref="Plans"/>. All deadlines use the map worker's monotonic clock.
    /// </summary>
    internal static partial class LandingZoneAssaults
    {
        private sealed class Assault
        {
            internal long Due, Started, Logged;
            internal int Waves;
            /// <summary>Assaults the AFS have held off since the point was last taken: the next is that much bigger.</summary>
            internal int Repelled;
            internal bool WarningSent, Relieved;
            internal readonly List<SpawnPool> Pools = new();
            internal readonly List<SpawnPool> Reinforcements = new();
            internal Creature Boss;
            internal long? Claim;
            internal int Vitality;
        }
        private static readonly ConditionalWeakTable<MapChannel, Dictionary<uint, Assault>> States = new();
        internal static Func<long> Now = () => Environment.TickCount64;
        internal static bool ControlsCapture(MapChannel map, ControlPoints.Point point) =>
            map != null && !map.IsCopy && point != null && !point.IsBattleground &&
            map.MapInfo?.MapContextId == point.MapContextId && PlanFor(point) != null;

        private static Dictionary<uint, Assault> StatesOf(MapChannel map) => States.GetValue(map, _ => new Dictionary<uint, Assault>());

        /// <summary>Each point of the map with an assault plan, from the control points' worker.</summary>
        internal static void Worker(MapChannel map, IEnumerable<ControlPoints.Point> points)
        {
            foreach (var point in points)
            {
                if (PlanFor(point) != null)
                    Worker(map, point);
                AfsSorties.Worker(map, point);
            }
        }

        internal static void Worker(MapChannel map, ControlPoints.Point point, bool warn = true)
        {
            if (point == null) return;
            if (!ControlsCapture(map, point) || !point.HeldByAfs)
            {
                if (map != null && States.TryGetValue(map, out var states) && states.Remove(point.Id, out var old))
                    Clear(map, point, old);
                return;
            }
            var plan = PlanFor(point);
            var now = Now();
            var state = StatesOf(map).TryGetValue(point.Id, out var known) ? known
                : StatesOf(map)[point.Id] = new Assault { Due = now + plan.FirstDelayMs };
            // Warn once for the pending assault, on the first worker tick in its final five minutes.
            // A skipped deadline or a GM-forced immediate start must not produce a late warning.
            if (warn && state.Waves == 0 && !state.WarningSent && now < state.Due && state.Due - now <= 300000)
            {
                state.WarningSent = true;
                var message = $"The Bane are mustering for an attack on {plan.Name ?? point.Name} Control Point. Reinforcements requested!";
                foreach (var client in map.ClientList.ToArray())
                    if (client?.State == ClientState.Ingame && MapInstanceScope.Contains(map, client.Player))
                        CommunicatorManager.Instance.SystemMessage(client, message);
            }
            if (state.Waves == 0 && now >= state.Due)
            {
                state.Started = now;
                state.WarningSent = false;
                state.Due = now + plan.PeriodMs;
                Launch(map, point, plan, state, 1);
            }
            if (state.Waves == 0) return;
            var gap = plan.WaveGapMs;
            // Fixed offsets from the start; survivors never delay a later wave.
            while (state.Waves < plan.Waves && now - state.Started >= state.Waves * gap)
                Launch(map, point, plan, state, state.Waves + 1);
            if (!state.Relieved && now - state.Started >= plan.ReliefDelayMs)
            {
                state.Relieved = true;
                plan.Reinforce(map, point, state);
            }
            // A claim under way finishes first (live run 2026-10-30: the timeout ended one 8 s in, with
            // no defender left, and counted the assault as held off).
            if (now - state.Started >= Math.Max(plan.LifetimeMs, (plan.Waves - 1) * gap + 60000) && !state.Claim.HasValue)
            {
                // Over with the AFS still holding: held off (a capture leaves through Claim, and a
                // point that changes hands drops its state, so the count starts again).
                if (plan.Escalates) state.Repelled++;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault held off ({state.Repelled} held off since it was last taken).");
                Clear(map, point, state);
                return;
            }
            if (now - state.Logged >= 60000)
            {
                state.Logged = now;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault +{(now - state.Started) / 1000} s, {Defenders(map, point, state)} AFS defenders " +
                    $"and {EntityManager.Instance.Creatures.Values.Count(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))} Bane stand: " +
                    string.Join("; ", EntityManager.Instance.Creatures.Values.Where(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))
                        .Select(c => $"{c.DbId} {Vector3.Distance(c.Position, point.Position):0} m {(c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting ? "fighting" : "idle")} " +
                            $"hp {c.Attributes[Attributes.Health].Current:0}")) + ".");
            }
            if (state.Boss != null && !CreatureManager.IsLivingOnMap(map, state.Boss)) Cancel(point, state);
            var guards = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(map, c.SpawnPool) == null && CreatureManager.IsLivingOnMap(map, c)).Concat(Reinforcements(map, state)).ToArray();
            foreach (var creature in EntityManager.Instance.Creatures.Values.Where(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c)).ToArray())
            {
                if (creature == state.Boss && PrepareClaim(map, point, creature, state) && Claim(map, point, state, now)) { if (!point.HeldByAfs) return; continue; }
                if (LandingZoneBreach.Order(map, point, creature, guards)) continue;
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting) continue;
                var guard = guards
                    .OrderBy(c => Vector3.DistanceSquared(creature.Position, c.Position)).FirstOrDefault();
                if (guard == null || !BehaviorManager.Instance.TrySetActionFighting(creature, guard.EntityId))
                    BehaviorManager.Instance.WalkIn(creature, Destination(map, point));
            }
        }
        internal static bool TryStart(MapChannel map, ControlPoints.Point point)
        {
            if (!ControlsCapture(map, point) || !point.HeldByAfs) return false;
            Worker(map, point, warn: false);
            var state = StatesOf(map).TryGetValue(point.Id, out var known) ? known : StatesOf(map)[point.Id] = new Assault();
            if (state.Waves != 0) return false;
            state.Due = Now();
            Worker(map, point, warn: false);
            return true;
        }
        private static bool PrepareClaim(MapChannel map, ControlPoints.Point point, Creature boss, Assault state)
        {
            if (LandingZoneBreach.StandingField(map, point) != null ||
                ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs) == ControlPoints.Garrison.Standing ||
                Reinforcements(map, state).Any())
                return true;
            // A defending player can interrupt a claim; a service NPC cannot hold the boss in combat.
            if (boss.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                EntityManager.Instance.Players.ContainsKey(boss.Controller.ActionFighting.TargetEntityId)) return true;
            if (boss.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                BehaviorManager.Instance.GiveUp(boss);
            return true;
        }
        private static Vector3 Destination(MapChannel map, ControlPoints.Point point) =>
            NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position;

        private static void Launch(MapChannel map, ControlPoints.Point point, Plan plan, Assault state, int wave)
        {
            var bossShip = Random.Shared.Next(plan.Drops.Length);
            for (var i = 0; i < plan.Drops.Length; i++)
            {
                var drop = plan.Drops[i];
                var slots = plan.Ship(wave, i, bossShip, state.Repelled);
                if (slots.Count == 0) continue;
                var pool = new SpawnPool { MapContextId = point.MapContextId, RuntimeMapChannel = map,
                    Position = drop.Position, Rotation = drop.Rotation, SpawnSlot = slots, AnimType = 1,
                    Mode = SpawnPoolManager.ModeScripted, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                    HasSpawned = true, IsControlPointAssault = true, AssaultPointId = point.Id };
                pool.Delivered = creatures => {
                    foreach (var creature in creatures)
                    {
                        if (creature.DbId == plan.Boss) state.Boss = creature;
                        BehaviorManager.Instance.WalkIn(creature, Destination(map, point));
                    }
                };
                state.Pools.Add(pool);
                map.SpawnPools.Add(pool);
                var brings = SpawnPoolManager.Instance.CreateListOfCreatures(pool);
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault wave {wave}/{plan.Waves}, ship {i + 1}: " +
                    $"{brings.Count} Bane ({string.Join(", ", brings.GroupBy(c => c.DbId).Select(g => $"{g.Count()}x {g.Key}"))}), {state.Repelled} held off before.");
                SpawnPoolManager.Instance.EnqueueDropship(map, pool, brings,
                    new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                        PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
            }
            state.Waves = wave;
            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): before wave {wave}, {Defenders(map, point, state)} AFS defenders " +
                $"and {EntityManager.Instance.Creatures.Values.Count(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))} Bane of the assault stand.");
            foreach (var client in map.ClientList)
                CommunicatorManager.Instance.SystemMessage(client, $"{plan.Name ?? point.Name}: Bane assault wave {wave}/{plan.Waves} incoming!");
        }
        /// <summary>
        /// Sets each pool's living count to the creatures of it that stand: TakeOffLiving takes
        /// nothing when every count is 0, and a count that has drifted below its living (live run
        /// 2026-10-29: a Commander and a Technician of a held-off assault stayed at the point until
        /// it fell) left them standing after their assault was over.
        /// </summary>
        internal static void CountLiving(MapChannel map, List<SpawnPool> pools)
        {
            foreach (var pool in pools)
            {
                var living = EntityManager.Instance.Creatures.Values.Count(c => c.SpawnPool == pool && CreatureManager.IsLivingOnMap(map, c));
                if (living > pool.AliveCreatures) pool.AliveCreatures = living;
            }
        }

        /// <summary>Assaults on the point the AFS have held off since it was last taken (.cp list).</summary>
        internal static int Repelled(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var state) ? state.Repelled : 0;

        /// <summary>The point's AFS garrison and relief that stand (for the Debug log).</summary>
        private static int Defenders(MapChannel map, ControlPoints.Point point, Assault state) =>
            EntityManager.Instance.Creatures.Values.Count(c => c.SpawnPool != null && CreatureManager.IsLivingOnMap(map, c) &&
                (point.AfsPools.Contains(c.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(map, c.SpawnPool) == null || state.Reinforcements.Contains(c.SpawnPool)));

        private static int Vitality(Creature boss) =>
            (int)boss.Attributes[Attributes.Health].Current + (int)boss.Attributes[Attributes.Armor].Current;

        private static bool Claim(MapChannel map, ControlPoints.Point point, Assault state, long now)
        {
            var boss = state.Boss;
            var ready = LandingZoneBreach.StandingField(map, point) == null &&
                CreatureManager.IsLivingOnMap(map, boss) &&
                ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs) != ControlPoints.Garrison.Standing &&
                !Reinforcements(map, state).Any() &&
                Vector3.DistanceSquared(boss.Position, Destination(map, point)) <= 36 &&
                boss.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting &&
                !boss.ActiveEffects.Values.Any(e => e.IsStun && !e.IsExpired);
            if (!ready || (state.Claim.HasValue && Vitality(boss) < state.Vitality))
            {
                Cancel(point, state);
                return false;
            }
            if (point.Object == null || (point.Object.UsedBy != null && point.Object.UsedBy != boss)) return false;
            if (!state.Claim.HasValue)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the {boss.Name ?? "boss"} claims it; AFS garrison " +
                    $"{ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs)}, {Defenders(map, point, state)} AFS defenders stand.");
                state.Claim = now;
                point.Object.UsedBy = boss;
                CellManager.Instance.CellCallMethod(point.Object, new LockToActorPacket(boss.EntityId));
                CellManager.Instance.CellCallMethod(point.Object, new UseInterruptiblePacket(boss.EntityId));
                BehaviorManager.Instance.SetActionAnchor(boss, boss.Position);
            }
            state.Vitality = Vitality(boss);
            if (now - state.Claim.Value >= Math.Max(1, ControlPoints.Instance.Config.LandingZoneAssaultCaptureSeconds) * 1000L)
            {
                Cancel(point, state);
                ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
                Clear(map, point, state);
            }
            return true;
        }
        internal static bool IsCapturing(MapChannel map, Actor actor, DynamicObject obj) =>
            map != null && States.TryGetValue(map, out var states) &&
            states.Values.Any(state => state.Claim.HasValue && state.Boss == actor && obj.UsedBy == actor);
        private static void Cancel(ControlPoints.Point point, Assault state)
        {
            if (state.Claim.HasValue && point?.Object?.UsedBy == state.Boss)
            {
                CellManager.Instance.CellCallMethod(point.Object, new UseInterruptedPacket(state.Boss.EntityId));
                CellManager.Instance.CellCallMethod(point.Object, new LockToActorPacket(0));
                point.Object.UsedBy = null;
            }
            state.Claim = null;
        }
        private static void Clear(MapChannel map, ControlPoints.Point point, Assault state)
        {
            Cancel(point, state);
            LandingZoneGarrison.CancelReinforcements(map, state.Reinforcements);
            var pools = state.Pools.Concat(state.Reinforcements).ToList();
            foreach (var pool in pools) pool.Suspended = true;
            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => pools.Contains(s.SpawnPool)).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, ship);
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                ship.SpawnPool.QueuedCreatures = 0;
                ship.SpawnPool.DropshipQueue = 0;
                ship.SpawnPool.QueuedCreatureList = null;
            }
            CountLiving(map, pools);
            ControlPoints.TakeOffLiving(map, pools);
            foreach (var pool in pools) { pool.Delivered = null; map.SpawnPools.Remove(pool); }
            state.Pools.Clear();
            state.Reinforcements.Clear();
            state.Boss = null;
            state.Waves = 0;
            state.Relieved = false;
        }
    }
}
