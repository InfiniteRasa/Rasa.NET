using System;
using System.Collections.Generic;
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

        private SpawnPoolManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
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
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature6Id, data.Creature5MinCount, data.Creature6MaxCount));

                var spawnPool = new SpawnPool
                {
                    AnimType = data.AnimType,
                    MapContextId = data.MapContextId,
                    DbId = data.Id,
                    Position = data.Position,
                    Rotation = (float)data.Rotation,
                    Mode = data.Mode,
                    RespawnTime = data.RespawnTime * 100,  //convert to ms
                    // to spawn all cretures at server start, we set UpdateTimer to RespawnTime
                    UpdateTimer = data.RespawnTime * 100, //convert to ms
                    SpawnSlot = spawnPoolSlots
                };

                LoadedSpawnPools.Add(data.Id, spawnPool);
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedSpawnPools.Count} SpawnPools");
        }

        public void SpawnPoolWorker(MapChannel mapChannel, long timePassed)
        {
            foreach (var key in LoadedSpawnPools)
            {
                var spawnPool = key.Value;

                if (spawnPool.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue; // spawnpool is not for this map

                var totalCreaturesActive = spawnPool.AliveCreatures + spawnPool.QueuedCreatures;

                if (totalCreaturesActive > 0)
                    continue; // there is still active creatures

                spawnPool.UpdateTimer += timePassed;

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    continue; // spawnpool is still on cooldown

                // create list of creatures to spawn
                var creatureList = new List<Creature>();

                foreach (var spawnSlot in spawnPool.SpawnSlot)
                {
                    var spawnCreatureCount = new Random().Next(spawnSlot.CountMin, spawnSlot.CountMax + 1);

                    for (var i = 0; i < spawnCreatureCount; i++)
                        creatureList.Add(new Creature(CreatureManager.Instance.LoadedCreatures[spawnSlot.CreatureId]));

                    if (creatureList.Count > 63)    // cannot spawn more than 64 creatures at once
                        break;
                }

                if (creatureList.Count == 0)
                    continue; // nothing to spawn

                if (spawnPool.AnimType == 0)    // animType==0; spawn without animation
                {
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);

                    foreach (var spawnSlot in creatureList)
                    {
                        var creature = CreatureManager.Instance.CreateCreature(spawnSlot.DbId, spawnPool);

                        if (creature == null)
                            continue;

                        // set ai path if spawnpool has any
                        //if (spawnPool->pathCount > 0)
                        //creature.Controller.aiPathFollowing.generalPath = spawnPool->pathList[rand() % spawnPool->pathCount]; // select random path

                        RandomizePosition(creature, creatureList.Count);

                        CellManager.Instance.AddToWorld(mapChannel, creature);
                    }

                    DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                }
                // animType == 1; bane dropship animation
                else if (spawnPool.AnimType == 1)
                {
                    // create bane_dropship
                    var dropship = new Dropship(Factions.Bane, DropshipType.Spawner, spawnPool);

                    CellManager.Instance.AddToWorld(mapChannel, dropship);

                    DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);

                    IncreaseQueueCount(spawnPool);
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                }
                // animType == 2; human dropship animation
                else if (spawnPool.AnimType == 2)
                {
                    // create human_dropship
                    var dropship = new Dropship(Factions.AFS, DropshipType.Spawner, spawnPool);

                    CellManager.Instance.AddToWorld(mapChannel, dropship);

                    DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);

                    IncreaseQueueCount(spawnPool);
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                }
            }
        }

        internal void RandomizePosition(Creature creature, int count)
        {
            var pos = creature.SpawnPool.Position;

            if (count != 1)
            {
                pos.X += new Random().Next() % 5 - 2;
                pos.Z += new Random().Next() % 5 - 2;
            }

            CreatureManager.Instance.SetLocation(creature, pos, creature.SpawnPool.Rotation, creature.SpawnPool.MapContextId);
        }
    }
}
