using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    public class SpawnPoolManager
    {
        private static SpawnPoolManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public readonly Dictionary<uint, SpawnPool> LoadedSpawnPools = new Dictionary<uint, SpawnPool>();

        public static SpawnPoolManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SpawnPoolManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal SpawnPoolManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void IncreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue++;
        }

        public void DecreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue--;

            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                spawnPool.UpdateTimer = 0;
        }

        public void IncreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures += count;
        }

        internal void DecreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures -= count;

            if (spawnPool.QueuedCreatures == 0)
                spawnPool.QueuedCreatureList = null;

            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                spawnPool.UpdateTimer = 0;
        }

        public void IncreaseAliveCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures++;
        }

        internal void DecreaseAliveCreatureCount(MapChannel mapChannel, SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures--;
            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                spawnPool.UpdateTimer = 0;
        }

        public void IncreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures++;
        }

        internal void DecreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures--;
        }

        public void SpawnPoolInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var spawnPoolList = unitOfWork.Spawnpools.Get();

            foreach (var data in spawnPoolList)
            {
                var spawnPoolSlots = new List<SpawnPoolSlot>();

                if (data.Creature1Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature1Id, data.Creature1MinCount, data.Creature1MaxCount));
                if (data.Creature2Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature2Id, data.Creature2MinCount, data.Creature2MaxCount));
                if (data.Creature3Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature3Id, data.Creature3MinCount, data.Creature3MaxCount));
                if (data.Creature4Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature4Id, data.Creature4MinCount, data.Creature4MaxCount));
                if (data.Creature5Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature5Id, data.Creature5MinCount, data.Creature5MaxCount));
                if (data.Creature6Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature6Id, data.Creature6MinCount, data.Creature6MaxCount));

                var respawnMilliseconds = data.RespawnTime * 1000L;
                var spawnPool = new SpawnPool
                {
                    AnimType = data.AnimType,
                    MapContextId = data.MapContextId,
                    DbId = data.Id,
                    Position = data.Position,
                    Rotation = (float)data.Rotation,
                    Mode = data.Mode,
                    RespawnTime = respawnMilliseconds,
                    UpdateTimer = respawnMilliseconds,
                    SpawnSlot = spawnPoolSlots
                };

                LoadedSpawnPools.Add(data.Id, spawnPool);
            }

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                InitializeMapChannel(mapChannel);

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedSpawnPools.Count} SpawnPools");
        }

        internal void InitializeMapChannel(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            mapChannel.SpawnPools.Clear();
            foreach (var template in LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId))
                mapChannel.SpawnPools.Add(CloneSpawnPool(template, mapChannel));
        }

        internal void CloneTemplateMap(MapChannel template, MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            mapChannel.SpawnPools.Clear();
            var source = template?.SpawnPools?.Count > 0
                ? template.SpawnPools
                : LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId);

            foreach (var spawnPool in source)
                if (spawnPool != null)
                    mapChannel.SpawnPools.Add(CloneSpawnPool(spawnPool, mapChannel));
        }

        // timePassed is elapsed milliseconds since this map's previous spawn-pool update.
        public void SpawnPoolWorker(MapChannel mapChannel, long timePassed)
        {
            var spawnPools = mapChannel.SpawnPools.Count > 0
                ? mapChannel.SpawnPools
                : LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId).ToList();

            foreach (var spawnPool in spawnPools)
            {
                if (spawnPool.SpawnPolicy == Structures.World.MissionSpawnGroupPolicy.ScenarioControlled)
                    continue;

                if (spawnPool.Mode != 0 || spawnPool.AnimType < 0 || spawnPool.AnimType > 2)
                    continue;

                if (spawnPool.AliveCreatures > 0 || spawnPool.QueuedCreatures > 0 || spawnPool.DropshipQueue > 0)
                    continue;

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    spawnPool.UpdateTimer += Math.Min(Math.Max(0, timePassed), spawnPool.RespawnTime - spawnPool.UpdateTimer);

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    continue; // spawnpool is still on cooldown

                // create list of creatures to spawn
                var creatureList = CreateListOfCreatures(spawnPool);

                if (creatureList.Count == 0)
                    continue; // nothing to spawn

                if (spawnPool.AnimType == 0)    // animType==0; spawn without animation
                {
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);

                    try
                    {
                        SpawnCreatures(spawnPool, creatureList);
                    }
                    finally
                    {
                        DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                    }
                }
                else
                {
                    EnqueueDropship(mapChannel, spawnPool, creatureList);
                }
            }
        }

        private void EnqueueDropship(MapChannel mapChannel, SpawnPool spawnPool, List<Creature> creatureList)
        {
            Dropship dropship = null;
            var reserved = false;
            try
            {
                dropship = new Dropship(spawnPool.AnimType == 1 ? Factions.Bane : Factions.AFS,
                    DropshipType.Spawner, spawnPool);
                spawnPool.QueuedCreatureList = creatureList;
                IncreaseQueueCount(spawnPool);
                IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                reserved = true;
                CellManager.Instance.AddToWorld(mapChannel, dropship);
                DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);
            }
            catch
            {
                try
                {
                    if (dropship != null)
                        RollBackDropship(mapChannel, dropship);
                }
                finally
                {
                    if (reserved)
                    {
                        DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                        DecreaseQueueCount(spawnPool);
                    }
                }
                throw;
            }
        }

        private void RollBackDropship(MapChannel mapChannel, Dropship dropship)
        {
            var entities = EntityManager.Instance;
            var wasRegistered = entities.RegisteredEntities.TryGetValue(dropship.EntityId, out var entityType) &&
                entityType == EntityType.Object &&
                entities.DynamicObjects.TryGetValue(dropship.EntityId, out var registered) && registered == dropship;
            var workers = DynamicObjectManager.Instance.Dropships;
            if (workers.TryGetValue(dropship.EntityId, out var worker) && worker == dropship)
                workers.Remove(dropship.EntityId);
            try
            {
                if (wasRegistered)
                    CellManager.Instance.RemoveFromWorld(mapChannel, dropship);
            }
            catch (Exception exception)
            {
                Logger.WriteLog(LogType.Error, $"Failed to remove rejected dropship {dropship.EntityId}: {exception.Message}");
            }
            finally
            {
                if (mapChannel.MapCellInfo?.Cells != null)
                    foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                        cell?.DynamicObjectList?.RemoveAll(candidate => candidate == dropship);

                var hasObject = entities.DynamicObjects.TryGetValue(dropship.EntityId, out var current);
                var ownsObject = hasObject && current == dropship;
                if (ownsObject)
                    entities.UnregisterDynamicObject(dropship.EntityId);

                if (ownsObject || (!wasRegistered && !hasObject))
                {
                    if (entities.RegisteredEntities.ContainsKey(dropship.EntityId))
                        entities.ReleaseEntity(dropship.EntityId, EntityType.Object);
                    else
                        entities.FreeEntity(dropship.EntityId);
                }
            }
        }

        internal void SpawnCreatures(SpawnPool spawnPool,List<Creature> creatureList)
        {
            var mapChannel = spawnPool.RuntimeMapChannel ??
                MapChannelManager.Instance.FindByContextId(spawnPool.MapContextId);

            foreach (var spawnSlot in creatureList)
            {
                var creature = CreatureManager.Instance.CreateCreature(spawnSlot.DbId, spawnPool);

                if (creature == null)
                    continue;

                try
                {
                    RandomizePosition(creature, creatureList.Count);
                    if (spawnPool.FollowOwnerCharacterId != 0 ||
                        spawnPool.FollowTargetEntityId != 0)
                        BehaviorManager.Instance.SetActionFollow(
                            creature,
                            spawnPool.FollowTargetEntityId);
                    CellManager.Instance.AddToWorld(mapChannel, creature);
                    MissionApplication.Instance.Scenes.ActorAvailable(mapChannel, spawnPool.DbId);
                }
                catch
                {
                    RollBackSpawn(mapChannel, creature);
                    throw;
                }
            }
        }

        private void RollBackSpawn(MapChannel mapChannel, Creature creature)
        {
            var removed = false;
            try
            {
                removed = CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creature);
            }
            catch (Exception exception)
            {
                Logger.WriteLog(LogType.Error, $"Failed to remove rejected spawn {creature.EntityId}: {exception.Message}");
            }
            finally
            {
                if (!removed)
                {
                    if (mapChannel.MapCellInfo?.Cells != null)
                        foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                            cell?.CreatureList?.RemoveAll(candidate => candidate == creature);

                    var entities = EntityManager.Instance;
                    if (entities.RegisteredEntities.ContainsKey(creature.EntityId))
                        entities.ReleaseEntity(creature.EntityId, EntityType.Creature);
                    else
                        entities.FreeEntity(creature.EntityId);
                }
                DecreaseAliveCreatureCount(mapChannel, creature.SpawnPool);
            }
        }

        internal List<Creature> CreateListOfCreatures(SpawnPool spawnPool)
        {
            return spawnPool.QueuedCreatureList ??
                CreateListOfCreatures(spawnPool, CreatureManager.Instance.LoadedCreatures, Random.Shared);
        }

        public static List<Creature> CreateListOfCreatures(SpawnPool spawnPool,
            IReadOnlyDictionary<uint, Creature> definitions, Random random)
        {
            var creatureList = new List<Creature>();

            if (spawnPool.SpawnSlot == null)
                return creatureList;

            foreach (var spawnSlot in spawnPool.SpawnSlot)
            {
                if (creatureList.Count == 64)
                    break;

                if (spawnSlot == null || spawnSlot.CreatureId == 0)
                    continue;

                if (spawnSlot.CountMin < 0 || spawnSlot.CountMax < spawnSlot.CountMin)
                {
                    Logger.WriteLog(LogType.Error, $"SpawnPool {spawnPool.DbId}: invalid counts for creature {spawnSlot.CreatureId}");
                    continue;
                }

                if (spawnSlot.CountMax == 0)
                    continue;

                if (!definitions.TryGetValue(spawnSlot.CreatureId, out var definition) || definition == null)
                {
                    Logger.WriteLog(LogType.Error, $"SpawnPool {spawnPool.DbId}: missing creature {spawnSlot.CreatureId}");
                    continue;
                }

                var spawnCreatureCount = random.Next(spawnSlot.CountMin, spawnSlot.CountMax + 1);

                for (var i = 0; i < spawnCreatureCount && creatureList.Count < 64; i++)
                    creatureList.Add(new Creature(definition));
            }

            return creatureList;
        }

        internal void RandomizePosition(Creature creature, int count)
        {
            var pos = creature.SpawnPool.Position;

            if (count != 1)
            {
                pos.X += new Random().Next() % 5 - 2;
                pos.Z += new Random().Next() % 5 - 2;
            }

            // Spawn pools were placed by hand; on a slope the offset members would hang in the
            // air or start in the ground. With a navmesh they stand on it.
            pos = NavMeshManager.SnapToGround(
                creature.SpawnPool?.RuntimeMapChannel ??
                MapChannelManager.Instance.FindByContextId(creature.SpawnPool.MapContextId),
                pos);

            var map = creature.SpawnPool.RuntimeMapChannel;
            var pose = creature.SpawnPool.ScenePose;
            if (pose != null && map?.IsPrivateInstance == true &&
                pose.OwnerCharacterId == map.OwnerCharacterId && pose.Handle.MapEpoch == map.MissionEpoch)
            {
                var authored = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
                if (Vector3.Distance(pos, authored) >= 0.5f)
                    throw new GameplayRejectionException("Authored recovered pose is not on its spawn's grounded surface.");
                pos = authored;
                creature.Controller.ScriptedMove = new ScriptedMove
                    { Destination = pos, Orientation = pose.Orientation, Arrived = true };
                creature.Controller.CurrentAction = BehaviorManager.BehaviorActionScriptedMove;
                creature.IsRunning = false;
            }
            CreatureManager.Instance.SetLocation(creature, pos, creature.SpawnPool.Rotation, creature.SpawnPool.MapContextId);
        }

        private static SpawnPool CloneSpawnPool(SpawnPool template, MapChannel mapChannel)
        {
            return new SpawnPool
            {
                DbId = template.DbId,
                Position = template.Position,
                Rotation = template.Rotation,
                SpawnSlot = template.SpawnSlot?.Select(slot =>
                    new SpawnPoolSlot(slot.CreatureId, slot.CountMin, slot.CountMax)).ToList() ?? new List<SpawnPoolSlot>(),
                Mode = template.Mode,
                AnimType = template.AnimType,
                MapContextId = mapChannel.MapInfo.MapContextId,
                RuntimeMapChannel = mapChannel,
                SpawnPolicy = template.SpawnPolicy,
                RespawnTime = template.RespawnTime,
                UpdateTimer = template.RespawnTime,
                ScenarioMissionId = template.ScenarioMissionId,
                ScenarioGroupId = template.ScenarioGroupId,
                ScenarioAttemptKey = template.ScenarioAttemptKey,
                ScenarioOwnerCharacterId = template.ScenarioOwnerCharacterId,
                FollowOwnerCharacterId = template.FollowOwnerCharacterId,
                FollowTargetEntityId = template.FollowTargetEntityId
            };
        }
    }
}
