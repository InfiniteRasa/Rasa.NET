using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Database.Tables.World;
    using Structures;

    public class SpawnPoolManager
    {
        private static SpawnPoolManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Dictionary<int, SpawnPool> LoadedSpawnPools = new Dictionary<int, SpawnPool>();

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
                            _instance = new SpawnPoolManager();
                    }
                }

                return _instance;
            }
        }

        private SpawnPoolManager()
        {
        }

        public void DecreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue--;

            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                spawnPool.UpdateTimer = spawnPool.RespawnTime;
        }

        private void DecreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures -= count;
            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                spawnPool.UpdateTimer = spawnPool.RespawnTime;
        }

        public void IncreaseAliveCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures++;
        }

        public void IncreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures++;
        }

        public void IncreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue++;
        }

        public void IncreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures += count;
        }

        public void SpawnPoolInit()
        {
            var spawnPoolList = SpawnPoolTable.LoadSpawnPool();

            foreach (var data in spawnPoolList)
            {
                var spawnPoolSlots = new List<SpawnPoolSlot>();

                if (data.CreatureId1 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId1, data.CreatureMinCount1, data.CreatureMaxCount1));
                if (data.CreatureId2 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId2, data.CreatureMinCount2, data.CreatureMaxCount2));
                if (data.CreatureId3 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId3, data.CreatureMinCount3, data.CreatureMaxCount3));
                if (data.CreatureId4 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId4, data.CreatureMinCount4, data.CreatureMaxCount4));
                if (data.CreatureId5 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId5, data.CreatureMinCount5, data.CreatureMaxCount5));
                if (data.CreatureId6 > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.CreatureId6, data.CreatureMinCount6, data.CreatureMaxCount6));

                var spawnPool = new SpawnPool
                {
                    AnimType = data.AnimType,
                    ContextId = data.ContextId,
                    DbId = data.DbId,
                    HomePosition = new Position(data.PosX, data.PosY, data.PosZ),
                    HomeRotation = new Quaternion(data.Rotation, 0D, 0D, 0D),
                    Mode = data.Mode,
                    RespawnTime = data.RespawnTime * 100,  //convert to ms
                    SpawnSlot = spawnPoolSlots
                };

                LoadedSpawnPools.Add(data.DbId, spawnPool);
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedSpawnPools.Count} SpawnPools");
        }

        public void SpawnPoolWorker(MapChannel mapChannel, long timePassed)
        {
            foreach (var key in LoadedSpawnPools)
            {
                var spawnPool = key.Value;

                if (spawnPool.ContextId != mapChannel.MapInfo.MapContextId)
                    continue; // spawnpool is not for this map

                var totalCreaturesActive = spawnPool.AliveCreatures + spawnPool.QueuedCreatures;

                if (totalCreaturesActive > 0)
                   continue; // there is still active creatures

                spawnPool.UpdateTimer = spawnPool.UpdateTimer + timePassed;

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    continue; // spawnpool is still on cooldown

                //reset timer
                spawnPool.UpdateTimer = 0;

                // create list of creatures to spawn
                var creatureList = new List<Creature>();

                foreach (var spawnSlot in spawnPool.SpawnSlot)
                {
                    var spawnCreatureCount = new Random().Next(spawnSlot.CountMin, spawnSlot.CountMax);

                    for (var i = 0; i < spawnCreatureCount; i++)
                        creatureList.Add(CreatureManager.Instance.FindCreature(spawnSlot.CreatureId));

                    if (creatureList.Count > 63)    // cannot spawn more than 64 creatures at once
                        break;
                }

                if (creatureList.Count == 0)
                    continue; // nothing to spawn

                if (spawnPool.AnimType == 0)    // animType==0; spawn without animation
                {
                    IncreaseQueueCount(spawnPool);
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);

                    foreach (var spawnSlot in creatureList)
                    {
                        var creature = CreatureManager.Instance.CreateCreature(spawnSlot.DbId, spawnPool);

                        if (creature == null)
                            continue;

                        // set ai path if spawnpool has any
                        //if (spawnPool->pathCount > 0)
                        //creature->controller.aiPathFollowing.generalPath = spawnPool->pathList[rand() % spawnPool->pathCount]; // select random path

                        // no random location if we spawn only one creature
                        if (creatureList.Count == 1)
                        {
                            //CreatureManager.Instance.SetLocation(creature, new Position(spawnPool.HomePosition.PosX, spawnPool.HomePosition.PosY, spawnPool.HomePosition.PosZ), new Quaternion(0D, 0D, 0D, 0D));
                            creature.Actor.Position = new Position(spawnPool.HomePosition.PosX, spawnPool.HomePosition.PosY, spawnPool.HomePosition.PosZ);
                            creature.Actor.Rotation = new Quaternion(spawnPool.HomeRotation.X, spawnPool.HomeRotation.Y, spawnPool.HomeRotation.Z, spawnPool.HomeRotation.W);
                        }
                        else
                        {
                            creature.Actor.Position = new Position(spawnPool.HomePosition.PosX + (double)new Random().Next(-10, 10), spawnPool.HomePosition.PosY + (double)new Random().Next(-10, 10), spawnPool.HomePosition.PosZ);
                            creature.Actor.Rotation = new Quaternion(spawnPool.HomeRotation.X, spawnPool.HomeRotation.Y, spawnPool.HomeRotation.Z, spawnPool.HomeRotation.W);
                        }

                        CellManager.Instance.AddToWorld(mapChannel, creature);
                    }

                    DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                    DecreaseQueueCount(spawnPool);
                }
                // animType == 1; bane dropship animation
                else if (spawnPool.AnimType == 1)
                {
                    //banedropship_create(mapChannel, location->x, location->y, location->z, spawnTypeCount, spawnTypeList, spawnPool);
                    Logger.WriteLog(LogType.Error, "spawnPool animType = 1 not supported yet");
                        }
                // animTyp e== 2; human dropship animation
                else if (spawnPool.AnimType == 2)
                {
                    //humandropship_create(mapChannel, location->x, location->y, location->z, spawnTypeCount, spawnTypeList, spawnPool);
                    Logger.WriteLog(LogType.Error, "spawnPool animType = 2 not supported yet");
                }
            }
        }

    }
}
