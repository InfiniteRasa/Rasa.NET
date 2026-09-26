using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Content;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class MissionAudioTests
    {
        [TestMethod]
        public void AudioMetadataWorksForAMainWorldMissionWithoutBootcampBranches()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            Assert.AreEqual(1220U, context.Client.Player.MapContextId);
            var catalog = new MissionContentCatalog(context, context.Manager.LoadedMissions,
                new Dictionary<uint, MissionRewardDefinition>());
            catalog.SceneBindings[321] = new MissionSceneDefinition
            {
                Audio = new MissionAudioDefinition
                {
                    OfferAudioSetId = 2774,
                    Events = new() { [MissionAudioEvent.Accepted] = 2775, [MissionAudioEvent.Completed] = 2777 },
                    Announcements = new() { [1634] = 2788 }
                }
            };
            var protocol = new MissionProtocolAdapter(context, catalog, new MissionJournalAdapter(catalog),
                () => DateTime.UtcNow, null, context.Manager.Sharing.CanShare);
            var offer = protocol.BuildOfferInfo(catalog.Missions[321]);
            Assert.AreEqual(2774, offer.AudioSetId);
            protocol.PublishAudio(context.Client, 321, MissionAudioEvent.Accepted);
            protocol.PublishAudio(context.Client, 321, MissionAudioEvent.Completed);
            protocol.PublishAnnouncementAudio(context.Client, 321, 1634);
            CollectionAssert.AreEqual(new uint?[] { 2775, 2777, 2788 },
                context.Drain().OfType<PlayTutorialAudioPacket>().Select(packet => packet.AudioSetId).ToArray());
            protocol.PublishAnnouncementAudio(context.Client, 321, 9999);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void InitiationOfferCarriesItsNativeNarrationInTheExistingWireSlot()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var packet = harness.Drain().OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual(2773, packet.MissionInfo.AudioSetId);
            using var stream = new MemoryStream(MissionTestContext.Encode(packet));
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(1990U, reader.ReadUInt());
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadUInt();
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            var currencies = reader.ReadList();
            for (var index = 0; index < currencies; index++)
            {
                Assert.AreEqual(2, reader.ReadTuple());
                reader.ReadInt();
                reader.ReadUInt();
            }
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(2773, reader.ReadInt());
        }

        [TestMethod]
        public void ElohAnnouncementsAndMcallisterCompletionPlayTheirCuesOnceAfterCommit()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            harness.Drain();

            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));

            var first = harness.Drain();
            Assert.IsTrue(first.OfType<ForceConversePacket>().Any());
            Assert.AreEqual(2788U, first.OfType<PlayTutorialAudioPacket>().Single().AudioSetId);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            Assert.IsFalse(harness.Drain().OfType<PlayTutorialAudioPacket>().Any());
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 431)));
            Assert.AreEqual(2789U, harness.Drain().OfType<PlayTutorialAudioPacket>().Single().AudioSetId);
            var priorCredits = harness.Client.Player.Credits[CurencyType.Credits];
            var priorExperience = harness.Client.Player.Experience;
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
            Assert.AreEqual(priorCredits + 100, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(priorExperience + 100U, harness.Client.Player.Experience);
            Assert.AreEqual(2777U, harness.Drain().OfType<PlayTutorialAudioPacket>().Single().AudioSetId);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
            Assert.IsFalse(harness.Drain().OfType<PlayTutorialAudioPacket>().Any());
        }

        [TestMethod]
        public void AcceptedAudioWaitsForCommitAndDoesNotReplayOnRetryOrReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: scenes =>
                scenes[1990].Audio = new MissionAudioDefinition
                {
                    Events = new() { [MissionAudioEvent.Accepted] = 2773 }
                });
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.Drain();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected acceptance failure.");
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            Assert.IsFalse(harness.Drain().OfType<PlayTutorialAudioPacket>().Any());
            harness.Context.BeforeSave = null;

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));

            Assert.AreEqual(2773U, harness.Drain().OfType<PlayTutorialAudioPacket>().Single().AudioSetId);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            Assert.IsFalse(harness.Drain().OfType<PlayTutorialAudioPacket>().Any());
            harness.ReconnectFresh(drainPackets: false);
            Assert.IsFalse(harness.Drain().OfType<PlayTutorialAudioPacket>().Any());
        }

        [TestMethod]
        [DataRow(0U)]
        [DataRow(uint.MaxValue)]
        public void InvalidAudioBindingsFailReadiness(uint audioId)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Content.MissionContentTestSupport.ConfigureScenes(harness.WorldContext,
                scenes => scenes[1990].Audio = new MissionAudioDefinition { OfferAudioSetId = audioId });
            Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => harness.Manager.LoadMissions());
        }
    }
}
