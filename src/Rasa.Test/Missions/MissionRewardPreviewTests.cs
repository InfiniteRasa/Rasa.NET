using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionRewardPreviewTests
    {
        [TestMethod]
        [DataRow("offer")]
        [DataRow("accepted")]
        [DataRow("snapshot")]
        public void NativeMissionProjectionsIncludeAuthoredCurrencyAndItemRewardsWithoutGrantingThem(string stage)
        {
            using var context = MissionTestContext.WithObjectiveMission();
            context.AddRewardTemplate(28, 3147);
            var reward = new MissionRewardDefinition(100,
                new Dictionary<CurencyType, int> { [CurencyType.Credits] = 25, [CurencyType.Prestige] = 7 },
                new[] { new MissionRewardItem(28, 3) }, new[] { new MissionRewardItem(29, 2) });
            var manager = new MissionApplication(context, context.Manager.LoadedMissions,
                new Dictionary<uint, MissionRewardDefinition> { [321] = reward },
                new ManifestationManager(context));
            var giver = context.AddNpc(77);
            var before = context.ReadRewardTotals();
            context.Drain();
            MissionInfo info;
            if (stage == "offer")
            {
                new NpcManager(context, manager).RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = giver.EntityId });
                var packet = context.Drain().OfType<ConversePacket>().Single();
                info = ((Dictionary<uint, MissionInfo>)packet.ConvoDataDict[ConversationType.MissionDispense])[321];
            }
            else
            {
                Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
                var gained = context.Drain().OfType<MissionGainedPacket>().Single();
                if (stage == "snapshot")
                {
                    using var unit = context.CreateChar();
                    manager.HydrateAndClearInvalid(context.Client.Player, unit);
                    info = manager.BuildStatusSnapshot(context.Client.Player)[321];
                }
                else
                    info = gained.MissionInfo;
            }

            var preview = info.MissionConstantData.RewardInfo;
            Assert.IsTrue(preview.FixedReward.Credits.TryGetValue(CurencyType.Credits, out var credits),
                "The native reward preview omitted its authored credits.");
            Assert.AreEqual(25U, credits);
            Assert.AreEqual(7U, preview.FixedReward.Credits[CurencyType.Prestige]);
            Assert.AreEqual(28U, preview.FixedReward.FixedItems.Single().ItemTemplateId);
            Assert.AreEqual(3U, preview.FixedReward.FixedItems.Single().Quantity);
            Assert.AreEqual(29U, preview.SelectableReward.Single().ItemTemplateId);
            Assert.AreEqual(2U, preview.SelectableReward.Single().Quantity);
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
                info.WriteOffer(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadUInt();
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual((int)CurencyType.Credits, reader.ReadInt());
            Assert.AreEqual(25U, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual((int)CurencyType.Prestige, reader.ReadInt());
            Assert.AreEqual(7U, reader.ReadUInt());
            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience, after.Experience);
            Assert.AreEqual(before.Credits, after.Credits);
            Assert.AreEqual(before.Prestige, after.Prestige);
            Assert.AreEqual(before.ItemCount, after.ItemCount);
        }

        [TestMethod]
        public void InitiationRadioOfferShowsCreditsWithoutConfusingThemWithItsExperienceGrant()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var offer = harness.Drain().OfType<DispenseRadioMissionPacket>().Single();
            var row = harness.WorldContext.Set<MissionRewardDefinitionEntry>()
                .Single(reward => reward.MissionId == 1990 && reward.RewardId == 1);
            Assert.AreEqual(100U, row.Experience);
            Assert.AreEqual(100U, offer.MissionInfo.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);
            Assert.AreEqual(0U, offer.MissionInfo.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Prestige]);
            Assert.AreEqual(0U, harness.Client.Player.Experience);
        }

        [TestMethod]
        [DataRow(1990U, 100U)]
        [DataRow(1992U, 200U)]
        [DataRow(1994U, 200U)]
        [DataRow(1995U, 200U)]
        [DataRow(2005U, 200U)]
        public void EveryBootcampTurnInHasTheApprovedCreditReward(uint missionId, uint credits)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Assert.IsTrue(harness.Manager.TryGetRewardInfo(missionId, out var reward));
            Assert.AreEqual(credits, reward.FixedReward.Credits[CurencyType.Credits]);
        }
    }
}
