using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Managers;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Structures;
    using Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class SpawnPoolLifecycleTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        private static MapChannel CreateMap(uint contextId = 1220)
        {
            return new MapChannel
            {
                MapInfo = new MapInfo(contextId, "Concordia Wilderness", 0, 0),
                ClientList = new()
            };
        }

        private static SpawnPool CreatePool()
        {
            return new SpawnPool
            {
                DbId = 55,
                MapContextId = 1220,
                RespawnTime = 2000,
                SpawnSlot = new List<SpawnPoolSlot>()
            };
        }

        [TestMethod]
        [DataRow(20U, 20000L)]
        [DataRow(uint.MaxValue, 4294967295000L)]
        public void LoaderConvertsPersistedSecondsToMillisecondsWithoutOverflow(uint seconds, long milliseconds)
        {
            var data = new SpawnData();
            data.Pools.Add(new SpawnPoolEntry { Id = 55, MapContextId = 1220, RespawnTime = seconds });
            var manager = new SpawnPoolManager(data);

            manager.SpawnPoolInit();
            var pool = manager.LoadedSpawnPools[55];

            Assert.AreEqual(milliseconds, (long)pool.RespawnTime);
            Assert.AreEqual(milliseconds, pool.UpdateTimer);
            manager.IncreaseAliveCreatureCount(pool);
            manager.DecreaseAliveCreatureCount(CreateMap(), pool);
            manager.SpawnPoolWorker(CreateMap(), milliseconds - 1);
            Assert.AreEqual(milliseconds - 1, pool.UpdateTimer);
            manager.SpawnPoolWorker(CreateMap(), 1);
            Assert.AreEqual(milliseconds, pool.UpdateTimer);
        }

        [TestMethod]
        public void WorkerOnlyAdvancesPoolsOnItsOwnMap()
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(CreateMap(1148), 1000);
            Assert.AreEqual(0L, pool.UpdateTimer);
            manager.SpawnPoolWorker(CreateMap(), 1000);
            Assert.AreEqual(1000L, pool.UpdateTimer);
        }

        [TestMethod]
        [DataRow((short)1, (short)0)]
        [DataRow((short)2, (short)0)]
        [DataRow((short)0, (short)3)]
        [DataRow((short)0, (short)-1)]
        public void AutomaticWorkerDoesNotAdvanceManualModesOrUnsupportedAnimations(short mode, short animation)
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            pool.Mode = mode;
            pool.AnimType = animation;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(CreateMap(), 1000);

            Assert.AreEqual(0L, pool.UpdateTimer);
            Assert.AreEqual(0, pool.DropshipQueue);
            Assert.AreEqual(0, pool.QueuedCreatures);
        }

        [TestMethod]
        [DataRow(1, 0, 0)]
        [DataRow(0, 1, 0)]
        [DataRow(0, 0, 1)]
        public void LiveCreaturesQueuedCreaturesAndDepartingDropshipsBlockRespawn(int alive, int queued, int dropships)
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            pool.AliveCreatures = alive;
            pool.QueuedCreatures = queued;
            pool.DropshipQueue = dropships;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(CreateMap(), 1000);

            Assert.AreEqual(0L, pool.UpdateTimer);
        }

        [TestMethod]
        public void LastDeathStartsCooldownAndCorpsesDoNotDelayTheNextGeneration()
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            manager.IncreaseAliveCreatureCount(pool);
            manager.IncreaseAliveCreatureCount(pool);

            manager.DecreaseAliveCreatureCount(CreateMap(), pool);
            manager.IncreaseDeadCreatureCount(pool);
            Assert.AreEqual(2000L, pool.UpdateTimer);
            manager.DecreaseAliveCreatureCount(CreateMap(), pool);
            manager.IncreaseDeadCreatureCount(pool);
            Assert.AreEqual(0L, pool.UpdateTimer);
            Assert.AreEqual(0, pool.AliveCreatures);
            Assert.AreEqual(2, pool.DeadCreatures);

            manager.SpawnPoolWorker(CreateMap(), 999);
            Assert.AreEqual(999L, pool.UpdateTimer);
            manager.SpawnPoolWorker(CreateMap(), 1001);
            Assert.AreEqual(2000L, pool.UpdateTimer);
            manager.DecreaseDeadCreatureCount(pool);
            manager.DecreaseDeadCreatureCount(pool);
            Assert.AreEqual(0, pool.DeadCreatures);
            Assert.AreEqual(2000L, pool.UpdateTimer);
        }

        [TestMethod]
        public void EmptyPoolsStopAccumulatingTimeOnceTheirConfiguredCooldownExpires()
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(CreateMap(), long.MaxValue);

            Assert.AreEqual(2000L, pool.UpdateTimer);
            Assert.AreEqual(0, pool.QueuedCreatures);
            Assert.AreEqual(0, pool.AliveCreatures);
        }

        [TestMethod]
        public void FailedDeliveryWaitsForTheLastDropshipToDepartBeforeStartingCooldown()
        {
            var manager = new SpawnPoolManager(null);
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            manager.IncreaseQueueCount(pool);
            manager.IncreaseQueuedCreatureCount(pool, 3);

            manager.DecreaseQueuedCreatureCount(pool, 3);
            Assert.AreEqual(2000L, pool.UpdateTimer);
            manager.DecreaseQueueCount(pool);

            Assert.AreEqual(0L, pool.UpdateTimer);
            Assert.AreEqual(0, pool.DropshipQueue);
            Assert.AreEqual(0, pool.QueuedCreatures);
            Assert.AreEqual(0, pool.AliveCreatures);
        }

        [TestMethod]
        [DataRow((short)1)]
        [DataRow((short)2)]
        public void AnimatedWavesDeliverTheOriginallySelectedCreaturesWithoutRerolling(short animation)
        {
            const uint creatureId = uint.MaxValue - 5;
            var manager = new SpawnPoolManager(null);
            var map = CreateMap();
            var pool = CreatePool();
            pool.AnimType = animation;
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.Add(new SpawnPoolSlot(creatureId, 2, 2));
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            var definitions = CreatureManager.Instance.LoadedCreatures;
            definitions.TryGetValue(creatureId, out var previous);
            definitions[creatureId] = new Creature { DbId = creatureId };
            try
            {
                manager.SpawnPoolWorker(map, 0);
                Assert.AreEqual(1, pool.DropshipQueue);
                Assert.AreEqual(2, pool.QueuedCreatures);
                var dropship = DynamicObjectManager.Instance.Dropships.Values.Single(ship => ship.SpawnPool == pool);
                Assert.AreEqual(1220U, dropship.MapContextId);
                Assert.AreEqual(animation == 1 ? Data.Factions.Bane : Data.Factions.AFS, dropship.Faction);

                pool.SpawnSlot[0].CountMin = 64;
                pool.SpawnSlot[0].CountMax = 64;
                var delivery = manager.CreateListOfCreatures(pool);
                Assert.HasCount(2, delivery);

                manager.DecreaseQueuedCreatureCount(pool, delivery.Count);
                manager.SpawnPoolWorker(map, 2000);
                Assert.AreEqual(1, pool.DropshipQueue);
                Assert.AreEqual(0, pool.QueuedCreatures);
                manager.DecreaseQueueCount(pool);
                Assert.AreEqual(0, pool.QueuedCreatures);
                Assert.AreEqual(0, pool.DropshipQueue);
                Assert.AreEqual(0L, pool.UpdateTimer);
                Assert.HasCount(64, manager.CreateListOfCreatures(pool));
            }
            finally
            {
                if (previous == null)
                    definitions.Remove(creatureId);
                else
                    definitions[creatureId] = previous;
                foreach (var dropship in DynamicObjectManager.Instance.Dropships.Values
                    .Where(ship => ship.SpawnPool == pool).ToArray())
                {
                    CellManager.Instance.RemoveFromWorld(map, dropship);
                    DynamicObjectManager.Instance.Dropships.Remove(dropship.EntityId);
                }
            }
        }

        [TestMethod]
        [DataRow((short)1, false)]
        [DataRow((short)1, true)]
        [DataRow((short)2, false)]
        [DataRow((short)2, true)]
        public void FailedAnimatedEnqueueRollsBackAllStateAndAllowsTheNextRetry(short animation, bool failAfterCellInsertion)
        {
            using var world = new WorldTestContext();
            using var runtime = new SpawnRuntime(world.Map, new SpawnData());
            var origin = new Vector3(10, 0, 10);
            var bootstrap = new Creature { Position = origin };
            CellManager.Instance.AddToWorld(world.Map, bootstrap);
            var center = world.Map.MapCellInfo.Cells[bootstrap.Cells[2, 2]];
            CellManager.Instance.RemoveCreatureFromWorld(world.Map, bootstrap);
            var clients = center.ClientList;
            var otherPool = CreatePool();
            otherPool.DbId = 56;
            otherPool.Position = origin;
            var unrelated = new Dropship(Data.Factions.AFS, Data.DropshipType.Spawner, otherPool);
            CellManager.Instance.AddToWorld(world.Map, unrelated);
            DynamicObjectManager.Instance.Dropships.Add(unrelated.EntityId, unrelated);
            runtime.Creatures.LoadedCreatures[3] = new Creature { DbId = 3 };
            var pool = CreatePool();
            pool.AnimType = animation;
            pool.Position = failAfterCellInsertion ? origin : new Vector3(float.NegativeInfinity, 0, float.NegativeInfinity);
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.Add(new SpawnPoolSlot(3, 1, 1));
            var manager = new SpawnPoolManager(null);
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            if (failAfterCellInsertion)
                center.ClientList = null;
            var entities = EntityManager.Instance;
            var workers = DynamicObjectManager.Instance.Dropships;
            var previousRegistrations = entities.RegisteredEntities.Keys.ToArray();
            var previousObjects = entities.DynamicObjects.Keys.ToArray();
            var previousWorkers = workers.Keys.ToArray();
            Exception firstFailure = null;
            var threadId = Environment.CurrentManagedThreadId;
            EventHandler<FirstChanceExceptionEventArgs> capture = (_, args) =>
            {
                if (Environment.CurrentManagedThreadId == threadId &&
                    (args.Exception is NullReferenceException || args.Exception is InvalidDataException))
                    firstFailure ??= args.Exception;
            };
            AppDomain.CurrentDomain.FirstChanceException += capture;
            try
            {
                Exception thrown = failAfterCellInsertion
                    ? Assert.ThrowsExactly<NullReferenceException>(() => manager.SpawnPoolWorker(world.Map, 0))
                    : Assert.ThrowsExactly<InvalidDataException>(() => manager.SpawnPoolWorker(world.Map, 0));

                Assert.AreSame(firstFailure, thrown);
                Assert.AreEqual(0, pool.QueuedCreatures);
                Assert.AreEqual(0, pool.DropshipQueue);
                Assert.AreEqual(0, pool.AliveCreatures);
                Assert.AreEqual(0, pool.DeadCreatures);
                Assert.AreEqual(0L, pool.UpdateTimer);
                Assert.IsNull(pool.QueuedCreatureList);
                CollectionAssert.AreEquivalent(previousRegistrations, entities.RegisteredEntities.Keys.ToArray());
                CollectionAssert.AreEquivalent(previousObjects, entities.DynamicObjects.Keys.ToArray());
                CollectionAssert.AreEquivalent(previousWorkers, workers.Keys.ToArray());
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell =>
                    cell.DynamicObjectList.OfType<Dropship>().Any(ship => ship.SpawnPool == pool)));

                center.ClientList = clients;
                pool.Position = origin;
                manager.SpawnPoolWorker(world.Map, pool.RespawnTime - 1);
                Assert.AreEqual(0, pool.DropshipQueue);
                manager.SpawnPoolWorker(world.Map, 1);

                var retry = workers.Values.Single(ship => ship.SpawnPool == pool);
                Assert.AreEqual(animation == 1 ? Data.Factions.Bane : Data.Factions.AFS, retry.Faction);
                Assert.AreEqual(1, pool.DropshipQueue);
                Assert.AreEqual(1, pool.QueuedCreatures);
                Assert.AreEqual(Data.EntityType.Object, entities.RegisteredEntities[retry.EntityId]);
                Assert.AreSame(retry, entities.DynamicObjects[retry.EntityId]);
                Assert.IsTrue(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.DynamicObjectList.Contains(retry)));
                manager.SpawnPoolWorker(world.Map, pool.RespawnTime);
                Assert.AreEqual(1, workers.Values.Count(ship => ship.SpawnPool == pool));
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= capture;
                center.ClientList = clients;
                foreach (var ship in entities.DynamicObjects.Values.OfType<Dropship>()
                    .Concat(workers.Values).Where(ship => ship.SpawnPool == pool || ship == unrelated).Distinct().ToArray())
                {
                    foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                        cell.DynamicObjectList.RemoveAll(candidate => candidate == ship);
                    if (workers.TryGetValue(ship.EntityId, out var worker) && worker == ship)
                        workers.Remove(ship.EntityId);
                    entities.ReleaseEntity(ship.EntityId, Data.EntityType.Object);
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MissingEntityClassMetadataRejectsTheCreatureWithoutBlockingRespawn(bool nullMetadata)
        {
            const uint creatureId = uint.MaxValue - 5;
            var manager = new SpawnPoolManager(null);
            var map = CreateMap();
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.Add(new SpawnPoolSlot(creatureId, 1, 1));
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            var definitions = CreatureManager.Instance.LoadedCreatures;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var classId = unchecked((Data.EntityClasses)(-1));
            var maps = MapChannelManager.Instance.MapChannelArray;
            definitions.TryGetValue(creatureId, out var previousCreature);
            var hadClass = classes.TryGetValue(classId, out var previousClass);
            maps.TryGetValue(1220, out var previousMap);
            definitions[creatureId] = new Creature
            {
                DbId = creatureId,
                EntityClass = classId
            };
            classes.Remove(classId);
            if (nullMetadata)
                classes[classId] = null;
            maps[1220] = map;
            try
            {
                manager.SpawnPoolWorker(map, 0);

                Assert.AreEqual(0, pool.QueuedCreatures);
                Assert.AreEqual(0, pool.AliveCreatures);
                Assert.AreEqual(0, pool.DropshipQueue);
                Assert.AreEqual(0L, pool.UpdateTimer);
            }
            finally
            {
                if (previousCreature == null)
                    definitions.Remove(creatureId);
                else
                    definitions[creatureId] = previousCreature;
                if (hadClass)
                    classes[classId] = previousClass;
                else
                    classes.Remove(classId);
                if (previousMap == null)
                    maps.Remove(1220);
                else
                    maps[1220] = previousMap;
            }
        }

        [TestMethod]
        public void NullCreatureTemplateIsRejectedBeforeAnyAliveCountIsAdded()
        {
            const uint creatureId = uint.MaxValue - 5;
            var definitions = CreatureManager.Instance.LoadedCreatures;
            var hadTemplate = definitions.TryGetValue(creatureId, out var previous);
            var pool = CreatePool();
            definitions[creatureId] = null;
            try
            {
                Assert.IsNull(CreatureManager.Instance.CreateCreature(creatureId, pool));
                Assert.AreEqual(0, pool.AliveCreatures);
            }
            finally
            {
                if (hadTemplate)
                    definitions[creatureId] = previous;
                else
                    definitions.Remove(creatureId);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RejectedAndNullTemplatesDoNotPreventLaterValidCreaturesFromEnteringTheWorld(bool missingAugmentations)
        {
            using var world = new WorldTestContext();
            using var runtime = new SpawnRuntime(world.Map, new SpawnData());
            var validClass = unchecked((Data.EntityClasses)(-2));
            world.AddClass(validClass);
            EntityClassManager.Instance.LoadedEntityClasses[validClass].Augmentations.Add(Data.AugmentationType.Creature);
            var invalidClass = unchecked((Data.EntityClasses)(-3));
            world.AddClass(invalidClass);
            if (missingAugmentations)
                EntityClassManager.Instance.LoadedEntityClasses[invalidClass].Augmentations = null;
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.AddRange(new[]
            {
                new SpawnPoolSlot(1, 1, 1), new SpawnPoolSlot(2, 1, 1),
                new SpawnPoolSlot(4, 1, 1), new SpawnPoolSlot(3, 2, 2)
            });
            runtime.Creatures.LoadedCreatures[1] = new Creature
            {
                DbId = 1,
                EntityClass = unchecked((Data.EntityClasses)(-1))
            };
            runtime.Creatures.LoadedCreatures[2] = null;
            runtime.Creatures.LoadedCreatures[4] = new Creature { DbId = 4, EntityClass = invalidClass };
            runtime.Creatures.LoadedCreatures[3] = new Creature
            {
                DbId = 3,
                EntityClass = validClass,
                AppearanceData = new()
            };
            var manager = new SpawnPoolManager(null);
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(world.Map, 0);

            var spawned = EntityManager.Instance.Creatures.Values.Where(creature => creature.SpawnPool == pool).ToArray();
            Assert.HasCount(2, spawned);
            Assert.IsTrue(spawned.All(creature => creature.DbId == 3));
            Assert.AreEqual(2, pool.AliveCreatures);
            Assert.AreEqual(0, pool.QueuedCreatures);
            Assert.AreEqual(0, pool.DeadCreatures);
            foreach (var creature in spawned)
                Assert.IsTrue(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature)));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WorldInsertionFailureRollsBackAliveRegistrationAndCellsAndStillThrows(bool failAfterCellInsertion)
        {
            using var world = new WorldTestContext();
            using var runtime = new SpawnRuntime(world.Map, new SpawnData());
            var validClass = unchecked((Data.EntityClasses)(-2));
            world.AddClass(validClass);
            EntityClassManager.Instance.LoadedEntityClasses[validClass].Augmentations.Add(Data.AugmentationType.Creature);
            runtime.Creatures.LoadedCreatures[3] = new Creature
            {
                DbId = 3,
                EntityClass = validClass,
                AppearanceData = new()
            };
            var observer = world.CreateClient();
            CellManager.Instance.AddToWorld(observer);
            var cell = world.Map.MapCellInfo.Cells.Values.First(candidate => candidate.ClientList.Contains(observer));
            var clients = cell.ClientList;
            var creatures = cell.CreatureList;
            if (failAfterCellInsertion)
                cell.ClientList = null;
            else
                cell.CreatureList = null;
            var previousRegistrations = EntityManager.Instance.RegisteredEntities.Keys.ToArray();
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.Add(new SpawnPoolSlot(3, 1, 1));
            var manager = new SpawnPoolManager(null);
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            try
            {
                Assert.ThrowsExactly<NullReferenceException>(() => manager.SpawnPoolWorker(world.Map, 0));

                Assert.AreEqual(0, pool.AliveCreatures);
                Assert.AreEqual(0, pool.QueuedCreatures);
                Assert.AreEqual(0, pool.DeadCreatures);
                Assert.AreEqual(0L, pool.UpdateTimer);
                CollectionAssert.AreEquivalent(previousRegistrations, EntityManager.Instance.RegisteredEntities.Keys.ToArray());
                Assert.IsFalse(EntityManager.Instance.Creatures.Values.Any(creature => creature.SpawnPool == pool));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(candidate =>
                    candidate.CreatureList?.Any(creature => creature.SpawnPool == pool) == true));
            }
            finally
            {
                cell.ClientList = clients;
                cell.CreatureList = creatures;
            }
        }

        [TestMethod]
        public void FailedLaterInsertionKeepsPreviouslySpawnedCreaturesAndReleasesTheWaveReservation()
        {
            using var world = new WorldTestContext();
            var data = new SpawnData();
            using var runtime = new SpawnRuntime(world.Map, data);
            var validClass = unchecked((Data.EntityClasses)(-2));
            world.AddClass(validClass);
            EntityClassManager.Instance.LoadedEntityClasses[validClass].Augmentations.Add(Data.AugmentationType.Creature);
            runtime.Creatures.LoadedCreatures[3] = new Creature { DbId = 3, EntityClass = validClass, AppearanceData = new() };
            var origin = new Vector3(10, 0, 10);
            var bootstrap = new Creature { Position = origin };
            CellManager.Instance.AddToWorld(world.Map, bootstrap);
            var cell = world.Map.MapCellInfo.Cells[bootstrap.Cells[2, 2]];
            CellManager.Instance.RemoveCreatureFromWorld(world.Map, bootstrap);
            var clients = cell.ClientList;
            var reads = 0;
            data.ReadStats = _ =>
            {
                if (++reads == 2)
                    cell.ClientList = null;
            };
            var pool = CreatePool();
            pool.Position = origin;
            pool.UpdateTimer = pool.RespawnTime;
            pool.SpawnSlot.Add(new SpawnPoolSlot(3, 2, 2));
            var manager = new SpawnPoolManager(null);
            manager.LoadedSpawnPools.Add(pool.DbId, pool);
            try
            {
                Assert.ThrowsExactly<NullReferenceException>(() => manager.SpawnPoolWorker(world.Map, 0));

                var remaining = EntityManager.Instance.Creatures.Values.Where(creature => creature.SpawnPool == pool).ToArray();
                Assert.HasCount(1, remaining);
                Assert.AreEqual(1, pool.AliveCreatures);
                Assert.AreEqual(0, pool.QueuedCreatures);
                Assert.AreEqual(0, pool.DeadCreatures);
                Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer);
                Assert.AreEqual(1, world.Map.MapCellInfo.Cells.Values.Sum(candidate =>
                    candidate.CreatureList.Count(creature => creature.SpawnPool == pool)));
            }
            finally
            {
                cell.ClientList = clients;
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DuplicateMembershipsAdvanceEachDistinctCreatureOnlyOnce(bool dead)
        {
            using var world = new WorldTestContext();
            var pool = CreatePool();
            var first = CreateThinkingCreature(pool, new Vector3(10, 0, 10), dead);
            var second = CreateThinkingCreature(pool, new Vector3(15, 0, 10), dead);
            CellManager.Instance.AddToWorld(world.Map, first);
            CellManager.Instance.AddToWorld(world.Map, second);
            var center = world.Map.MapCellInfo.Cells[first.Cells[2, 2]];
            var other = world.Map.MapCellInfo.Cells.Values.First(cell => cell != center);
            center.CreatureList.Add(first);
            other.CreatureList.Add(first);
            other.CreatureList.Add(second);
            try
            {
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);

                Assert.AreEqual(250L, first.LastAgression);
                Assert.AreEqual(250L, second.LastAgression);
                Assert.AreEqual(dead ? 250L : 0L, first.Controller.DeadTime);
                Assert.AreEqual(dead ? 250L : 0L, second.Controller.DeadTime);
                Assert.AreEqual(dead ? 2 : 0, pool.DeadCreatures);
                Assert.AreEqual(dead ? 0 : 2, pool.AliveCreatures);
            }
            finally
            {
                foreach (var creature in new[] { first, second })
                    if (CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature) && !dead)
                        SpawnPoolManager.Instance.DecreaseAliveCreatureCount(world.Map, pool);
            }
        }

        [TestMethod]
        public void BehaviorWorkerExpiresADuplicatedCorpseAtTwentySecondsAndCountsRemovalOnce()
        {
            using var world = new WorldTestContext();
            var pool = CreatePool();
            pool.UpdateTimer = pool.RespawnTime;
            var corpse = CreateThinkingCreature(pool, new Vector3(10, 0, 10), true);
            corpse.Controller.DeadTime = 19500;
            CellManager.Instance.AddToWorld(world.Map, corpse);
            var center = world.Map.MapCellInfo.Cells[corpse.Cells[2, 2]];
            var other = world.Map.MapCellInfo.Cells.Values.First(cell => cell != center);
            center.CreatureList.Add(corpse);
            other.CreatureList.Add(corpse);
            try
            {
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                Assert.AreEqual(19750L, corpse.Controller.DeadTime);
                Assert.AreEqual(1, pool.DeadCreatures);
                Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(corpse.EntityId));

                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                Assert.AreEqual(20000L, corpse.Controller.DeadTime);
                Assert.AreEqual(0, pool.DeadCreatures);
                Assert.AreEqual(0, pool.AliveCreatures);
                Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer);
                Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(corpse.EntityId));
                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(corpse.EntityId));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(corpse)));

                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                Assert.IsFalse(CellManager.Instance.RemoveCreatureFromWorld(world.Map, corpse));
                Assert.AreEqual(0, pool.DeadCreatures);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, corpse);
            }
        }

        private static Creature CreateThinkingCreature(SpawnPool pool, Vector3 position, bool dead)
        {
            var creature = new Creature
            {
                DbId = 47,
                Position = position,
                SpawnPool = pool,
                State = dead ? Data.CharacterState.Dead : Data.CharacterState.Idle
            };
            creature.Attributes.Add(Data.Attributes.Health,
                new ActorAttributes(Data.Attributes.Health, 100, 100, dead ? 0 : 100, 0, 1000));
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            if (dead)
                SpawnPoolManager.Instance.IncreaseDeadCreatureCount(pool);
            else
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(pool);
            return creature;
        }

        private sealed class SpawnRuntime : IDisposable
        {
            private readonly FieldInfo _singleton = typeof(CreatureManager).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            private readonly CreatureManager _previousCreatures;
            private readonly MapChannel _map;
            private readonly MapChannel _previousMap;
            private readonly HashSet<ulong> _previousCreatureIds;
            internal CreatureManager Creatures { get; }

            internal SpawnRuntime(MapChannel map, IGameUnitOfWorkFactory data)
            {
                _map = map;
                _previousCreatureIds = EntityManager.Instance.Creatures.Keys.ToHashSet();
                _previousCreatures = CreatureManager.Instance;
                // Supply only the database boundary to the legacy private singleton constructor.
                Creatures = (CreatureManager)Activator.CreateInstance(typeof(CreatureManager),
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { data }, null);
                _singleton.SetValue(null, Creatures);
                MapChannelManager.Instance.MapChannelArray.TryGetValue(map.MapInfo.MapContextId, out _previousMap);
                MapChannelManager.Instance.MapChannelArray[map.MapInfo.MapContextId] = map;
            }

            public void Dispose()
            {
                foreach (var creature in EntityManager.Instance.Creatures.Values
                    .Where(creature => !_previousCreatureIds.Contains(creature.EntityId)).ToArray())
                    CellManager.Instance.RemoveCreatureFromWorld(_map, creature);
                _singleton.SetValue(null, _previousCreatures);
                if (_previousMap == null)
                    MapChannelManager.Instance.MapChannelArray.Remove(_map.MapInfo.MapContextId);
                else
                    MapChannelManager.Instance.MapChannelArray[_map.MapInfo.MapContextId] = _previousMap;
            }
        }

        private sealed class SpawnData : IGameUnitOfWorkFactory, IWorldUnitOfWork, ISpawnpoolRepository, ICreatureRepository
        {
            public List<SpawnPoolEntry> Pools { get; } = new();
            public Action<uint> ReadStats { get; set; }
            public ISpawnpoolRepository Spawnpools => this;
            public IWorldUnitOfWork CreateWorld() => this;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public List<SpawnPoolEntry> Get() => Pools;
            public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction() => throw new NotSupportedException();
            public void Complete() => throw new NotSupportedException();
            public void Reject() => throw new NotSupportedException();
            public void Dispose() { }
            public IActionRepository Actions => throw new NotSupportedException();
            public IEquipmentRepository Equipment => throw new NotSupportedException();
            public ICreatureRepository Creatures => this;
            public IEntityClassRepository EntityClasses => throw new NotSupportedException();
            public IFootlockerRepository Footlockers => throw new NotSupportedException();
            public ILogosRepository Logoses => throw new NotSupportedException();
            public IMapInfoRepository MapInfos => throw new NotSupportedException();
            public IMapLinkRepository MapLinks => throw new NotSupportedException();
            public IKraftwerksRepository Kraftwerks => throw new NotSupportedException();
            public IMapRegionRepository MapRegions => throw new NotSupportedException();
            public IMapMarkerRepository MapMarkers => throw new NotSupportedException();
            public IRecipeRepository Recipes => throw new NotSupportedException();
            public INpcMissionRepository NpcMissions => throw new NotSupportedException();
            public INpcMissionRewardRepository NpcMissionRewards => throw new NotSupportedException();
            public IMissionContentRepository MissionContent => throw new NotSupportedException();
            public INpcPackageRepository NpcPackages => throw new NotSupportedException();
            public IPlayerRandomNameRepository RandomNames => throw new NotSupportedException();
            public ITeleporterRepository Teleporters => throw new NotSupportedException();
            List<CreatureEntry> ICreatureRepository.Get() => throw new NotSupportedException();
            public List<CreatureClassFlagEntry> GetClassFlags() => new();
            public CreatureStatEntry GetCreatureStats(uint creatureId)
            {
                ReadStats?.Invoke(creatureId);
                return null;
            }
            public CreatureActionEntry GetCreatureActionById(uint id) => throw new NotSupportedException();
            public Dictionary<uint, CreatureActionEntry> GetCreatureActions() => throw new NotSupportedException();
            public void CreateOrUpdateAppearance(uint dbId, uint slotId, uint classId, uint hue) => throw new NotSupportedException();
            public List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId) => throw new NotSupportedException();
            public List<VendorItemEntry> GetVendorItems() => throw new NotSupportedException();
            public List<VendorEntry> GetVendors() => throw new NotSupportedException();
        }
    }
}
