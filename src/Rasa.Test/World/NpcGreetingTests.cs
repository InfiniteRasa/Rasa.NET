using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
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
                        NpcGreetingSeed.Rows.Concat(NpcGreetingSeed.Marked).Select(row => (row.CreatureId, row.GreetingId)).ToList(),
                        rows.Select(row => (row.Id, row.GreetingId)).ToList());

                    // The client does not say which were marked. One is, with its line, by footage.
                    CollectionAssert.AreEqual(new[] { (510002u, 488u) }, NpcGreetingSeed.Marked.ToList(), "Brigadier General Beacham");
                    Assert.IsFalse(NpcGreetingSeed.Rows.Any(row => row.CreatureId == 510002), "he had no line before");
                    Assert.IsTrue(NpcGreetings.IsLine(488));
                    CollectionAssert.AreEqual(new[] { (510002u, 488u) },
                        rows.Where(row => row.Important).Select(row => (row.Id, row.GreetingId)).ToList());

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

                    // And its mark: on a row that has a line, and kept when the line is changed.
                    Assert.IsTrue(repository.SaveNpcGreetingImportant(99, true));
                    Assert.IsFalse(repository.SaveNpcGreetingImportant(4242, true), "no line, nothing to mark");
                    repository.SaveNpcGreeting(99, 466);
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var repository = new CreatureRepository(context);
                    var rows = repository.GetNpcGreetings().ToDictionary(row => row.Id, row => row.GreetingId);

                    Assert.AreEqual(84, rows.Count, "one added, two changed");
                    Assert.AreEqual(146u, rows[101]);
                    Assert.AreEqual(19u, rows[100]);
                    Assert.AreEqual(466u, rows[99]);
                    CollectionAssert.AreEquivalent(new[] { 99u, 510002u }, repository.GetNpcGreetings().Where(row => row.Important).Select(row => row.Id).ToList());

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
                        NpcGreetingSeed.Rows.Concat(NpcGreetingSeed.Marked).Select(row => (row.CreatureId, row.GreetingId)).ToList(),
                        world.Creatures.GetNpcGreetings().Select(row => (row.Id, row.GreetingId)).ToList());
            }

            using var migrated = WildernessRuntimeTestHarness.Create();

            Assert.AreEqual(1620, NpcGreetings.For(migrated.Creatures.LoadedCreatures[Rogers]));
            Assert.IsFalse(NpcGreetings.IsImportant(migrated.Creatures.LoadedCreatures[Rogers]));

            // Brigadier General Beacham, Alia Das: his line and its mark come with the column.
            Assert.AreEqual(488, NpcGreetings.For(migrated.Creatures.LoadedCreatures[510002]));
            Assert.IsTrue(NpcGreetings.IsImportant(migrated.Creatures.LoadedCreatures[510002]));
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

        [TestMethod]
        public void AnNpcWhoseLineIsMarkedImportantHasTheSpeechBubbleAndSaysItAsTheImportantGreeting()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var idle = context.AddNpc(9078);
            var npcs = new NpcManager(context, context.Manager);

            NPCConversationStatusPacket Told(Creature npc)
            {
                context.Drain();
                npcs.UpdateConversationStatus(context.Client, npc);

                return context.Drain().OfType<NPCConversationStatusPacket>().Single();
            }

            ConversationStatus Status(Creature npc) => Told(npc).ConvoStatusId;

            Dictionary<ConversationType, object> Converse(Creature npc)
            {
                context.Drain();
                npcs.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });

                var packet = context.Drain().OfType<ConversePacket>().Single();
                Assert.IsTrue(MissionTestContext.Encode(packet).Length > 0, "one the packet will write");

                return packet.ConvoDataDict;
            }

            Assert.AreEqual(12, (int)ConversationStatus.ImportantGreeting, "CONVO_STATUS_IMPORTANT_GREETING");
            Assert.AreEqual(12, (int)ConversationType.ImportantGreering, "CONVO_TYPE_IMPORTANT_GREETING");

            // A mark and no line: nothing to say, and nothing over its head.
            idle.Npc.GreetingImportant = true;
            Assert.IsFalse(NpcGreetings.IsImportant(idle));
            Assert.IsFalse(NpcGreetings.IsImportant(null));
            Assert.IsFalse(NpcGreetings.IsImportant(new Creature()), "a creature that is no NPC");
            Assert.AreEqual(ConversationStatus.None, Status(idle));

            // With a line: the status the client draws the speech bubble for, and the line alone.
            idle.Npc.GreetingId = 488;
            Assert.IsTrue(NpcGreetings.IsImportant(idle));
            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(idle));
            Assert.AreEqual(0, Told(idle).Data.Count, "the client reads no data with it");

            var said = Converse(idle);
            Assert.AreEqual(1, said.Count);
            Assert.AreEqual(488, said[ConversationType.ImportantGreering]);

            // Not marked, it is as it was: no bubble, and the plain greeting.
            idle.Npc.GreetingImportant = false;
            Assert.AreEqual(ConversationStatus.Greeting, Status(idle));

            var plain = Converse(idle);
            Assert.AreEqual(1, plain.Count);
            Assert.AreEqual(488, plain[ConversationType.Greeting]);

            // Anything else it has for the player comes first: a shop's icon and a shop's window.
            idle.Npc.GreetingImportant = true;
            idle.Npc.Vendor = new Vendor(586);
            Assert.AreEqual(ConversationStatus.Vending, Status(idle));

            var shop = Converse(idle);
            Assert.IsTrue(shop.ContainsKey(ConversationType.Vending));
            Assert.IsFalse(shop.ContainsKey(ConversationType.ImportantGreering), "npc.py would show it in place of the shop");
            Assert.IsFalse(shop.ContainsKey(ConversationType.Greeting));

            // So does a mission: its pip, and straight to it.
            var giver = context.AddNpc(77);
            giver.Npc.GreetingId = 488;
            giver.Npc.GreetingImportant = true;
            Assert.AreEqual(ConversationStatus.Available, Status(giver));

            var offer = Converse(giver);
            Assert.IsTrue(offer.ContainsKey(ConversationType.MissionDispense));
            Assert.IsFalse(offer.ContainsKey(ConversationType.ImportantGreering));
            Assert.IsFalse(offer.ContainsKey(ConversationType.Greeting));

            // And a topic list is headed by the line as a plain greeting, which is the one the
            // client's list reads.
            giver.Npc.Vendor = new Vendor(586);

            var listed = Converse(giver);
            Assert.AreEqual(488, listed[ConversationType.Greeting]);
            Assert.IsFalse(listed.ContainsKey(ConversationType.ImportantGreering));
        }

        [TestMethod]
        public void AGameMastersMarkIsKeptWithTheLineAndGoesWithIt()
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

                Assert.IsFalse(NpcGreetings.SetImportant(npc, true, factory), "no line of its own to mark");
                Assert.IsFalse(NpcGreetings.SetImportant(new Creature { DbId = 5 }, true, factory), "no NPC");
                Assert.IsFalse(NpcGreetings.SetImportant(null, true, factory));
                Assert.AreEqual(0, Marks(database).Count);

                Assert.IsTrue(NpcGreetings.Set(npc, 488, factory));
                Assert.AreEqual((4242u, 488u, false), Marks(database).Single(), "a line starts plain");
                Assert.IsFalse(NpcGreetings.IsImportant(npc));

                Assert.IsTrue(NpcGreetings.SetImportant(npc, true, factory));
                Assert.IsTrue(npc.Npc.GreetingImportant);
                Assert.IsTrue(NpcGreetings.IsImportant(npc));
                Assert.AreEqual((4242u, 488u, true), Marks(database).Single());

                Assert.IsTrue(NpcGreetings.Set(npc, 1620, factory), "another line");
                Assert.AreEqual((4242u, 1620u, true), Marks(database).Single(), "and the mark stays");
                Assert.IsTrue(NpcGreetings.IsImportant(npc));

                Assert.IsTrue(NpcGreetings.SetImportant(npc, false, factory));
                Assert.IsFalse(npc.Npc.GreetingImportant);
                Assert.AreEqual((4242u, 1620u, false), Marks(database).Single());

                // Taken away with the line, and a line given afterwards is plain.
                Assert.IsTrue(NpcGreetings.SetImportant(npc, true, factory));
                Assert.IsTrue(NpcGreetings.Clear(npc, factory));
                Assert.IsFalse(npc.Npc.GreetingImportant);
                Assert.AreEqual(0, Marks(database).Count);

                Assert.IsTrue(NpcGreetings.Set(npc, 146, factory));
                Assert.AreEqual((4242u, 146u, false), Marks(database).Single());
                Assert.IsFalse(NpcGreetings.IsImportant(npc));

                // A row that has gone from under it: not marked, and the NPC is left as it was.
                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    Assert.IsTrue(new CreatureRepository(context).DeleteNpcGreeting(4242));

                Assert.IsFalse(NpcGreetings.SetImportant(npc, true, factory));
                Assert.IsFalse(npc.Npc.GreetingImportant);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheGreetingCommandMarksTheTargetedNpcsLineAndThePlayersWhoSeeItAreTold()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var instance = typeof(NpcManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var managerBefore = instance.GetValue(null);
            var factoryBefore = Rasa.Game.Server.GameUnitOfWorkFactory;

            try
            {
                var database = Path.Combine(directory, "world");

                using (var world = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    world.Database.EnsureCreated();

                using var context = MissionTestContext.WithDefinitions(321);
                var idle = context.AddNpc(9078);
                var npcs = new NpcManager(context, context.Manager);
                var commands = new ChatCommandsManager(npcs);

                commands.RegisterChatCommands();
                instance.SetValue(null, npcs);
                Rasa.Game.Server.GameUnitOfWorkFactory = new WorldFactory(database);
                context.Client.AccountEntry.Level = (byte)GmLevel.GameMaster;
                context.Client.Player.Target = idle.EntityId;

                List<PythonPacket> Run(string command)
                {
                    context.Drain();
                    commands.ProcessCommand(context.Client, command);

                    return context.Drain();
                }

                string Said(List<PythonPacket> packets) =>
                    string.Join(" | ", packets.OfType<SystemMessagePacket>().Select(message => message.TextMessage));

                ConversationStatus Told(List<PythonPacket> packets) =>
                    packets.OfType<NPCConversationStatusPacket>().Single().ConvoStatusId;

                // Nothing to mark until it has a line of its own.
                var none = Run(".greeting important");
                StringAssert.Contains(Said(none), "no greeting of its own to mark");
                Assert.AreEqual(0, none.OfType<NPCConversationStatusPacket>().Count());
                Assert.AreEqual(0, Marks(database).Count);

                Assert.AreEqual(ConversationStatus.Greeting, Told(Run(".greeting 488")));
                StringAssert.Contains(Said(Run(".greeting")), "says greeting 488.");

                // Marked: the player looking at it has the bubble at once.
                var marked = Run(".greeting important");
                StringAssert.Contains(Said(marked), "is marked important: the speech bubble");
                Assert.AreEqual(ConversationStatus.ImportantGreeting, Told(marked));
                Assert.IsTrue(idle.Npc.GreetingImportant);
                Assert.AreEqual((9078u, 488u, true), Marks(database).Single());

                StringAssert.Contains(Said(Run(".greeting")), "says greeting 488, marked important.");

                var again = Run(".greeting important on");
                StringAssert.Contains(Said(again), "marked important already");
                Assert.AreEqual(0, again.OfType<NPCConversationStatusPacket>().Count());

                // Another line keeps the mark.
                var changed = Run(".greeting 1620");
                StringAssert.Contains(Said(changed), "says greeting 1620, marked important as its last was.");
                Assert.AreEqual(ConversationStatus.ImportantGreeting, Told(changed));
                Assert.AreEqual((9078u, 1620u, true), Marks(database).Single());

                // Plain again.
                var plain = Run(".greeting important off");
                StringAssert.Contains(Said(plain), "is plain again");
                Assert.AreEqual(ConversationStatus.Greeting, Told(plain));
                Assert.AreEqual((9078u, 1620u, false), Marks(database).Single());
                StringAssert.Contains(Said(Run(".greeting important off")), "is not marked important");

                // Anything else after the word is not understood, and nothing is changed.
                foreach (var command in new[] { ".greeting important maybe", ".greeting important on now", ".greeting 488 important" })
                {
                    StringAssert.Contains(Said(Run(command)), "usage: .greeting", command);
                    Assert.AreEqual((9078u, 1620u, false), Marks(database).Single(), command);
                }

                // The mark goes with the line.
                Run(".greeting important");
                Assert.AreEqual(ConversationStatus.None, Told(Run(".greeting clear")));
                Assert.IsFalse(idle.Npc.GreetingImportant);
                Assert.AreEqual(0, Marks(database).Count);
                Assert.AreEqual(ConversationStatus.Greeting, Told(Run(".greeting 146")), "and a line given afterwards is plain");

                // No NPC targeted.
                context.Client.Player.Target = 0;
                StringAssert.Contains(Said(Run(".greeting important")), "Target an NPC");
            }
            finally
            {
                instance.SetValue(null, managerBefore);
                Rasa.Game.Server.GameUnitOfWorkFactory = factoryBefore;
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AWorldStoppedBeforeTheMarkHasItsLinesUnmarkedAndTheColumnComesWithTheGeneralsLine()
        {
            // Outpost Commander Rogers, Alia Das: 1620 in the seed. Brigadier General Beacham,
            // Alia Das: no line until the column's migration, which gives him 488, marked.
            const uint Rogers = 100;
            const uint Beacham = 510002;
            const string Before = "20261128000000_Add_loot_groups";

            var seeded = NpcGreetingSeed.Rows.Select(row => (row.CreatureId, row.GreetingId)).ToList();

            // The table and no column for the mark yet: the harness starts on it, as the
            // Wilderness tests that stop a World between the two migrations do, and every NPC
            // has its line, not marked.
            using var stopped = WildernessRuntimeTestHarness.Create(targetWorldMigration: Before);

            List<(uint, uint, bool)> Lines()
            {
                using var world = stopped.CreateWorld();

                return world.Creatures.GetNpcGreetings().Select(row => (row.Id, row.GreetingId, row.Important)).ToList();
            }

            void Down() => stopped.World.GetService<IMigrator>().Migrate(Before);

            CollectionAssert.AreEquivalent(seeded.Select(row => (row.CreatureId, row.GreetingId, false)).ToList(), Lines());
            Assert.AreEqual(1620, NpcGreetings.For(stopped.Creatures.LoadedCreatures[Rogers]));
            Assert.IsFalse(NpcGreetings.IsImportant(stopped.Creatures.LoadedCreatures[Rogers]));
            Assert.IsFalse(NpcGreetings.HasOwn(stopped.Creatures.LoadedCreatures[Beacham]));

            // Up: every line is kept, none of them marked, and the General has his, marked.
            stopped.World.Initialize();

            CollectionAssert.AreEquivalent(
                seeded.Select(row => (row.CreatureId, row.GreetingId, false)).Append((Beacham, 488u, true)).ToList(), Lines());

            using (var world = stopped.CreateWorld())
                Assert.IsTrue(world.Creatures.SaveNpcGreetingImportant(Rogers, true));

            // Down: the column goes and the lines stay, the General's with them.
            Down();

            CollectionAssert.AreEquivalent(
                seeded.Select(row => (row.CreatureId, row.GreetingId, false)).Append((Beacham, 488u, false)).ToList(), Lines());

            // Up again over a line he has already - one given by hand before the column was
            // there: it is the line the mark is for, and he is marked. Nobody else is.
            stopped.World.Initialize();

            CollectionAssert.AreEqual(new[] { (Beacham, 488u, true) }, Lines().Where(row => row.Item3).ToList());
            Assert.AreEqual(83, Lines().Count);

            // And over another line a game master gave him: it is left as it is, not marked.
            Down();
            stopped.World.Database.ExecuteSqlRaw("update npc_greeting set greeting_id = 1620 where id = 510002");
            stopped.World.Initialize();

            Assert.AreEqual((Beacham, 1620u, false), Lines().Single(row => row.Item1 == Beacham));
            Assert.IsFalse(Lines().Any(row => row.Item3));
            Assert.AreEqual(83, Lines().Count);
        }

        private static List<(uint, uint, bool)> Marks(string database)
        {
            using var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

            return new CreatureRepository(context).GetNpcGreetings().Select(row => (row.Id, row.GreetingId, row.Important)).ToList();
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
