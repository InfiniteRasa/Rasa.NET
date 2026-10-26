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
    /// <summary>Public Wilderness Landing Zone assaults. All deadlines use the map worker's monotonic clock.</summary>
    internal static partial class LandingZoneAssaults
    {
        private sealed class Assault
        {
            internal long Due, Started;
            internal int Waves;
            internal bool WarningSent;
            internal readonly List<SpawnPool> Pools = new();
            internal readonly List<SpawnPool> Reinforcements = new();
            internal Creature Boss;
            internal long? Claim;
            internal int Vitality;
        }
        private static readonly ConditionalWeakTable<MapChannel, Assault> States = new();
        internal static Func<long> Now = () => Environment.TickCount64;
        internal static bool ControlsCapture(MapChannel map, ControlPoints.Point point) =>
            map != null && !map.IsCopy && map.MapInfo?.MapContextId == LandingZoneAssaultContent.MapId &&
            point?.Id == LandingZoneAssaultContent.PointId && !point.IsBattleground &&
            ControlPoints.Instance.Config.LandingZoneAssaultEnabled;

        internal static void Worker(MapChannel map, ControlPoints.Point point, bool warn = true)
        {
            if (!ControlsCapture(map, point) || !point.HeldByAfs)
            {
                if (map != null && States.TryGetValue(map, out var old))
                    Clear(map, point, old);
                if (map != null) States.Remove(map);
                return;
            }
            var config = ControlPoints.Instance.Config;
            var now = Now();
            var state = States.GetValue(map, _ => new Assault { Due = now + Math.Max(1, config.LandingZoneAssaultPeriodSeconds) * 1000L });
            // Warn once for the pending assault, on the first worker tick in its final five minutes.
            // A skipped deadline or a GM-forced immediate start must not produce a late warning.
            if (warn && state.Waves == 0 && !state.WarningSent && now < state.Due && state.Due - now <= 300000)
            {
                state.WarningSent = true;
                var message = $"The Bane are mustering for an attack on {point.Name} Control Point. Reinforcements requested!";
                foreach (var client in map.ClientList.ToArray())
                    if (client?.State == ClientState.Ingame && MapInstanceScope.Contains(map, client.Player))
                        CommunicatorManager.Instance.SystemMessage(client, message);
            }
            if (state.Waves == 0 && now >= state.Due)
            {
                state.Started = now;
                state.WarningSent = false;
                state.Due = now + Math.Max(1, config.LandingZoneAssaultPeriodSeconds) * 1000L;
                Launch(map, point, state, 1);
                Reinforce(map, state);
            }
            if (state.Waves == 0) return;
            var gap = Math.Max(1, config.LandingZoneAssaultWaveSeconds) * 1000L;
            // Fixed offsets from the start; survivors never delay a later wave.
            while (state.Waves < 3 && now - state.Started >= state.Waves * gap)
                Launch(map, point, state, state.Waves + 1);
            if (now - state.Started >= Math.Max(config.LandingZoneAssaultLifetimeSeconds * 1000L, 2 * gap + 60000))
            {
                Clear(map, point, state);
                return;
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
            var state = States.GetValue(map, _ => new Assault());
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

        private static void Launch(MapChannel map, ControlPoints.Point point, Assault state, int wave)
        {
            var bossShip = Random.Shared.Next(6);
            for (var i = 0; i < LandingZoneAssaultContent.Drops.Length; i++)
            {
                var drop = LandingZoneAssaultContent.Drops[i];
                var slots = new List<SpawnPoolSlot> {
                    new(wave == 1 ? LandingZoneAssaultContent.Trainee : LandingZoneAssaultContent.Private, 3, 5),
                    new(wave == 1 ? LandingZoneAssaultContent.Private : LandingZoneAssaultContent.Sergeant, 1, 1) };
                if (wave == 1 || Random.Shared.Next(2) == 0) slots.Add(new(LandingZoneAssaultContent.Hunter, 1, 1));
                else slots.Add(new(LandingZoneAssaultContent.Linker, 2, 2));
                if (wave == 3 && i == bossShip) slots.Add(new(LandingZoneAssaultContent.Boss, 1, 1));
                var pool = new SpawnPool { MapContextId = LandingZoneAssaultContent.MapId, RuntimeMapChannel = map,
                    Position = drop.Position, Rotation = drop.Rotation, SpawnSlot = slots, AnimType = 1,
                    Mode = SpawnPoolManager.ModeScripted, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                    HasSpawned = true, IsControlPointAssault = true };
                pool.Delivered = creatures => {
                    foreach (var creature in creatures)
                    {
                        if (creature.DbId == LandingZoneAssaultContent.Boss) state.Boss = creature;
                        BehaviorManager.Instance.WalkIn(creature, Destination(map, point));
                    }
                };
                state.Pools.Add(pool);
                map.SpawnPools.Add(pool);
                SpawnPoolManager.Instance.EnqueueDropship(map, pool, SpawnPoolManager.Instance.CreateListOfCreatures(pool),
                    new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                        PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
            }
            state.Waves = wave;
            foreach (var client in map.ClientList)
                CommunicatorManager.Instance.SystemMessage(client, $"Landing Zone: Bane assault wave {wave}/3 incoming!");
        }
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
            map != null && States.TryGetValue(map, out var state) && state.Claim.HasValue && state.Boss == actor && obj.UsedBy == actor;
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
            ControlPoints.TakeOffLiving(map, pools);
            foreach (var pool in pools) { pool.Delivered = null; map.SpawnPools.Remove(pool); }
            state.Pools.Clear();
            state.Reinforcements.Clear();
            state.Boss = null;
            state.Waves = 0;
        }
    }
}
