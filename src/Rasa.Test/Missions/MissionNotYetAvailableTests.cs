using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    // ConversationStatus.Unavailable: the icon over a giver whose mission the player cannot take
    // yet. Bootcamp's chain is the content: Corporal DeSimone gives Capture the Flag (1994), which
    // wants Gearing Up for Battle (1992) completed; Captain Youngblood gives Calling for
    // Reinforcements (1995), which wants 1994, and its retry (2005), which wants 1995 failed.
    [TestClass]
    [DoNotParallelize]
    public class MissionNotYetAvailableTests
    {
        [TestMethod]
        public void AGiverWhoseMissionIsAheadOfThePlayerShowsItUnavailableUntilItIsNot()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var deSimone = Npc(harness, BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            var npcs = new NpcManager(harness.Context, harness.Manager);

            // A recruit who has done nothing: 1994 waits on 1992.
            CollectionAssert.AreEqual(new uint[] { 1994 }, Waiting(harness, deSimone));

            var before = Status(harness, npcs, deSimone);
            Assert.AreEqual(ConversationStatus.Unavailable, before.ConvoStatusId);
            CollectionAssert.AreEqual(new uint[] { 1994 }, before.Data);

            // 1992 done: the same giver now offers it, and nothing waits.
            Complete(harness, 1990, 1992);

            Assert.AreEqual(0, Waiting(harness, deSimone).Length);
            var after = Status(harness, npcs, deSimone);
            Assert.AreEqual(ConversationStatus.Available, after.ConvoStatusId);
            CollectionAssert.AreEqual(new uint[] { 1994 }, after.Data);
        }

        [TestMethod]
        public void AMissionThatWantsAnotherFailedIsNotWaitedFor()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            // Youngblood is put on the map by the scenario, not by a spawn.
            var youngblood = harness.AddNpc(BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId, 2561);

            // 1995 is ahead; the retry is not - it is for the player who fails 1995.
            CollectionAssert.AreEqual(new uint[] { 1995 }, Waiting(harness, youngblood));

            Complete(harness, 1990, 1992, 1994);

            Assert.AreEqual(0, Waiting(harness, youngblood).Length, "1995 is offered, and the retry still is not waited for");
            var status = Status(harness, new NpcManager(harness.Context, harness.Manager), youngblood);
            Assert.AreEqual(ConversationStatus.Available, status.ConvoStatusId);
            CollectionAssert.AreEqual(new uint[] { 1995 }, status.Data);
        }

        [TestMethod]
        public void WhatAGiverHasForThePlayerNowComesBeforeWhatWaits()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var mcAllister = Npc(harness, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var deSimone = Npc(harness, BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            var npcs = new NpcManager(harness.Context, harness.Manager);

            // McAllister gives Initiation now and Gearing Up after it: the mission on offer is the status.
            CollectionAssert.AreEqual(new uint[] { 1992 }, Waiting(harness, mcAllister));
            var status = Status(harness, npcs, mcAllister);
            Assert.AreEqual(ConversationStatus.Available, status.ConvoStatusId);
            CollectionAssert.AreEqual(new uint[] { 1990 }, status.Data);

            // And an NPC with a trade keeps its own status: the client knows an auctioneer by it.
            deSimone.Npc.NpcIsAuctioneer = true;
            try
            {
                Assert.AreEqual(ConversationStatus.Auctioneer, Status(harness, npcs, deSimone).ConvoStatusId);
            }
            finally
            {
                deSimone.Npc.NpcIsAuctioneer = false;
            }
        }

        [TestMethod]
        public void TalkingToAGiverWithOnlyAMissionThatWaitsClosesTheConversationAndNamesTheMission()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var deSimone = Npc(harness, BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            var npcs = new NpcManager(harness.Context, harness.Manager);

            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = deSimone.EntityId });

            var sent = harness.Drain();
            var conversation = sent.OfType<ConversePacket>().Single();
            Assert.AreEqual(1, conversation.ConvoDataDict.Count);
            Assert.AreEqual(true, conversation.ConvoDataDict[ConversationType.EndConversation]);

            var message = sent.OfType<DisplayClientMessagePacket>().Single();
            Assert.AreEqual(PlayerMessage.PmMissionNotAvailableNow, message.MsgId);
            Assert.AreEqual(1994u, message.NumberArgs["missionId"]);
            Assert.AreEqual(0, message.Args.Count);
        }

        /// <summary>
        /// Reported from play: every objective of Gearing Up for Battle done but the last word
        /// with Corporal Hartmann, and Corporal DeSimone - whom the mission is handed in to, and
        /// who gives Capture the Flag after it - answered "'Capture the Flag' is not available to
        /// you now." What he has to say is about the mission the player is holding.
        /// </summary>
        [TestMethod]
        public void TheNpcAMissionIsHandedInToSaysItIsNotFinishedAheadOfWhatHeGivesAfterIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var deSimone = Npc(harness, BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            var mcAllister = Npc(harness, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var npcs = new NpcManager(harness.Context, harness.Manager);

            Assert.IsEmpty(Unfinished(harness, deSimone), "nothing is held yet");

            Assert.IsTrue(harness.Manager.TryGiveMission(harness.Client, 1992, "a test"));
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);

            CollectionAssert.AreEqual(new uint[] { 1992 }, Unfinished(harness, deSimone));
            CollectionAssert.AreEqual(new uint[] { 1994 }, Waiting(harness, deSimone));
            Assert.IsEmpty(Unfinished(harness, mcAllister), "the giver is not who it is handed in to");

            AssertNotFinished(harness, npcs, deSimone);

            // All of it but the last: Hartmann's "report in to Corporal DeSimone".
            foreach (var objective in new uint[] { 4, 1, 2, 5, 6, 3, 9, 8 })
                Assert.IsTrue(harness.Manager.TryForceCompleteObjective(harness.Client, 1992, objective, "a test"), $"objective {objective}");

            Assert.IsFalse(harness.Client.Player.Missions[1992].Completeable);
            AssertNotFinished(harness, npcs, deSimone);

            Assert.IsTrue(harness.Manager.TryForceCompleteObjective(harness.Client, 1992, 7, "a test"));
            Assert.IsTrue(harness.Client.Player.Missions[1992].Completeable);
            Assert.IsEmpty(Unfinished(harness, deSimone));

            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = deSimone.EntityId });

            var sent = harness.Drain();
            var handIn = (Dictionary<uint, RewardInfo>)sent.OfType<ConversePacket>().Single().ConvoDataDict[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(new uint[] { 1992 }, handIn.Keys.ToArray());
            Assert.IsEmpty(sent.OfType<DisplayClientMessagePacket>().ToArray());
        }

        #region Fixture

        /// <summary>Asked, the NPC closes the conversation and says the mission is not finished - and nothing of what waits.</summary>
        private static void AssertNotFinished(BootcampRuntimeTestHarness.Harness harness, NpcManager npcs, Creature npc)
        {
            harness.MovePlayerTo(npc);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });

            var sent = harness.Drain();
            var conversation = sent.OfType<ConversePacket>().Single();
            Assert.AreEqual(1, conversation.ConvoDataDict.Count);
            Assert.AreEqual(true, conversation.ConvoDataDict[ConversationType.EndConversation]);

            var message = sent.OfType<DisplayClientMessagePacket>().Single();
            Assert.AreEqual(PlayerMessage.PmHaveNotCompletedRequirements, message.MsgId);
            Assert.AreEqual(0, message.Args.Count);
            Assert.AreEqual(0, message.NumberArgs.Count);
        }

        private static uint[] Unfinished(BootcampRuntimeTestHarness.Harness harness, Creature npc) =>
            harness.Manager.ClassifyNpcConversation(harness.Client.Player, npc).Unfinished.ToArray();

        private static Creature Npc(BootcampRuntimeTestHarness.Harness harness, uint creatureId) =>
            BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, creatureId);

        private static uint[] Waiting(BootcampRuntimeTestHarness.Harness harness, Creature npc) =>
            harness.Manager.ClassifyNpcConversation(harness.Client.Player, npc).NotYetAvailable.ToArray();

        /// <summary>The status the NPC manager sends this client for the NPC.</summary>
        private static NPCConversationStatusPacket Status(BootcampRuntimeTestHarness.Harness harness, NpcManager npcs, Creature npc)
        {
            WorldTestContext.Drain(harness.Client);
            npcs.UpdateConversationStatus(harness.Client, npc, harness.Manager);

            return WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == npc.EntityId).Select(message => message.Packet)
                .OfType<NPCConversationStatusPacket>().Single();
        }

        /// <summary>These missions done and rewarded, in the journal the classification reads.</summary>
        private static void Complete(BootcampRuntimeTestHarness.Harness harness, params uint[] missions)
        {
            var player = harness.Client.Player;

            foreach (var mission in missions)
                harness.SeedMission(player.Id, mission, (uint)MissionState.Completed, true);

            using var unit = harness.Context.CreateChar();
            harness.Manager.Hydrate(player, unit.CharacterMissions.Get(player.Id), unit.CharacterMissionProgress.Get(player.Id));
        }

        #endregion
    }
}
