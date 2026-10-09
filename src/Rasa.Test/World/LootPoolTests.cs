using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Api;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// Loot pools (LootPools): how a kill rolls them, what a creature that has them drops, and
    /// the three tables with the stores gametools' endpoints read and write them through - the
    /// pools', and the creature flags'.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LootPoolTests
    {
        private const string Migration = "20261128000000_Add_loot_groups";
        private const string Before = "20261127000000_Add_creature_battlecries";

        #region The roll

        [TestMethod]
        public void EveryItemOfEveryPoolIsRolledOnItsOwnAtItsChance()
        {
            var pools = new[]
            {
                new LootPool(1, "Junk", "", new[]
                {
                    new LootPoolItem(10, 100, 2, 2),
                    new LootPoolItem(11, 0, 1, 1),
                    new LootPoolItem(12, 25, 1, 1),
                    new LootPoolItem(13, 0.0001, 1, 1)
                }),
                new LootPool(2, "Gear", "", new[] { new LootPoolItem(10, 50, 3, 7) })
            };

            // A roll is a number out of a million: 25% is the first 250 000 of them.
            var asked = new List<(int, int)>();
            var rolls = new Queue<int>(new[] { 249999, 0, 499999, 7 });
            var drops = LootPools.Roll(pools, (minimum, maximum) =>
            {
                asked.Add((minimum, maximum));
                return rolls.Dequeue();
            });

            CollectionAssert.AreEqual(new (uint, uint)[] { (10, 2), (12, 1), (13, 1), (10, 7) }, drops,
                "A certain item, one that came up at the edge of its chance, the rarest there is, and the same item again from another pool.");
            CollectionAssert.AreEqual(new[] { (0, 1000000), (0, 1000000), (0, 1000000), (3, 8) }, asked,
                "Nothing is rolled for what is certain or impossible, nor for a quantity that is one number.");

            // One past the edge of each chance, nothing but the certain item.
            rolls = new Queue<int>(new[] { 250000, 1, 500000 });
            CollectionAssert.AreEqual(new (uint, uint)[] { (10, 2) }, LootPools.Roll(pools, (_, _) => rolls.Dequeue()));

            Assert.IsEmpty(LootPools.Roll(null, (_, _) => 0));
            Assert.IsEmpty(LootPools.Roll(new[] { new LootPool(3, "Empty", "", null) }, (_, _) => 0));
        }

        [TestMethod]
        public void ASetIsMadeOfTheTablesRowsAndIsTidiedAsItIsMade()
        {
            var set = LootPools.FromRows(
                new[]
                {
                    new LootGroupEntry { Id = 2, Name = "Second", Comment = "note" },
                    new LootGroupEntry { Id = 1, Name = "First", Comment = "" }
                },
                new[]
                {
                    new LootGroupItemEntry { GroupId = 1, ItemTemplateId = 30, Chance = 12.345678, MinQuantity = 2, MaxQuantity = 4 },
                    new LootGroupItemEntry { GroupId = 1, ItemTemplateId = 20, Chance = 250, MinQuantity = 0, MaxQuantity = 0 },
                    new LootGroupItemEntry { GroupId = 2, ItemTemplateId = 20, Chance = -3, MinQuantity = 5, MaxQuantity = 2 },
                    new LootGroupItemEntry { GroupId = 9, ItemTemplateId = 20, Chance = 5, MinQuantity = 1, MaxQuantity = 1 }
                },
                new[]
                {
                    new CreatureLootGroupEntry { CreatureId = 700, GroupId = 2 },
                    new CreatureLootGroupEntry { CreatureId = 700, GroupId = 1 },
                    new CreatureLootGroupEntry { CreatureId = 701, GroupId = 9 }
                });

            CollectionAssert.AreEqual(new uint[] { 1, 2 }, set.Pools.Select(pool => pool.Id).ToArray());
            Assert.AreEqual("note", set.Pools[1].Note);
            Assert.AreEqual(3, set.ItemCount, "A row of a pool there is not is nobody's.");

            var first = set.Pools[0].Items;

            Assert.AreEqual((20u, 100d, 1u, 1u), (first[0].TemplateId, first[0].Chance, first[0].Minimum, first[0].Maximum), "No more than certain, and at least one.");
            Assert.AreEqual((30u, 12.3457, 2u, 4u), (first[1].TemplateId, first[1].Chance, first[1].Minimum, first[1].Maximum));
            Assert.AreEqual((0d, 5u, 5u), (set.Pools[1].Items[0].Chance, set.Pools[1].Items[0].Minimum, set.Pools[1].Items[0].Maximum));

            CollectionAssert.AreEqual(new uint[] { 1, 2 }, set.For(700).Select(pool => pool.Id).ToArray());
            Assert.IsNull(set.For(701), "A creature given a pool there is not has none.");
            Assert.IsNull(set.For(5));
            Assert.AreEqual(1, set.CreatureCount);
            CollectionAssert.AreEqual(new (uint, uint)[] { (700, 1), (700, 2) }, set.Assignments.ToArray());

            // A row somebody wrote by hand, with numbers the API would never take: as much
            // as the table's limit and no more, and a kill that rolls it is a kill like another.
            var wild = LootPools.FromRows(
                new[] { new LootGroupEntry { Id = 1, Name = "By hand" } },
                new[]
                {
                    new LootGroupItemEntry { GroupId = 1, ItemTemplateId = 20, Chance = 100, MinQuantity = 3, MaxQuantity = uint.MaxValue },
                    new LootGroupItemEntry { GroupId = 1, ItemTemplateId = 21, Chance = 100, MinQuantity = uint.MaxValue, MaxQuantity = int.MaxValue }
                },
                null).Pools[0].Items;

            Assert.AreEqual((3u, LootGroupItemEntry.QuantityLimit), (wild[0].Minimum, wild[0].Maximum));
            Assert.AreEqual((LootGroupItemEntry.QuantityLimit, LootGroupItemEntry.QuantityLimit), (wild[1].Minimum, wild[1].Maximum));

            var random = new Random(7);
            var rolled = LootPools.Roll(new[] { new LootPool(1, "By hand", "", wild) }, random.Next);

            Assert.HasCount(2, rolled);
            Assert.IsTrue(rolled[0].Quantity >= 3 && rolled[0].Quantity <= LootGroupItemEntry.QuantityLimit);
            Assert.AreEqual(LootGroupItemEntry.QuantityLimit, rolled[1].Quantity);

            Assert.AreEqual(0d, LootPools.TidyChance(double.NaN));
            Assert.AreEqual(100d, LootPools.TidyChance(double.PositiveInfinity));
            Assert.AreEqual(0.0001, LootPools.TidyChance(0.00006), "To the nearest ten-thousandth of a percent, as the editor rounds it.");
            Assert.AreEqual(0d, LootPools.TidyChance(0.00004));
        }

        #endregion

        #region A kill

        [TestMethod]
        public void ACreatureWithPoolsDropsWhatTheyRollWithTheCreditsEveryCorpseHas()
        {
            using var context = MissionTestContext.WithDefinitions();
            var before = LootPools.Current;

            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3148);
            context.AddRewardTemplate(30, 3149);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)3149].ItemClassInfo.StackSize = 5;
            ItemManager.Instance.GetItemTemplateById(29).QualityId = (int)LootQuality.Epic;
            ItemManager.Instance.GetItemTemplateById(30).QualityId = (int)LootQuality.Normal;
            context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            // Every roll its lowest: every chance above nothing comes up, with its least quantity.
            var manager = new LootDispenserManager(context, lootRoll: (minimum, maximum) => minimum);

            try
            {
                LootPools.Use(new LootPoolSet(
                    new[]
                    {
                        new LootPool(1, "Salves", "", new[]
                        {
                            new LootPoolItem(29, 10, 2, 4),
                            new LootPoolItem(30, 100, 9, 9),
                            new LootPoolItem(31, 100, 1, 1),
                            new LootPoolItem(28, 0, 1, 1)
                        })
                    },
                    new (uint, uint)[] { (3, 1) }));

                var corpse = Corpse(context, 3);

                manager.Loot(context.Client, corpse);

                var loot = context.Map.LootDispensers[corpse.CorpseLootEntityId];

                CollectionAssert.AreEquivalent(new uint[] { 29, 30 }, loot.LootItems.Select(item => item.ItemTemplateId).ToArray(),
                    "What came up, less the item the server has not got; and none of the cartridges a corpse with no pool has.");
                Assert.AreEqual(2u, loot.LootItems.Single(item => item.ItemTemplateId == 29).ItemQuantity);
                Assert.AreEqual(5u, loot.LootItems.Single(item => item.ItemTemplateId == 30).ItemQuantity, "A stack is the most a row gives.");
                Assert.AreEqual(1, loot.Credits, "The credits every corpse has.");
                Assert.AreEqual(LootQuality.Epic, loot.LootQuality, "The corpse shows the best of what is on it.");

                foreach (var item in loot.LootItems)
                {
                    Assert.AreNotEqual(0u, item.Item.Id, "Made and stored as it is rolled, so the corpse window has an entity behind the row.");
                    Assert.AreSame(item.Item, EntityManager.Instance.GetItem(item.Item.EntityId));
                }

                // Another creature row has no pool, and is not this code's to give loot to.
                Assert.IsNull(LootPools.Current.For(4));

                // A pool whose every chance fails gives the credits and no more.
                LootPools.Use(new LootPoolSet(
                    new[] { new LootPool(1, "Unlucky", "", new[] { new LootPoolItem(29, 0, 1, 1) }) },
                    new (uint, uint)[] { (5, 1) }));

                var bare = Corpse(context, 5);

                manager.Loot(context.Client, bare);

                var nothing = context.Map.LootDispensers[bare.CorpseLootEntityId];

                Assert.IsEmpty(nothing.LootItems);
                Assert.AreEqual(1, nothing.Credits);
                Assert.AreEqual(LootQuality.Junk, nothing.LootQuality);
            }
            finally
            {
                LootPools.Use(before);
                manager.RemoveForOwner(context.Map, context.Client);
            }
        }

        private static Creature Corpse(MissionTestContext context, uint creatureId)
        {
            var corpse = context.AddNpc(creatureId);

            corpse.Npc = null;
            corpse.TargetCategory = TargetCategory.Hostile;
            corpse.State = CharacterState.Dead;
            corpse.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
            CellManager.Instance.UpdateVisibility(context.Client);

            return corpse;
        }

        #endregion

        #region The tables and the stores

        [TestMethod]
        public void TheTablesAreMadeEmptyAndTakenAwayAgainAndAreTheSameOnMySql()
        {
            using var world = new WorldDatabase();

            using (var context = world.Open())
            {
                var repository = new LootGroupRepository(context);

                Assert.IsEmpty(repository.GetGroups(), "No creature drops anything new until pools are given.");
                Assert.IsEmpty(repository.GetItems());
                Assert.IsEmpty(repository.GetCreatureGroups());

                var migrator = context.GetService<IMigrator>();

                migrator.Migrate(Before);

                foreach (var table in new[] { "loot_group", "loot_group_item", "creature_loot_group" })
                    Assert.AreEqual(0, context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}", table)
                        .AsEnumerable().Single(), table);

                migrator.Migrate(Migration);
                Assert.IsEmpty(repository.GetGroups());
            }

            using var mysql = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var script = mysql.GetService<IMigrator>();
            var up = script.GenerateScript(Before, Migration);
            var down = script.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `loot_group`");
            StringAssert.Contains(up, "`name` varchar(100) NOT NULL");
            StringAssert.Contains(up, "`comment` varchar(200) NOT NULL");
            StringAssert.Contains(up, "CREATE TABLE `loot_group_item`");
            StringAssert.Contains(up, "`chance` double NOT NULL");
            StringAssert.Contains(up, "PRIMARY KEY (`group_id`, `item_template_id`)");
            StringAssert.Contains(up, "CREATE TABLE `creature_loot_group`");
            StringAssert.Contains(up, "PRIMARY KEY (`creature_id`, `group_id`)");
            StringAssert.Contains(up, $"'{Migration}'");
            Assert.IsFalse(up.Contains("INSERT INTO `loot_group"), "The tables ship empty.");

            foreach (var table in new[] { "creature_loot_group", "loot_group_item", "loot_group" })
                StringAssert.Contains(down, $"DROP TABLE `{table}`");
        }

        [TestMethod]
        public void ThePoolsAreReplacedWholeOrNotAtAll()
        {
            using var world = new WorldDatabase();

            LootGroupEntry Group(uint id, string name) => new LootGroupEntry { Id = id, Name = name, Comment = "" };
            LootGroupItemEntry Item(uint group, uint template, double chance) =>
                new LootGroupItemEntry { GroupId = group, ItemTemplateId = template, Chance = chance, MinQuantity = 1, MaxQuantity = 2 };
            CreatureLootGroupEntry Link(uint creature, uint group) => new CreatureLootGroupEntry { CreatureId = creature, GroupId = group };

            using (var context = world.Open())
                new LootGroupRepository(context).ReplaceAll(
                    new[] { Group(1, "One"), Group(2, "Two") },
                    new[] { Item(1, 28, 5), Item(1, 29, 0.0001), Item(2, 28, 100) },
                    new[] { Link(118, 1), Link(118, 2), Link(119, 2) });

            using (var context = world.Open())
            {
                var repository = new LootGroupRepository(context);

                CollectionAssert.AreEqual(new[] { "One", "Two" }, repository.GetGroups().Select(row => row.Name).ToArray());
                CollectionAssert.AreEqual(new[] { (1u, 28u, 5d), (1u, 29u, 0.0001), (2u, 28u, 100d) },
                    repository.GetItems().Select(row => (row.GroupId, row.ItemTemplateId, row.Chance)).ToArray());
                Assert.HasCount(3, repository.GetCreatureGroups());

                // What is sent next is all there is afterwards.
                repository.ReplaceAll(new[] { Group(7, "Seven") }, new[] { Item(7, 30, 1) }, new[] { Link(121, 7) });

                CollectionAssert.AreEqual(new uint[] { 7 }, repository.GetGroups().Select(row => row.Id).ToArray());
                CollectionAssert.AreEqual(new uint[] { 30 }, repository.GetItems().Select(row => row.ItemTemplateId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 121 }, repository.GetCreatureGroups().Select(row => row.CreatureId).ToArray());
            }

            // A set the database will not take - the same item twice in a pool - leaves what was there.
            using (var context = world.Open())
            {
                var refused = false;

                try
                {
                    new LootGroupRepository(context).ReplaceAll(new[] { Group(8, "Eight") }, new[] { Item(8, 30, 1), Item(8, 30, 2) }, Array.Empty<CreatureLootGroupEntry>());
                }
                catch (Exception error) when (error is DbUpdateException || error is InvalidOperationException)
                {
                    refused = true;
                }

                Assert.IsTrue(refused);
            }

            using (var context = world.Open())
                CollectionAssert.AreEqual(new uint[] { 7 }, new LootGroupRepository(context).GetGroups().Select(row => row.Id).ToArray());
        }

        [TestMethod]
        public void TheLootPoolStoreChecksASetAgainstTheServersDataWritesItAndPutsItInForce()
        {
            using var world = new WorldDatabase();
            var before = LootPools.Current;

            // Templates 28 and 29 are the server's, with stacks of 100 and 1; creature rows 118 and 119 are in the world.
            var store = new LootPoolStore(world, template => template == 28 ? 100u : template == 29 ? 1u : (uint?)null);

            try
            {
                Assert.IsEmpty(store.Read().Pools);

                var unknown = store.Replace(new LootPoolSet(
                    new[] { new LootPool(1, "Bad", "", new[] { new LootPoolItem(28, 5, 1, 1), new LootPoolItem(4040404, 5, 1, 1) }) },
                    new (uint, uint)[] { (118, 1), (4000000, 1) }));

                Assert.IsFalse(unknown.Done);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "group 1 (Bad): item template 4040404 is not one the server has",
                        "creature 4000000 is not a creature row the server has"
                    },
                    unknown.Problems);
                Assert.IsEmpty(store.Read().Pools, "Refused, nothing is written.");
                Assert.AreSame(before, LootPools.Current, "And nothing is put in force.");

                var set = new LootPoolSet(
                    new[]
                    {
                        new LootPool(1, "Officers", "what an officer carries", new[] { new LootPoolItem(28, 12.5, 1, 250), new LootPoolItem(29, 100, 1, 1) }),
                        new LootPool(2, "Nothing yet", "", null)
                    },
                    new (uint, uint)[] { (118, 1), (119, 1), (119, 2) });
                var done = store.Replace(set);

                Assert.IsTrue(done.Done, string.Join("; ", done.Problems));
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "group 1 (Officers): item 28 asks for up to 250, and a stack is 100; a stack is the most it gives",
                        "group 2 (Nothing yet) has no items"
                    },
                    done.Warnings);
                Assert.AreSame(set, LootPools.Current, "The next kill rolls it.");

                var read = store.Read();

                CollectionAssert.AreEqual(new[] { "Officers", "Nothing yet" }, read.Pools.Select(pool => pool.Name).ToArray());
                Assert.AreEqual("what an officer carries", read.Pools[0].Note);
                Assert.AreEqual((28u, 12.5, 1u, 250u), (read.Pools[0].Items[0].TemplateId, read.Pools[0].Items[0].Chance, read.Pools[0].Items[0].Minimum, read.Pools[0].Items[0].Maximum));
                CollectionAssert.AreEqual(new (uint, uint)[] { (118, 1), (119, 1), (119, 2) }, read.Assignments.ToArray());

                // As the server finds them when it next starts.
                LootPools.Use(LootPoolSet.Empty);
                LootPools.Init(world);
                Assert.AreEqual(2, LootPools.Current.Pools.Count);
                Assert.HasCount(2, LootPools.Current.For(119));

                // An empty set is no pools.
                Assert.IsTrue(store.Replace(LootPoolSet.Empty).Done);
                Assert.IsEmpty(store.Read().Pools);
                Assert.IsNull(LootPools.Current.For(119));
            }
            finally
            {
                LootPools.Use(before);
            }
        }

        [TestMethod]
        public void TheMonsterFlagStoreGivesClassesTheirFlagsInTheDatabaseAndInTheRunningServer()
        {
            using var world = new WorldDatabase();

            // Three of the world's classes as the server has them loaded: two creatures' and one that is not.
            var amoeboid = Class(6032, "Bane_Amoeboid_v1", AugmentationType.Creature);
            var creepa = Class(10215, "Ambient_Cavern_Creepa", AugmentationType.Creature);
            var crate = Class(3814, "A_Control_Point", AugmentationType.Door);
            var loaded = new Dictionary<EntityClasses, EntityClass>
            {
                [(EntityClasses)6032] = amoeboid,
                [(EntityClasses)10215] = creepa,
                [(EntityClasses)3814] = crate
            };

            List<uint> Stored(uint classId)
            {
                using var context = world.Open();

                return context.CreatureClassFlagEntries.AsNoTracking().Where(row => row.ClassId == classId).Select(row => row.FlagId).OrderBy(flag => flag).ToList();
            }

            uint another;

            using (var context = world.Open())
                another = context.CreatureClassFlagEntries.AsNoTracking().Where(row => row.ClassId != 6032 && row.ClassId != 10215).Select(row => row.ClassId).First();

            var seeded = Stored(6032);
            var untouched = Stored(another);

            CollectionAssert.AreEqual(new uint[] { 5, 71 }, seeded, "As the world is seeded: biological, and an Amoeboid.");
            Assert.IsNotEmpty(untouched);

            amoeboid.CreatureFlags = seeded.Select(flag => (CreatureFlag)flag).ToList();

            var store = new MonsterFlagStore(world, () => loaded);
            var read = store.Read();

            CollectionAssert.AreEqual(new uint[] { 6032, 10215 }, read.Select(entry => entry.ClassId).ToArray(), "The creature classes, by id.");
            Assert.AreEqual("Bane_Amoeboid_v1", read[0].ClassName);
            CollectionAssert.AreEqual(new uint[] { 5, 71 }, read[0].Flags.ToArray());
            Assert.IsEmpty(read[1].Flags, "A class with no flags is listed with none.");

            // What will not do: a class the server has not got, one that is no creature's, a flag there is not.
            var refused = store.Replace(new[]
            {
                new MonsterFlags(6032, "", new uint[] { 5, 71, 999 }),
                new MonsterFlags(3814, "", new uint[] { 5 }),
                new MonsterFlags(424242, "", new uint[] { 5 })
            });

            CollectionAssert.AreEqual(
                new[]
                {
                    "class 6032: 999 is not a creature flag",
                    "class 3814 (A_Control_Point) is not a creature's",
                    "class 424242 is not a class the server has"
                },
                refused.Problems);
            CollectionAssert.AreEqual(seeded, Stored(6032), "Refused, nothing is written.");

            // Fire hurts it, and the Creepa is an animal.
            var before = amoeboid.CreatureFlags;
            var done = store.Replace(new[]
            {
                new MonsterFlags(6032, "", new uint[] { 5, 71, 142 }),
                new MonsterFlags(10215, "", new uint[] { 1 })
            });

            Assert.IsTrue(done.Done, string.Join("; ", done.Problems));
            CollectionAssert.AreEqual(new uint[] { 5, 71, 142 }, Stored(6032));
            CollectionAssert.AreEqual(new uint[] { 1 }, Stored(10215));
            CollectionAssert.AreEqual(untouched, Stored(another), "A class not named keeps its own.");

            CollectionAssert.AreEqual(new[] { CreatureFlag.Biological, (CreatureFlag)71, CreatureFlag.VulnerableFire }, amoeboid.CreatureFlags);
            CollectionAssert.AreEqual(new[] { CreatureFlag.KingdomAnimal }, creepa.CreatureFlags);
            Assert.AreNotSame(before, amoeboid.CreatureFlags, "A new list in place of the old, which whoever was reading it still has whole.");
            Assert.HasCount(2, before);

            // No flags at all is something a class can be given.
            Assert.IsTrue(store.Replace(new[] { new MonsterFlags(10215, "", null) }).Done);
            Assert.IsEmpty(Stored(10215));
            Assert.IsEmpty(creepa.CreatureFlags);
            CollectionAssert.AreEqual(new uint[] { 5, 71, 142 }, store.Read()[0].Flags.ToArray());
        }

        private static EntityClass Class(uint id, string name, AugmentationType augmentation) =>
            new EntityClass(id, name, 0, 0, new List<AugmentationType> { augmentation }, true);

        #endregion

        #region Fixture

        /// <summary>A Sqlite world of the test's own, migrated, handing out the two repositories the stores use.</summary>
        private sealed class WorldDatabase : IGameUnitOfWorkFactory, IDisposable
        {
            private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            private readonly string _database;

            internal WorldDatabase()
            {
                if (Logger.Config == null)
                    Logger.UpdateConfig(new Logger.LoggerConfig());

                Directory.CreateDirectory(_directory);
                _database = Path.Combine(_directory, "world");

                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), _database);

                MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
            }

            internal WorldContext Open() => (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), _database);

            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();

            public IWorldUnitOfWork CreateWorld() => new Unit(Open());

            public void Dispose()
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                try { Directory.Delete(_directory, true); } catch (IOException) { }
            }

            private sealed class Unit : IWorldUnitOfWork
            {
                private readonly WorldContext _context;

                internal Unit(WorldContext context)
                {
                    _context = context;
                    Creatures = new CreatureRepository(context);
                    LootGroups = new LootGroupRepository(context);
                }

                public ICreatureRepository Creatures { get; }
                public ILootGroupRepository LootGroups { get; }
                public IActionRepository Actions => null;
                public IEquipmentRepository Equipment => null;
                public IEntityClassRepository EntityClasses => null;
                public IFootlockerRepository Footlockers => null;
                public ILogosRepository Logoses => null;
                public IMapInfoRepository MapInfos => null;
                public IMapLinkRepository MapLinks => null;
                public IKraftwerksRepository Kraftwerks => null;
                public IMapRegionRepository MapRegions => null;
                public IMapMarkerRepository MapMarkers => null;
                public IMapEmitterRepository MapEmitters => null;
                public ISpawnPoolArrivalRepository SpawnPoolArrivals => null;
                public IRecipeRepository Recipes => null;
                public INpcMissionRepository NpcMissions => null;
                public INpcMissionRewardRepository NpcMissionRewards => null;
                public IMissionContentRepository MissionContent => null;
                public INpcPackageRepository NpcPackages => null;
                public IPlayerRandomNameRepository RandomNames => null;
                public ISpawnpoolRepository Spawnpools => null;
                public ITeleporterRepository Teleporters => null;
                public void Complete() { }
                public void Reject() { }
                public IDbContextTransaction BeginTransaction() => throw new NotSupportedException();
                public void Dispose() => _context.Dispose();
            }
        }

        #endregion
    }
}
