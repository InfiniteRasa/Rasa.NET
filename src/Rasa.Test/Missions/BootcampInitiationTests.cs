using System;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.UnitOfWork;

    [TestClass]
    [DoNotParallelize]
    public class BootcampInitiationTests
    {
        [TestMethod]
        public void MissionLogDoesNotShowTheUnrevealedSecondElohObjective()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            harness.Drain();
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();

            CollectionAssert.AreEqual(new uint[] { 1 },
                gained.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray(),
                "The native mission window renders every received row, even when its status is Inactive.");
            Assert.AreEqual(2, harness.Client.Player.Missions[1990].Objectives.Count,
                "Hiding a future objective must not remove its durable progression state.");
            harness.ReconnectFromSelection();
            CollectionAssert.AreEqual(new uint[] { 1 },
                harness.Manager.BuildStatusSnapshot(harness.Client.Player)[1990].ObjectivesList
                    .Select(objective => objective.ObjectiveId).ToArray());

            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            var revealed = harness.Drain().OfType<ObjectiveRevealedPacket>().Single();
            CollectionAssert.AreEqual(new uint[] { 1, 2 },
                revealed.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
            Assert.AreEqual(MissionObjectiveState.Completed, revealed.MissionInfo.ObjectivesList[0].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, revealed.MissionInfo.ObjectivesList[1].State);
        }

        [TestMethod]
        public void RevealedButNotActivatedObjectivesRemainVisibleAfterReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            using (var unit = harness.Context.CreateChar())
            {
                unit.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, 1990)[2].ObjectiveState =
                    (byte)MissionObjectiveState.NotAssigned;
                unit.Complete();
            }
            harness.ReconnectFromSelection();

            var info = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[1990];
            CollectionAssert.AreEqual(new uint[] { 1, 2 },
                info.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
            Assert.AreEqual(MissionObjectiveState.NotAssigned, info.ObjectivesList[1].State);
        }

        [TestMethod]
        public void ArrivalOffersInitiationUntilTheClientAcceptsAndAcceptanceSurvivesReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var characterId = harness.Client.Player.Id;
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            var arrival = harness.Drain();
            Assert.AreEqual(1, arrival.Count(packet => packet.Opcode == GameOpcode.DispenseRadioMission));
            var offer = arrival.OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual(BootcampRuntimeTestHarness.MissionInitiation, offer.MissionId);
            Assert.IsTrue(offer.ForceDialog);
            CollectionAssert.AreEqual(new[] { 1U },
                offer.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
            Assert.IsFalse(arrival.OfType<MissionGainedPacket>().Any());
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(
                    characterId, BootcampRuntimeTestHarness.MissionInitiation));

            harness.ReconnectFromSelection();
            Assert.AreEqual(1, harness.Drain().Count(packet => packet.Opcode == GameOpcode.DispenseRadioMission));
            Assert.AreEqual(characterId, harness.Client.Player.Id);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));

            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.AreEqual(MissionState.Active,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].State);
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count());
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Incomplete), (2U, MissionObjectiveState.Inactive));

            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Any());
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(1, unit.CharacterMissions.Get(characterId)
                    .Count(mission => mission.MissionId == BootcampRuntimeTestHarness.MissionInitiation));

            harness.ReconnectFromSelection();
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.IsFalse(harness.Drain().Any(packet =>
                packet.Opcode == GameOpcode.DispenseRadioMission || packet is MissionGainedPacket));
        }

        [TestMethod]
        [DataRow(CharacterStartingExperienceState.Pending)]
        [DataRow(CharacterStartingExperienceState.Legacy)]
        [DataRow(CharacterStartingExperienceState.Completed)]
        [DataRow(CharacterStartingExperienceState.Skipped)]
        public void RadioAcceptanceCannotBypassStartingExperienceState(CharacterStartingExperienceState state)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using (var unit = harness.Context.CreateChar())
            {
                unit.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(
                    harness.Client.Player.Id, "deployment_11", state));
                unit.Complete();
            }

            harness.Drain();
            harness.Manager.PublishInitialState(harness.Client);
            new CharacterManager(harness.Context, harness.Manager).OfferStartingExperienceMission(harness.Client);
            Assert.IsFalse(harness.Drain().Any(packet => packet.Opcode == GameOpcode.DispenseRadioMission));
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Any());
        }

        [TestMethod]
        public void FailedRadioAcceptanceRollsBackAndCanBeRetriedWithoutAFalseMissionGain()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            harness.Drain();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected mission acceptance failure.");
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Any());
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(
                    harness.Client.Player.Id, BootcampRuntimeTestHarness.MissionInitiation));

            harness.Context.BeforeSave = null;
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void RadioAcceptanceRejectsOtherMissionsAndACharacterOutsideBootcamp()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            harness.Drain();
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionGearingUp);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionGearingUp));
            harness.Client.Player.MapContextId = BootcampRuntimeTestHarness.WildernessMapContextId;
            AcceptRadioMission(harness.Client, BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.IsFalse(harness.Drain().OfType<MissionGainedPacket>().Any());
        }

        [TestMethod]
        public void FirstElohAreaPlaysTheNativeLightningHighlightGreetingOnce()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client, mcAllister.EntityId, BootcampRuntimeTestHarness.MissionInitiation));
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(
                BootcampRuntimeTestHarness.MissionInitiation, 430, out var bridge));
            var outside = bridge.Position + new Vector3(100, 0, 0);
            var areas = new MissionAreaService(() => harness.Manager);

            Assert.IsTrue(areas.RecordAcceptedMovement(harness.Client, outside, bridge.Position));
            var greeting = harness.Drain().OfType<ForceConversePacket>().Single();
            Assert.AreEqual(1634, greeting.GreetingId,
                "The native conversation window starts tutlightning_left/right only for greeting 1634.");
            Assert.IsFalse(areas.RecordAcceptedMovement(harness.Client, outside, bridge.Position));
            Assert.IsFalse(harness.Drain().OfType<ForceConversePacket>().Any());

            harness.ReconnectFresh();
            areas = new MissionAreaService(() => harness.Manager);
            Assert.IsFalse(areas.RecordAcceptedMovement(harness.Client, outside, bridge.Position));
            Assert.IsFalse(harness.Drain().OfType<ForceConversePacket>().Any());
        }

        [TestMethod]
        public void InitiationRequiresOrderedAreasSurvivesReconnectsAndCannotBeAbandoned()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionInitiation));
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Incomplete),
                (2U, MissionObjectiveState.Inactive));

            Assert.IsFalse(harness.Manager.TryAbandon(
                harness.Client,
                BootcampRuntimeTestHarness.MissionInitiation));
            using (var unit = harness.Context.CreateChar())
            {
                Assert.IsNotNull(unit.CharacterMissions.GetByCharacterAndMission(
                    harness.Client.Player.Id,
                    BootcampRuntimeTestHarness.MissionInitiation));
            }

            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, 431)));
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Incomplete),
                (2U, MissionObjectiveState.Inactive));

            harness.Reconnect();
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Incomplete),
                (2U, MissionObjectiveState.Inactive));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, 430)));
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Incomplete));
            Assert.AreEqual(
                1,
                harness.Context.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.ObjectiveId == 1));

            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, 430)));

            harness.Reconnect();
            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation],
                (1U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Incomplete));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, 431)));
            Assert.IsTrue(harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].Completeable);
            Assert.AreEqual(
                1,
                harness.Context.Drain().OfType<MissionCompleteablePacket>().Count(packet => packet.IsCompleteable));

            harness.Reconnect();
            Assert.IsTrue(harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].Completeable);
            var currentMcAllister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                currentMcAllister?.EntityId ?? mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionInitiation,
                selectionIndex: null,
                rating: null));
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].State);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                currentMcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
        }

        private static void AcceptRadioMission(Client client, uint missionId)
        {
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var packetType = router.GetPacketType(GameOpcode.AssignRadioMission);
            Assert.IsNotNull(packetType);
            var packet = (ClientPythonPacket)Activator.CreateInstance(packetType);
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream)))
            {
                writer.WriteTuple(1);
                writer.WriteUInt(missionId);
            }
            using var input = new MemoryStream(stream.ToArray());
            using var reader = new PythonReader(new BinaryReader(input));
            packet.Read(reader);
            Assert.AreEqual(input.Length, input.Position);
            router.RoutePacket(handler, packet);
        }
    }
}
