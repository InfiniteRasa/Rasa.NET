using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;
    using Structures.World;

    public partial class ControlPoints
    {
        private readonly Dictionary<uint, Point> _serviceLinks = new();

        /// <summary>A friendly conversational or trading NPC, rather than a combat-only guard.</summary>
        internal static bool IsServiceNpc(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature) &&
            creature.TargetCategory == TargetCategory.Friendly && creature.Npc is { } npc &&
            (npc.Vendor != null || npc.NpcMissionIds?.Count > 0 || npc.NpcPackageId != 0 ||
             npc.NpcIsAuctioneer || npc.NpcIsClanMaster || npc.NpcIsTrainer);

        private Point NearestServicePoint(MapChannel map, Vector3 position)
        {
            if (map?.MapInfo == null || map.IsCopy)
                return null;
            var radius = Config?.ServiceNpcRadius ?? 130f;
            if (!float.IsFinite(radius) || radius <= 0)
                return null;
            return OnMap(map.MapInfo.MapContextId).Where(point => !point.IsBattleground &&
                    Vector3.DistanceSquared(point.Position, position) <= radius * radius)
                .OrderBy(point => Vector3.DistanceSquared(point.Position, position))
                .ThenBy(point => point.Id).FirstOrDefault();
        }

        /// <summary>The point owning a scene-created service NPC at its authored position.</summary>
        internal Point ServicePointOf(MapChannel map, uint creatureId, Vector3 position) =>
            IsServiceNpc(creatureId) ? NearestServicePoint(map, position) : null;

        /// <summary>
        /// Explicit service links and existing AFS garrison links override proximity. A pool
        /// mixing combatants and service NPCs is never automatically assigned as a whole.
        /// Scenario group-local spawn ids must not be interpreted as world spawnpool ids.
        /// </summary>
        internal Point ServicePointOf(MapChannel map, SpawnPool pool)
        {
            if (map?.MapInfo == null || map.IsCopy || pool == null ||
                pool.MapContextId != map.MapInfo.MapContextId ||
                pool.RuntimeMapChannel != null && !ReferenceEquals(pool.RuntimeMapChannel, map))
                return null;
            var native = pool.SceneRunId == null && pool.SpawnPolicy != MissionSpawnGroupPolicy.ScenarioControlled;
            if (native && _serviceLinks.TryGetValue(pool.DbId, out var linked))
                return linked.MapContextId == pool.MapContextId && !linked.IsBattleground ? linked : null;
            var slots = pool.SpawnSlot?.Where(slot => slot != null && slot.CreatureId != 0 &&
                slot.CountMax > 0 && slot.CountMax >= slot.CountMin).ToArray();
            if (slots == null || slots.Length == 0 || !slots.All(slot => IsServiceNpc(slot.CreatureId)))
                return null;
            if (native && _byPool.TryGetValue(pool.DbId, out linked))
                return linked.MapContextId == pool.MapContextId && linked.AfsPools.Contains(pool.DbId) && !linked.IsBattleground
                    ? linked : null;
            return NearestServicePoint(map, pool.Position);
        }

        private void UpdateServiceState(MapChannel map, SpawnPool pool)
        {
            var point = ServicePointOf(map, pool);
            var wasBlocked = pool.ServiceSuspended;
            var previous = pool.ServiceControlPointId;
            pool.ServiceControlPointId = point?.Id ?? 0;
            pool.ServiceSuspended = point != null && !point.HeldByAfs;
            if (!pool.ServiceSuspended && (wasBlocked || previous == 0 && point != null))
            {
                pool.HasSpawned = false;
                pool.UpdateTimer = pool.RespawnTime;
            }
        }

        /// <summary>
        /// Applies startup ownership, capture and configuration changes to all service pools.
        /// Despawning uses normal entity cleanup: stale vendor ids, stock, conversation markers,
        /// and far-away mission contacts are removed without awarding kills or mission progress.
        /// </summary>
        internal void RefreshServices(MapChannel map)
        {
            if (map?.MapInfo == null)
                return;
            var closed = new List<SpawnPool>();
            foreach (var pool in map.SpawnPools.ToArray())
            {
                if (pool == null)
                    continue;
                UpdateServiceState(map, pool);
                if (pool.ServiceSuspended)
                    closed.Add(pool);
            }
            if (closed.Count > 0)
                MissionApplication.Instance.PublicActors.ServicesUnavailable(map, closed);
            TakeOffLiving(map, closed);
            // A scene recreates its removed actor from durable desired effects on recapture.
            // Retaining its old pool would accumulate duplicate scenario pools each capture.
            foreach (var pool in closed.Where(pool => pool.SpawnPolicy == MissionSpawnGroupPolicy.ScenarioControlled &&
                pool.AliveCreatures == 0 && pool.QueuedCreatures == 0 && pool.DropshipQueue == 0))
                map.SpawnPools.Remove(pool);
        }

        /// <summary>Checked by both ordinary and scene creation, including delayed arrivals.</summary>
        internal bool MaySpawnServicePool(SpawnPool pool)
        {
            if (pool == null)
                return true;
            var map = pool.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(pool.MapContextId);
            if (map == null)
                return true;
            UpdateServiceState(map, pool);
            return !pool.ServiceSuspended;
        }
    }
}
