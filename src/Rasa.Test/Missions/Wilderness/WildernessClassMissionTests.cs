using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using static Rasa.Test.Missions.Wilderness.WildernessAliaBranchesTests;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessClassMissionTests
    {
        [TestMethod]
        [DataRow(CharacterClass.Recruit, (byte)4, false)]
        [DataRow(CharacterClass.Recruit, (byte)5, true)]
        [DataRow(CharacterClass.Soldier, (byte)5, true)]
        [DataRow(CharacterClass.Specialist, (byte)5, true)]
        [DataRow(CharacterClass.Ranger, (byte)15, true)]
        public void TrainingDayKeepsTheRealTrainerServiceAndDoesNotForceRetraining(
            CharacterClass characterClass, byte level, bool eligible)
        {
            using var harness = CreateHarness();
            SetProfile(harness, characterClass, level);
            harness.SpawnWorld(100, 510006, 501001, 501002);
            var npcs = new NpcManager(harness, harness.Manager);
            var rogers = harness.Npc(100);
            harness.MoveTo(rogers.Position);
            Assert.AreEqual(eligible, Offers(Open(harness, npcs, rogers.EntityId), 1526));

            foreach (var trainerId in new uint[] { 501001, 501002 })
            {
                var trainer = harness.Npc(trainerId);
                Assert.IsNotNull(trainer);
                Assert.IsTrue(trainer.Npc.NpcIsTrainer);
                harness.MoveTo(trainer.Position);
                Assert.IsTrue(Open(harness, npcs, trainer.EntityId).ConvoDataDict
                    .ContainsKey(ConversationType.Training));
            }
            if (!eligible)
                return;

            Accept(harness, npcs, 100, 1526);
            var kincaid = harness.Npc(510006);
            Assert.IsNotNull(kincaid);
            Assert.AreEqual(2588U, kincaid.Npc.NpcPackageId);
            Assert.IsTrue(kincaid.Npc.NpcIsTrainer);
            harness.MoveTo(kincaid.Position);
            var conversation = Open(harness, npcs, kincaid.EntityId);
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.Training, out var service));
            Assert.IsInstanceOfType<TrainingConverse>(service);
            Assert.AreEqual(characterClass == CharacterClass.Recruit, ((TrainingConverse)service).CanTrain);
            Assert.IsTrue(conversation.ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete),
                "The same NPC conversation must offer the mission objective and training service.");
            CompleteObjective(harness, npcs, 510006, 1526, 1);
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            TurnIn(harness, npcs, 510006, 1526);
            ReplayTurnIn(harness, npcs, 510006, 1526);
            Assert.AreEqual((uint)characterClass, harness.Client.Player.Class);
            Assert.AreEqual(before + 200, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience + 1000U, harness.Client.Player.Experience);
        }

        [TestMethod]
        [DataRow(CharacterClass.Soldier, 501001U, 2010U, 2011U, 4019U, 35486U)]
        [DataRow(CharacterClass.Specialist, 501002U, 2011U, 2010U, 3869U, 20250U)]
        public void NativeClassTrainingOffersOnlyItsGearBranchAndGrantsThePreviewOnce(
            CharacterClass selectedClass, uint trainerSpawn, uint missionId, uint excludedId,
            uint tool, uint boots)
        {
            using var harness = CreateHarness();
            SetProfile(harness, CharacterClass.Recruit, 5);
            harness.SpawnWorld(trainerSpawn, 210);
            var npcs = new NpcManager(harness, harness.Manager);
            var trainer = harness.Npc(trainerSpawn);
            harness.MoveTo(trainer.Position);
            var training = Open(harness, npcs, trainer.EntityId);
            Assert.IsTrue(((TrainingConverse)training.ConvoDataDict[ConversationType.Training]).CanTrain);
            harness.Manager.PublishInitialState(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>()
                .Any(packet => packet.MissionId is 2010 or 2011), "Untrained recruits must not get a class gear offer.");

            Route(harness, new SelectNewCharacterClassPacket { ClassId = (uint)selectedClass });
            var offers = harness.Drain().OfType<DispenseRadioMissionPacket>().ToArray();
            Assert.AreEqual(1, offers.Count(packet => packet.MissionId == missionId));
            Assert.IsFalse(offers.Any(packet => packet.MissionId == excludedId));
            var preview = offers.Single(packet => packet.MissionId == missionId)
                .MissionInfo.MissionConstantData.RewardInfo.FixedReward.FixedItems;
            CollectionAssert.AreEquivalent(new[] { tool, boots }, preview.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsTrue(preview.All(item => item.Quantity == 1));
            using (var unit = harness.CreateChar())
                Assert.AreEqual((uint)selectedClass, unit.Characters.Get(harness.Client.Player.Id).Class);

            Route(harness, new AssignRadioMissionPacket { MissionId = excludedId });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(excludedId));
            Route(harness, new AssignRadioMissionPacket { MissionId = missionId });
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == missionId));
            CompleteObjective(harness, npcs, 210, missionId, 1);
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            TurnIn(harness, npcs, 210, missionId);
            ReplayTurnIn(harness, npcs, 210, missionId);
            Assert.AreEqual(1U, HeldQuantity(harness, tool));
            Assert.AreEqual(1U, HeldQuantity(harness, boots));
            Assert.AreEqual(before + 200, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience + 1000U, harness.Client.Player.Experience);

            Route(harness, new SelectNewCharacterClassPacket
            {
                ClassId = (uint)(selectedClass == CharacterClass.Soldier ? CharacterClass.Specialist : CharacterClass.Soldier)
            });
            harness.Manager.PublishInitialState(harness.Client);
            Assert.AreEqual((uint)selectedClass, harness.Client.Player.Class);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>()
                .Any(packet => packet.MissionId is 2010 or 2011));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(excludedId));
            VerifyArmorReward(harness, boots, selectedClass, selectedClass == CharacterClass.Soldier ? 21U : 30U);
        }

        [TestMethod]
        [DataRow(CharacterClass.Soldier, (byte)5, 2010U, 2011U)]
        [DataRow(CharacterClass.Ranger, (byte)15, 2010U, 2011U)]
        [DataRow(CharacterClass.Sniper, (byte)30, 2010U, 2011U)]
        [DataRow(CharacterClass.Specialist, (byte)5, 2011U, 2010U)]
        [DataRow(CharacterClass.Sapper, (byte)15, 2011U, 2010U)]
        [DataRow(CharacterClass.Engineer, (byte)30, 2011U, 2010U)]
        public void InitialStateRecognizesAlreadyTrainedDescendantsWithoutRetraining(
            CharacterClass characterClass, byte level, uint missionId, uint excludedId)
        {
            using var harness = CreateHarness();
            SetProfile(harness, characterClass, level);
            harness.Manager.PublishInitialState(harness.Client);
            var offers = harness.Drain().OfType<DispenseRadioMissionPacket>().ToArray();
            Assert.AreEqual(1, offers.Count(packet => packet.MissionId == missionId));
            Assert.IsFalse(offers.Any(packet => packet.MissionId == excludedId));
            harness.Manager.PublishInitialState(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>()
                .Any(packet => packet.MissionId is 2010 or 2011), "An unchanged pending offer must not be duplicated.");
            Route(harness, new AssignRadioMissionPacket { MissionId = missionId });
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(missionId));
            Assert.AreEqual((uint)characterClass, harness.Client.Player.Class);
        }

        [TestMethod]
        public void ClientSideClassChangesCannotQualifyAnUntrainedCharacter()
        {
            using var harness = CreateHarness();
            SetProfile(harness, CharacterClass.Recruit, 5);
            harness.Client.Player.Class = (uint)CharacterClass.Soldier;
            harness.Manager.PublishInitialState(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>()
                .Any(packet => packet.MissionId is 2010 or 2011));
            Route(harness, new AssignRadioMissionPacket { MissionId = 2010 });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(2010));
            using var unit = harness.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 2010));
        }

        [TestMethod]
        public void TheTwoItemClassKitCannotPartiallyCommitWithOnlyOneFreeSlot()
        {
            using var harness = CreateHarness();
            SetProfile(harness, CharacterClass.Soldier, 5);
            harness.SpawnWorld(210);
            harness.Manager.PublishInitialState(harness.Client);
            Route(harness, new AssignRadioMissionPacket { MissionId = 2010 });
            var npcs = new NpcManager(harness, harness.Manager);
            CompleteObjective(harness, npcs, 210, 2010, 1);
            FillCategory(harness, 3869, 49);
            var caufield = harness.Npc(210);
            Open(harness, npcs, caufield.EntityId);
            var request = new CompleteNPCMissionPacket { EntityId = caufield.EntityId, MissionId = 2010 };
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            var experience = harness.Client.Player.Experience;
            npcs.CompleteNPCMission(harness.Client, request);
            Assert.AreEqual(0U, HeldQuantity(harness, 4019));
            Assert.AreEqual(0U, HeldQuantity(harness, 35486));
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(experience, harness.Client.Player.Experience);
            FreeOneSlot(harness, 3869);
            npcs.CompleteNPCMission(harness.Client, request);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[2010].State);
            Assert.AreEqual(1U, HeldQuantity(harness, 4019));
            Assert.AreEqual(1U, HeldQuantity(harness, 35486));
            ReplayTurnIn(harness, npcs, 210, 2010);
            Assert.AreEqual(1U, HeldQuantity(harness, 4019));
            Assert.AreEqual(1U, HeldQuantity(harness, 35486));
        }

        [TestMethod]
        public void ANewConnectionNeedsAFreshAuthoritativeClassOffer()
        {
            using var harness = CreateHarness();
            SetProfile(harness, CharacterClass.Soldier, 5);
            harness.Manager.PublishInitialState(harness.Client);
            string originalOffer;
            using (var unit = harness.CreateChar())
            {
                var offer = unit.MissionOffers.Get(harness.Client.Player.Id, 2010);
                Assert.IsNotNull(offer);
                originalOffer = offer.OfferId;
            }
            var reconnect = harness.Context.CreateCompetingClient(harness.Manager);
            try
            {
                using (var unit = harness.CreateChar())
                    reconnect.Player.Class = unit.Characters.Get(reconnect.Player.Id).Class;
                reconnect.SetWorldPosition(harness.Client.Player.Position, harness.Client.Player.Rotation);
                CellManager.Instance.UpdateVisibility(reconnect);
                Route(harness, new AssignRadioMissionPacket { MissionId = 2010 }, reconnect);
                Assert.IsFalse(reconnect.Player.Missions.ContainsKey(2010));
                harness.Manager.PublishInitialState(reconnect);
                Assert.AreEqual(1, MissionTestContext.Drain(reconnect).OfType<DispenseRadioMissionPacket>()
                    .Count(packet => packet.MissionId == 2010));
                using (var unit = harness.CreateChar())
                    Assert.AreNotEqual(originalOffer, unit.MissionOffers.Get(reconnect.Player.Id, 2010).OfferId);
                Route(harness, new AssignRadioMissionPacket { MissionId = 2010 });
                Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(2010), "The old connection cannot consume the new offer.");
                Route(harness, new AssignRadioMissionPacket { MissionId = 2010 }, reconnect);
                Assert.IsTrue(reconnect.Player.Missions.ContainsKey(2010));
                Assert.AreEqual((uint)CharacterClass.Soldier, reconnect.Player.Class);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(reconnect);
                harness.Map.ClientList.Remove(reconnect);
            }
        }

        private static void SetProfile(
            WildernessRuntimeTestHarness harness, CharacterClass characterClass, byte level)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                unit.Characters.UpdateCharacterClass(harness.Client.Player.Id, (uint)characterClass);
                unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id,
                    harness.Client.Player.Experience, level);
            });
            var persisted = unit.Characters.Get(harness.Client.Player.Id);
            Assert.AreEqual((uint)characterClass, persisted.Class);
            Assert.AreEqual(level, persisted.Level);
            harness.Client.Player.Class = (uint)characterClass;
            harness.Client.Player.Level = level;
            harness.Drain();
        }

        private static void Route(WildernessRuntimeTestHarness harness, ClientPythonPacket packet, Client client = null)
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client ?? harness.Client);
            router.RoutePacket(handler, packet);
        }
    }
}
