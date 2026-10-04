using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

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

        private static Dictionary<uint, MissionInfo> Offers(params uint[] missionIds) =>
            missionIds.ToDictionary(id => id, id => new MissionInfo { MissionConstantData = new() { Level = 1, GroupType = 1 } });
    }
}
