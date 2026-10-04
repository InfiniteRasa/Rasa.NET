using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    // The greeting an NPC's conversation is sent with (NpcGreetings): the client heads its topic
    // list with it, and prints "ERROR: 7: No greeting" there if it was sent none.
    [TestClass]
    [DoNotParallelize]
    public class NpcGreetingTests
    {
        [TestMethod]
        public void TheTopicsAreCountedAsTheClientCountsThem()
        {
            Assert.AreEqual(0, NpcGreetings.Topics(null));
            Assert.AreEqual(0, NpcGreetings.Topics(new Dictionary<ConversationType, object>()));

            Assert.AreEqual(2, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.MissionDispense] = Offers(321, 322) }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.MissionComplete] = new Dictionary<uint, RewardInfo> { [321] = new() } }));
            Assert.AreEqual(2, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.MissionReminder] = new List<uint> { 321, 322 } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.MissionReward] = new List<RewardableMissions> { new(321, new()) } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.ObjectiveComplete] = new List<CompleteableObjectives> { new(321, 1, 1) } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.ObjectiveChoice] = new List<ChoiceObjectives> { new(321, 1, 1) } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.ObjectiveAmbient] = new List<AmbientObjectives> { new(321, 1, 1) } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.Vending] = new List<uint> { 586 } }));
            Assert.AreEqual(1, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.Auctioneer] = true }));
            Assert.AreEqual(0, NpcGreetings.Topics(new Dictionary<ConversationType, object> { [ConversationType.Auctioneer] = false }));

            // What the client opens from the NPC's status, and what is no topic, are not counted.
            Assert.AreEqual(0, NpcGreetings.Topics(new Dictionary<ConversationType, object>
            {
                [ConversationType.Training] = new TrainingConverse(true, 1),
                [ConversationType.Clan] = true,
                [ConversationType.Greeting] = 19,
                [ConversationType.EndConversation] = true,
                [ConversationType.ForceTopic] = new ForceTopic(ConversationType.MissionReward, 321)
            }));
        }

        [TestMethod]
        public void AConversationOfMoreThanOneTopicIsGivenTheGreeting()
        {
            var twoMissions = new Dictionary<ConversationType, object> { [ConversationType.MissionDispense] = Offers(321, 322) };

            Assert.IsTrue(NpcGreetings.AddTo(twoMissions, new Creature()));
            Assert.AreEqual(NpcGreetings.Default, twoMissions[ConversationType.Greeting]);
            Assert.AreEqual(93, NpcGreetings.Default, "npcgreetinglanguage 93: \"Greetings.\"");

            var missionAndShop = new Dictionary<ConversationType, object>
            {
                [ConversationType.MissionDispense] = Offers(321),
                [ConversationType.Vending] = new List<uint> { 586 }
            };

            Assert.IsTrue(NpcGreetings.AddTo(missionAndShop, null));
            Assert.AreEqual(93, missionAndShop[ConversationType.Greeting]);

            var shopAndAuction = new Dictionary<ConversationType, object>
            {
                [ConversationType.Vending] = new List<uint> { 586 },
                [ConversationType.Auctioneer] = true
            };

            Assert.IsTrue(NpcGreetings.AddTo(shopAndAuction, null));

            // One that has a greeting keeps it.
            var greeted = new Dictionary<ConversationType, object>
            {
                [ConversationType.MissionDispense] = Offers(321, 322),
                [ConversationType.Greeting] = 19
            };

            Assert.IsFalse(NpcGreetings.AddTo(greeted, null));
            Assert.AreEqual(19, greeted[ConversationType.Greeting]);
        }

        [TestMethod]
        public void AConversationTheClientOpensNoTopicListForIsLeftAsItIs()
        {
            var conversations = new[]
            {
                new Dictionary<ConversationType, object>(),
                new Dictionary<ConversationType, object> { [ConversationType.MissionDispense] = Offers(321) },
                new Dictionary<ConversationType, object> { [ConversationType.Vending] = new List<uint> { 586 } },
                new Dictionary<ConversationType, object> { [ConversationType.EndConversation] = true },
                new Dictionary<ConversationType, object>
                {
                    [ConversationType.MissionDispense] = Offers(321),
                    [ConversationType.Training] = new TrainingConverse(true, 1),
                    [ConversationType.Clan] = true
                }
            };

            foreach (var conversation in conversations)
            {
                var count = conversation.Count;

                Assert.IsFalse(NpcGreetings.AddTo(conversation, null));
                Assert.AreEqual(count, conversation.Count);
                Assert.IsFalse(conversation.ContainsKey(ConversationType.Greeting));
            }

            Assert.IsFalse(NpcGreetings.AddTo(null, null));
        }

        [TestMethod]
        public void AnNpcWithAMissionAndAShopIsSentWithItsGreeting()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            var npcs = new NpcManager(context, context.Manager);

            // The mission alone: straight to it, and no greeting.
            context.Drain();
            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = giver.EntityId });

            var single = context.Drain().OfType<ConversePacket>().Single();

            Assert.IsTrue(single.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
            Assert.IsFalse(single.ConvoDataDict.ContainsKey(ConversationType.Greeting));

            // With a shop as well the client lists the two, under the greeting.
            giver.Npc.Vendor = new Vendor(586);
            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = giver.EntityId });

            var listed = context.Drain().OfType<ConversePacket>().Single();

            Assert.AreEqual(NpcGreetings.Default, listed.ConvoDataDict[ConversationType.Greeting]);
            Assert.IsTrue(listed.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
            Assert.IsTrue(listed.ConvoDataDict.ContainsKey(ConversationType.Vending));
            Assert.IsTrue(MissionTestContext.Encode(listed).Length > 0, "and it is one the packet will write");
        }

        [TestMethod]
        public void AnNpcWithALineOfItsOwnSaysItInPlaceOfTheDefault()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var giver = context.AddNpc(77);
            var npcs = new NpcManager(context, context.Manager);

            Assert.IsFalse(NpcGreetings.HasOwn(giver));
            Assert.AreEqual(NpcGreetings.Default, NpcGreetings.For(giver));
            Assert.IsFalse(NpcGreetings.HasOwn(null));
            Assert.IsFalse(NpcGreetings.HasOwn(new Creature()), "a creature that is no NPC");

            giver.Npc.GreetingId = 1620;
            giver.Npc.Vendor = new Vendor(586);

            Assert.IsTrue(NpcGreetings.HasOwn(giver));
            Assert.AreEqual(1620, NpcGreetings.For(giver));

            context.Drain();
            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = giver.EntityId });

            Assert.AreEqual(1620, context.Drain().OfType<ConversePacket>().Single().ConvoDataDict[ConversationType.Greeting]);
        }

        [TestMethod]
        public void AnNpcWithALineOfItsOwnAndNothingElseCanBeSpokenToAndSaysIt()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var idle = context.AddNpc(9078);
            var npcs = new NpcManager(context, context.Manager);

            // No line of its own: nothing to say, and not to be spoken to, as before.
            context.Drain();
            npcs.UpdateConversationStatus(context.Client, idle);
            Assert.AreEqual(ConversationStatus.None, context.Drain().OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);

            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = idle.EntityId });

            var nothing = context.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
            Assert.IsTrue(nothing.ContainsKey(ConversationType.EndConversation));
            Assert.IsFalse(nothing.ContainsKey(ConversationType.Greeting));

            // With one: the client's greeting status, and the line alone.
            idle.Npc.GreetingId = 146;
            npcs.UpdateConversationStatus(context.Client, idle);

            var status = context.Drain().OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.Greeting, status.ConvoStatusId);
            Assert.AreEqual(11, (int)status.ConvoStatusId, "CONVO_STATUS_GREETING");
            Assert.AreEqual(0, status.Data.Count);

            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = idle.EntityId });

            var packet = context.Drain().OfType<ConversePacket>().Single();
            Assert.AreEqual(1, packet.ConvoDataDict.Count);
            Assert.AreEqual(146, packet.ConvoDataDict[ConversationType.Greeting]);
            Assert.IsTrue(MissionTestContext.Encode(packet).Length > 0);

            // Anything else it has to offer comes first: a shop is a shop.
            idle.Npc.Vendor = new Vendor(586);
            npcs.UpdateConversationStatus(context.Client, idle);
            Assert.AreEqual(ConversationStatus.Vending, context.Drain().OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);

            npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = idle.EntityId });

            var shop = context.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
            Assert.IsTrue(shop.ContainsKey(ConversationType.Vending));
            Assert.IsFalse(shop.ContainsKey(ConversationType.Greeting), "one topic: straight to it");
        }

        [TestMethod]
        public void OnlyALineTheClientHasIsALine()
        {
            foreach (var id in new uint[] { 1, 3, 93, 360, 362, 436, 440, 571, 574, 600, 603, 1270, 1272, 1277, 1279, 1282, 1620, 1710, 10000001, 10000002, 20000003, 20000006, 20000008 })
                Assert.IsTrue(NpcGreetings.IsLine(id), id.ToString());

            foreach (var id in new uint[] { 0, 2, 361, 437, 439, 572, 573, 601, 602, 1271, 1273, 1278, 1281, 1711, 10000000, 10000003, 20000004, 20000009 })
                Assert.IsFalse(NpcGreetings.IsLine(id), id.ToString());

            Assert.IsTrue(NpcGreetings.IsLine((uint)NpcGreetings.Default));
        }

        [TestMethod]
        public void TheSeedIsOfNpcsTheWorldHasAndLinesTheClientHas()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                Assert.AreEqual(82, NpcGreetingSeed.Rows.Count);
                Assert.AreEqual(82, NpcGreetingSeed.Rows.Select(row => row.CreatureId).Distinct().Count(), "one line an NPC");
                Assert.IsTrue(NpcGreetingSeed.Rows.All(row => NpcGreetings.IsLine(row.GreetingId)));
                Assert.IsTrue(NpcGreetingSeed.Rows.Contains((100u, 1620u)), "Outpost Commander Rogers");

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var repository = new CreatureRepository(context);
                    var rows = repository.GetNpcGreetings();

                    CollectionAssert.AreEquivalent(
                        NpcGreetingSeed.Rows.Select(row => (row.CreatureId, row.GreetingId)).ToList(),
                        rows.Select(row => (row.Id, row.GreetingId)).ToList());

                    // Every one is a creature row of a class with the NPC augmentation (52).
                    var creatures = repository.Get().ToDictionary(creature => creature.Id);
                    var classes = context.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.AugList ?? "");

                    foreach (var row in rows)
                    {
                        Assert.IsTrue(creatures.TryGetValue(row.Id, out var creature), $"creature {row.Id}");
                        Assert.IsTrue(classes[creature.ClassId].Split(',').Select(aug => aug.Trim()).Contains("52"), $"creature {row.Id} is an NPC");
                    }

                    // A game master's line: added, changed, and taken away.
                    repository.SaveNpcGreeting(101, 146);
                    repository.SaveNpcGreeting(100, 19);
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var repository = new CreatureRepository(context);
                    var rows = repository.GetNpcGreetings().ToDictionary(row => row.Id, row => row.GreetingId);

                    Assert.AreEqual(83, rows.Count, "one added, one changed");
                    Assert.AreEqual(146u, rows[101]);
                    Assert.AreEqual(19u, rows[100]);

                    Assert.IsTrue(repository.DeleteNpcGreeting(101));
                    Assert.IsFalse(repository.DeleteNpcGreeting(101), "it has none now");
                    Assert.IsFalse(repository.GetNpcGreetings().Any(row => row.Id == 101));
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AWorldStoppedBeforeTheGreetingTableStartsWithNoGreetingsAndAMigratedOneWithTheSeed()
        {
            // Outpost Commander Rogers, Alia Das: 1620 in the seed.
            const uint Rogers = 100;

            // The Wilderness tests stop a World at an earlier migration, where there is no
            // npc_greeting table yet (WildernessRuntimeTestHarness.CreaturesOfThisWorld): Game
            // starts on it, and nobody has a line of their own.
            using (var stopped = WildernessRuntimeTestHarness.Create(targetWorldMigration: "20261104000200_WildernessAliaOpening"))
            {
                using (var world = stopped.CreateWorld())
                    Assert.AreEqual(0, world.Creatures.GetNpcGreetings().Count);

                Assert.IsFalse(NpcGreetings.HasOwn(stopped.Creatures.LoadedCreatures[Rogers]));
                Assert.AreEqual(NpcGreetings.Default, NpcGreetings.For(stopped.Creatures.LoadedCreatures[Rogers]));

                // Migrated to the end, the same World has the table and the seed in it.
                stopped.World.Initialize();

                using (var world = stopped.CreateWorld())
                    CollectionAssert.AreEquivalent(
                        NpcGreetingSeed.Rows.Select(row => (row.CreatureId, row.GreetingId)).ToList(),
                        world.Creatures.GetNpcGreetings().Select(row => (row.Id, row.GreetingId)).ToList());
            }

            using var migrated = WildernessRuntimeTestHarness.Create();

            Assert.AreEqual(1620, NpcGreetings.For(migrated.Creatures.LoadedCreatures[Rogers]));
        }

        [TestMethod]
        public void AGameMastersLineIsKeptForTheCreatureRowAndTakenAwayAgain()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    context.Database.EnsureCreated();

                var factory = new WorldFactory(database);
                var npc = new Creature { DbId = 4242, Npc = new Npc() };

                Assert.IsFalse(NpcGreetings.Set(npc, 2, factory), "the client has no such line");
                Assert.IsFalse(NpcGreetings.Set(new Creature { DbId = 5 }, 146, factory), "no NPC");
                Assert.IsFalse(NpcGreetings.Clear(npc, factory), "it has none");

                Assert.IsTrue(NpcGreetings.Set(npc, 146, factory));
                Assert.AreEqual(146u, npc.Npc.GreetingId);
                Assert.AreEqual(146, NpcGreetings.For(npc));
                Assert.AreEqual((4242u, 146u), Rows(database).Single());

                Assert.IsTrue(NpcGreetings.Set(npc, 1620, factory), "changed");
                Assert.AreEqual((4242u, 1620u), Rows(database).Single());

                Assert.IsTrue(NpcGreetings.Clear(npc, factory));
                Assert.AreEqual(0u, npc.Npc.GreetingId);
                Assert.AreEqual(NpcGreetings.Default, NpcGreetings.For(npc));
                Assert.AreEqual(0, Rows(database).Count);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        private static List<(uint, uint)> Rows(string database)
        {
            using var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

            return new CreatureRepository(context).GetNpcGreetings().Select(row => (row.Id, row.GreetingId)).ToList();
        }

        /// <summary>A unit of work factory over a world database of the schema alone: the creatures.</summary>
        private sealed class WorldFactory : IGameUnitOfWorkFactory
        {
            private readonly string _database;

            public WorldFactory(string database)
            {
                _database = database;
            }

            public ICharUnitOfWork CreateChar() => throw new InvalidOperationException("Unexpected character database access.");

            public IWorldUnitOfWork CreateWorld()
            {
                var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), _database);

                return DepartureFailureTests.StrictProxy.Create<IWorldUnitOfWork>(new Dictionary<string, object>
                {
                    ["get_Creatures"] = new CreatureRepository(context)
                }, "Dispose");
            }
        }

        private static Dictionary<uint, MissionInfo> Offers(params uint[] missionIds) =>
            missionIds.ToDictionary(id => id, id => new MissionInfo { MissionConstantData = new() { Level = 1, GroupType = 1 } });
    }
}
