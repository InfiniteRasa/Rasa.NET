using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game.Handlers;
    using Rasa.Memory;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Scenes;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionDialogueTests
    {
        [TestMethod]
        [DataRow(ConversationType.Greeting, "8161101D13")]
        [DataRow(ConversationType.ForceTopic, "81611182171E5301")]
        [DataRow(ConversationType.MissionDispense, "816112611E53018611828270707000707011")]
        [DataRow(ConversationType.MissionComplete, "816113611E53018282707070")]
        [DataRow(ConversationType.MissionReminder, "816114711E5301")]
        [DataRow(ConversationType.ObjectiveAmbient, "81611571831E53011811")]
        [DataRow(ConversationType.ObjectiveComplete, "81611671831E53011811")]
        [DataRow(ConversationType.MissionReward, "816117611E53018282707070")]
        [DataRow(ConversationType.ObjectiveChoice, "81611871831E53011811")]
        [DataRow(ConversationType.EndConversation, "81611901")]
        [DataRow(ConversationType.Training, "81611A82021D13")]
        [DataRow(ConversationType.Vending, "81611B711E4A02")]
        [DataRow(ConversationType.ImportantGreering, "81611C1D13")]
        [DataRow(ConversationType.Clan, "81611D0D01")]
        [DataRow(ConversationType.Auctioneer, "81611D0E02")]
        [DataRow(ConversationType.ForcedByScript, "81611D0F01")]
        public void ConverseTypesMatchNativeBytes(ConversationType kind, string expected)
        {
            var value = kind switch
            {
                ConversationType.Greeting or ConversationType.ImportantGreering => (object)19,
                ConversationType.ForceTopic => new ForceTopic(ConversationType.MissionReward, 339),
                ConversationType.MissionDispense => new Dictionary<uint, MissionInfo>
                {
                    [339] = new() { MissionConstantData = new() { Level = 1, GroupType = 1 } }
                },
                ConversationType.MissionComplete => new Dictionary<uint, RewardInfo> { [339] = new() },
                ConversationType.MissionReminder => new List<uint> { 339 },
                ConversationType.ObjectiveAmbient => new List<AmbientObjectives> { new(339, 8, 1) },
                ConversationType.ObjectiveComplete => new List<CompleteableObjectives> { new(339, 8, 1) },
                ConversationType.MissionReward => new List<RewardableMissions> { new(339, new()) },
                ConversationType.ObjectiveChoice => new List<ChoiceObjectives> { new(339, 8, 1) },
                ConversationType.Training => new TrainingConverse(false, 19),
                ConversationType.Vending => new List<uint> { 586 },
                ConversationType.Auctioneer => false,
                _ => true
            };

            CollectionAssert.AreEqual(Convert.FromHexString(expected),
                MissionTestContext.Encode(new ConversePacket(new() { [kind] = value })));
        }

        [TestMethod]
        [DataRow(ConversationType.Greeting)]
        [DataRow(ConversationType.ForceTopic)]
        [DataRow(ConversationType.MissionDispense)]
        [DataRow(ConversationType.MissionComplete)]
        [DataRow(ConversationType.MissionReminder)]
        [DataRow(ConversationType.ObjectiveAmbient)]
        [DataRow(ConversationType.ObjectiveComplete)]
        [DataRow(ConversationType.MissionReward)]
        [DataRow(ConversationType.ObjectiveChoice)]
        [DataRow(ConversationType.EndConversation)]
        [DataRow(ConversationType.Training)]
        [DataRow(ConversationType.Vending)]
        [DataRow(ConversationType.ImportantGreering)]
        [DataRow(ConversationType.Clan)]
        [DataRow(ConversationType.Auctioneer)]
        [DataRow(ConversationType.ForcedByScript)]
        [DataRow((ConversationType)99)]
        public void InvalidConverseEntryFailsBeforeAnyOutput(ConversationType kind)
        {
            AssertInvalidConversation(kind, new object());
        }

        [TestMethod]
        public void InvalidNestedConversePayloadFailsBeforeAnyOutput()
        {
            AssertInvalidConversation(ConversationType.ObjectiveChoice,
                new List<ChoiceObjectives> { new(339, 8, 1), null });
            AssertInvalidConversation(ConversationType.ObjectiveComplete,
                new List<CompleteableObjectives> { new(339, 0, 1) });
            AssertInvalidConversation(ConversationType.MissionDispense,
                new Dictionary<uint, MissionInfo> { [339] = new() { MissionConstantData = null } });
            AssertInvalidConversation(ConversationType.MissionComplete,
                new Dictionary<uint, RewardInfo> { [339] = new() { SelectableReward = null } });
            AssertInvalidConversation(ConversationType.MissionReward,
                new List<RewardableMissions> { new(339, new() { FixedReward = null }) });
            AssertInvalidConversation(ConversationType.Vending, new List<uint>());
            AssertInvalidConversation(ConversationType.Vending, new List<uint> { 586, 134 });
            AssertInvalidConversation(ConversationType.Training, new TrainingConverse(true, 0));
            AssertInvalidConversation(ConversationType.ForcedByScript, 1);
            AssertInvalidConversation(ConversationType.EndConversation, null);
        }

        [TestMethod]
        public void ForcedByScriptFalseIsANativeBoolAndNullDictionaryWritesNothing()
        {
            CollectionAssert.AreEqual(Convert.FromHexString("81611D0F02"),
                MissionTestContext.Encode(new ConversePacket(new() { [ConversationType.ForcedByScript] = false })));
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            Assert.ThrowsExactly<InvalidDataException>(() => new ConversePacket(null).Write(writer));
            Assert.AreEqual(0L, stream.Length);
        }

        [TestMethod]
        public void NativeChoiceOpcodeHasARegisteredCallback()
        {
            Assert.AreEqual(497, (int)GameOpcode.PerformNPCChoice);
            var type = new PacketRouter<ClientPacketHandler, GameOpcode>()
                .GetPacketType(GameOpcode.PerformNPCChoice);
            Assert.IsNotNull(type, "The native choice request must reach an actual handler.");
            Assert.AreEqual("PerformNPCChoicePacket", type.Name);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void ChoicePacketPreservesNativeIndexAndSixtyFourBitEntityId(int index)
        {
            var packet = DecodeChoice(ChoicePayload(writer => writer.WriteInt(index)));
            Assert.AreEqual(GameOpcode.PerformNPCChoice, packet.Opcode);
            Assert.AreEqual(0x100000005UL, packet.EntityId);
            Assert.AreEqual(339U, packet.MissionId);
            Assert.AreEqual(8U, packet.ObjectiveId);
            Assert.AreEqual(1U, packet.PlayerFlagId);
            Assert.AreEqual(index, packet.ChoiceIdx);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(4)]
        [DataRow(int.MaxValue)]
        public void ChoicePacketRejectsNonNativeIndices(int index)
        {
            Assert.ThrowsExactly<InvalidDataException>(() =>
                DecodeChoice(ChoicePayload(writer => writer.WriteInt(index))));
        }

        [TestMethod]
        public void ChoicePacketRejectsWrongTupleAndScalarTypes()
        {
            foreach (var invalid in new Action<PythonWriter>[]
            {
                writer => writer.WriteBool(true),
                writer => writer.WriteBool(false),
                writer => writer.WriteNoneStruct(),
                writer => writer.WriteLong(1),
                writer => writer.WriteString("1")
            })
                Assert.ThrowsExactly<InvalidDataException>(() => DecodeChoice(ChoicePayload(invalid)));
            foreach (var tupleSize in new[] { 0, 4, 6 })
                Assert.ThrowsExactly<InvalidDataException>(() =>
                    DecodeChoice(ChoicePayload(writer => writer.WriteInt(1), tupleSize)));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void ChoicePacketRejectsWrongIdentityScalarTypes(int field)
        {
            foreach (var invalid in new Action<PythonWriter>[]
            {
                writer => writer.WriteBool(true), writer => writer.WriteNoneStruct(),
                writer => writer.WriteDouble(1), writer => writer.WriteString("1"),
                field == 0 ? writer => writer.WriteInt(1) : writer => writer.WriteLong(1)
            })
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));
                writer.WriteTuple(5);
                for (var index = 0; index < 4; index++)
                    if (field == index) invalid(writer);
                    else if (index == 0) writer.WriteULong(0x100000005UL);
                    else writer.WriteUInt(index == 1 ? 339U : index == 2 ? 8U : 1U);
                writer.WriteInt(1);
                Assert.ThrowsExactly<InvalidDataException>(() => DecodeChoice(stream.ToArray()));
            }
        }

        [TestMethod]
        [DataRow(MissionDialogueKind.Reminder, ConversationType.MissionReminder)]
        [DataRow(MissionDialogueKind.Ambient, ConversationType.ObjectiveAmbient)]
        [DataRow(MissionDialogueKind.Choice, ConversationType.ObjectiveChoice)]
        public void AuthoredDialogueIsNotCoercedIntoCompletion(MissionDialogueKind kind, ConversationType expected)
        {
            using var fixture = new DialogueFixture(kind);
            var packet = fixture.Open();
            Assert.IsTrue(packet.ConvoDataDict.ContainsKey(expected));
            Assert.IsFalse(packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                fixture.Context.Client.Player.Missions[339].Objectives[8].State);
            Assert.IsFalse(fixture.Context.Manager.TryCompleteNpcObjective(
                fixture.Context.Client, fixture.Npc.EntityId, 339, 8, 1));
            Assert.AreEqual(0, fixture.Context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void EachNativeChoiceSelectsOnlyItsAuthoredTransition(int index)
        {
            using var fixture = new DialogueFixture();
            fixture.Open();

            fixture.Choose(index);

            Assert.AreEqual(MissionObjectiveState.Completed,
                fixture.Context.Client.Player.Missions[339].Objectives[8].State);
            using var unit = fixture.Context.CreateChar();
            var objectives = unit.CharacterMissionProgress.GetTracked(1, 339);
            for (var branch = 1; branch <= 3; branch++)
            {
                Assert.AreEqual(branch == index ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive,
                    (MissionObjectiveState)objectives[(uint)(8 + branch)].ObjectiveState);
                Assert.AreEqual(branch == index, unit.CharacterFlags.Get(1).ContainsKey((uint)(700 + branch)));
            }
            Assert.IsFalse(unit.CharacterFlags.Get(1).ContainsKey(1),
                "The native dialogue selector is not an instruction to persist a player flag.");
            Assert.IsNull(fixture.Context.Client.MissionConversation);
            Assert.AreEqual(1, fixture.Context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void FailedOptionalChoicePublishesItsAuthoredSuccessorAndFlag()
        {
            using var fixture = new DialogueFixture(failSecondChoice: true);
            fixture.Open();

            fixture.Choose(2);

            var mission = fixture.Context.Client.Player.Missions[339];
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(MissionObjectiveState.Failed, mission.Objectives[8].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[10].State);
            Assert.AreEqual(1U, fixture.Context.Client.Player.PlayerFlags[702]);
            var packets = fixture.Context.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ObjectiveActivatedPacket>().Count());
            Assert.IsFalse(packets.OfType<MissionFailedPacket>().Any());
            using var unit = fixture.Context.CreateChar();
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                unit.CharacterMissionProgress.GetTracked(1, 339)[10].ObjectiveState);
        }

        [TestMethod]
        [DataRow("unopened")]
        [DataRow("other-npc")]
        [DataRow("package")]
        [DataRow("flag")]
        [DataRow("mission")]
        [DataRow("objective")]
        [DataRow("index-zero")]
        [DataRow("index-four")]
        [DataRow("distance")]
        [DataRow("class")]
        [DataRow("assignment")]
        [DataRow("generation")]
        [DataRow("revision")]
        public void ChoiceRequiresItsOpenedCurrentTopic(string invalid)
        {
            using var fixture = new DialogueFixture();
            if (invalid != "unopened")
                fixture.Open();
            var request = new PerformNPCChoicePacket
            {
                EntityId = fixture.Npc.EntityId, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 1
            };
            switch (invalid)
            {
                case "other-npc": request.EntityId = fixture.Context.AddNpc(501, npcPackageId: 586).EntityId; break;
                case "package": fixture.Npc.Npc.NpcPackageId = 134; break;
                case "flag": request.PlayerFlagId = 141; break;
                case "mission": request.MissionId = 331; break;
                case "objective": request.ObjectiveId = 9; break;
                case "index-zero": request.ChoiceIdx = 0; break;
                case "index-four": request.ChoiceIdx = 4; break;
                case "distance": fixture.Npc.Position = new Vector3(6, 0, 0); break;
                case "class": fixture.Npc.EntityClass = (EntityClasses)21081; break;
            }
            if (invalid is "assignment" or "generation" or "revision")
            {
                using var unit = fixture.Context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 339);
                    if (invalid == "assignment") assignment.AssignmentId = Guid.NewGuid().ToString("N");
                    if (invalid == "generation") assignment.Generation++;
                    if (invalid == "revision") assignment.ContentRevision = "unversioned";
                });
            }

            fixture.Route(request);

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                fixture.Context.Client.Player.Missions[339].Objectives[8].State);
            using (var unit = fixture.Context.CreateChar())
                Assert.AreEqual(0, unit.CharacterFlags.Get(1).Count);
            Assert.AreEqual(0, fixture.Context.Drain().Count);
        }

        [TestMethod]
        public void FailedChoiceRollsBackFlagsAndObjectivesAndCanRetryTheSameSession()
        {
            using var fixture = new DialogueFixture();
            fixture.Open();
            var session = fixture.Context.Client.MissionConversation;
            fixture.Context.AfterSave = _ => throw new DbUpdateException("Injected dialogue rollback.");

            fixture.Choose(2);

            Assert.AreSame(session, fixture.Context.Client.MissionConversation);
            using (var unit = fixture.Context.CreateChar())
            {
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    unit.CharacterMissionProgress.GetTracked(1, 339)[8].ObjectiveState);
                Assert.AreEqual(0, unit.CharacterFlags.Get(1).Count);
            }
            Assert.AreEqual(0, fixture.Context.Client.Player.PlayerFlags.Count);
            Assert.AreEqual(0, fixture.Context.Drain().Count);
            fixture.Context.AfterSave = null;
            fixture.Choose(3);
            using var verify = fixture.Context.CreateChar();
            Assert.AreEqual(1U, verify.CharacterFlags.Get(1)[703]);
            Assert.IsFalse(verify.CharacterFlags.Get(1).ContainsKey(702));
        }

        [TestMethod]
        public void AConsumedChoiceCannotExecuteAnotherBranch()
        {
            using var fixture = new DialogueFixture();
            fixture.Open();
            fixture.Choose(1);
            fixture.Context.Drain();

            fixture.Choose(2);
            fixture.Choose(3);
            fixture.Open();
            fixture.Choose(2);

            using var unit = fixture.Context.CreateChar();
            CollectionAssert.AreEquivalent(new uint[] { 701 }, unit.CharacterFlags.Get(1).Keys.ToArray());
            Assert.AreEqual(0, fixture.Context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public async Task CompetingOpenedSessionsCommitOnlyOneChoice()
        {
            using var fixture = new DialogueFixture();
            var second = fixture.Context.CreateCompetingClient();
            fixture.Open();
            new NpcManager(fixture.Context, fixture.Context.Manager).RequestNpcConverse(second,
                new RequestNPCConversePacket { EntityId = fixture.Npc.EntityId });
            Assert.IsTrue(MissionTestContext.Drain(second).OfType<ConversePacket>().Single()
                .ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice));

            var results = await Task.WhenAll(
                Task.Run(() => fixture.Context.Manager.TryPerformNpcChoice(fixture.Context.Client,
                    fixture.Npc.EntityId, 339, 8, 1, 1)),
                Task.Run(() => fixture.Context.Manager.TryPerformNpcChoice(second,
                    fixture.Npc.EntityId, 339, 8, 1, 2)));

            Assert.AreEqual(1, results.Count(result => result));
            using var unit = fixture.Context.CreateChar();
            Assert.AreEqual(1, unit.CharacterFlags.Get(1).Count);
            Assert.AreEqual(1, fixture.Context.Drain().Concat(MissionTestContext.Drain(second))
                .OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MigratedNpcAndObjectChoicesCommitFlagsAndSceneInputBeforePublication(bool conversationObject)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionDialogueTestContent.Install(harness.WorldContext, conversationObject);
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            var giver = harness.AddNpc(77);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 339));
            var target = DialogueTarget(harness, conversationObject);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
            Assert.IsTrue(harness.Drain().OfType<ConversePacket>().Single().ConvoDataDict
                .ContainsKey(ConversationType.ObjectiveChoice));
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var checkedTransaction = false;
            harness.Context.BeforeSave = database =>
            {
                if (!database.ChangeTracker.Entries<MissionSceneMessageEntry>().Any(entry =>
                    entry.State == EntityState.Added && entry.Entity.SequenceId == 202))
                    return;
                checkedTransaction = true;
                Assert.IsNotNull(database.Database.CurrentTransaction);
                Assert.AreEqual((byte)MissionObjectiveState.Completed,
                    database.CharacterMissionObjectiveEntries.Single(entry =>
                        entry.CharacterId == harness.Client.Player.Id && entry.MissionId == 339 && entry.ObjectiveId == 8)
                        .ObjectiveState);
                Assert.AreEqual(1U, database.Set<CharacterFlagEntry>().Single(entry =>
                    entry.CharacterId == harness.Client.Player.Id && entry.FlagId == 702).Value);
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[339].Objectives[8].State);
                Assert.IsFalse(harness.Drain().OfType<ObjectiveCompletedPacket>().Any());
            };

            npcs.PerformNPCChoice(harness.Client, new()
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 2
            });

            Assert.IsTrue(checkedTransaction);
            Assert.AreEqual(before + 20, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsNull(harness.Client.MissionConversation);
            using (var unit = harness.Context.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 339).Single();
                Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(scene.RunId)
                    .Count(message => message.SequenceId == 202 && message.Status == "Handled"));
                Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[702]);
                Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[802]);
                Assert.IsTrue(unit.CharacterMissions.Runtime.HasReceipt(scene.RunId, 0, "choice-reward-2"));
            }
            var packets = harness.Drain().ToList();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
            Assert.IsTrue((bool)packets.OfType<ConversePacket>().Single().ConvoDataDict[ConversationType.EndConversation]);
            Assert.IsTrue(packets.FindIndex(packet => packet is PlayerFlagsPacket) <
                packets.FindIndex(packet => packet is ObjectiveCompletedPacket));
            npcs.PerformNPCChoice(harness.Client, new()
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 3
            });
            Assert.AreEqual(before + 20, harness.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void NativeChoiceCommitsFlagAndAssignmentItemAtomicallyAndRestoresBothOnReconnect(
            bool conversationObject, bool failItemWrite)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallItemDialogue(harness, conversationObject, choice: true, consume: false);
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.AddNpc(77).EntityId, 339));
            var assignment = harness.Client.Player.Missions[339];
            var target = DialogueTarget(harness, conversationObject);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
            harness.Drain();
            var session = harness.Client.MissionConversation;
            var inventory = harness.Client.Player.Inventory.PersonalInventory.ToArray();
            var observedItemFlush = false;
            harness.Context.BeforeSave = database =>
            {
                Assert.IsNotNull(database.Database.CurrentTransaction);
                Assert.IsFalse(harness.Client.Player.PlayerFlags.ContainsKey(701));
                Assert.AreEqual(MissionObjectiveState.Incomplete, assignment.Objectives[8].State);
                CollectionAssert.AreEqual(inventory, harness.Client.Player.Inventory.PersonalInventory);
                Assert.IsEmpty(harness.Drain(), "Flag, objective and item publication must wait for commit.");
            };
            harness.Context.AfterSave = database =>
            {
                if (!database.ItemEntries.Local.Any(item => item.ItemTemplateId == 28))
                    return;
                observedItemFlush = true;
                if (failItemWrite)
                    throw new DbUpdateException("Injected selected-choice item flush failure.");
            };
            var request = new PerformNPCChoicePacket
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 1
            };

            npcs.PerformNPCChoice(harness.Client, request);

            Assert.IsTrue(observedItemFlush);
            if (failItemWrite)
            {
                Assert.AreSame(session, harness.Client.MissionConversation);
                Assert.AreEqual(MissionObjectiveState.Incomplete, assignment.Objectives[8].State);
                using (var unit = harness.Context.CreateChar())
                {
                    Assert.IsFalse(unit.CharacterFlags.Get(harness.Client.Player.Id).ContainsKey(701));
                    Assert.IsEmpty(unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id));
                    Assert.IsNull(unit.CharacterMissionItems.GetReceipt(
                        harness.Client.Player.Id, assignment.AssignmentId, "choice-key"));
                }
                CollectionAssert.AreEqual(inventory, harness.Client.Player.Inventory.PersonalInventory);
                Assert.IsEmpty(harness.Drain());
                harness.Context.AfterSave = null;
                npcs.PerformNPCChoice(harness.Client, request);
            }
            harness.Context.BeforeSave = null;
            harness.Context.AfterSave = null;
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[339].Objectives[8].State);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[701]);
            var packets = harness.Drain();
            CollectionAssert.AreEqual(new uint[] { 701 }, packets.OfType<PlayerFlagsPacket>().Single().PlayerFlagIds.ToArray());
            Assert.HasCount(1, packets.OfType<Rasa.Packets.Inventory.Server.InventoryAddItemPacket>().ToArray());
            uint itemId;
            using (var unit = harness.Context.CreateChar())
            {
                var owned = unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Single();
                Assert.AreEqual(assignment.AssignmentId, owned.AssignmentId);
                Assert.AreEqual(assignment.Generation, owned.Generation);
                Assert.AreEqual(1U, owned.Quantity);
                Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[701]);
                Assert.IsNotNull(unit.CharacterMissionItems.GetReceipt(
                    harness.Client.Player.Id, assignment.AssignmentId, "choice-key"));
                itemId = owned.ItemId;
            }
            npcs.PerformNPCChoice(harness.Client, request);
            Assert.IsEmpty(harness.Drain());
            harness.ReconnectFresh();
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[701]);
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.Id == itemId);
            Assert.AreEqual(assignment.AssignmentId, item.MissionOwnership.AssignmentId);
            Assert.AreEqual(assignment.Generation, item.MissionOwnership.Generation);
            Assert.AreEqual(1U, item.StackSize);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[339].Objectives[8].State);
        }

        public static IEnumerable<object[]> LateItemDialogueCases()
        {
            foreach (var conversationObject in new[] { false, true })
                foreach (var choice in new[] { false, true })
                    foreach (var consume in new[] { false, true })
                        foreach (var change in new[] { "disabled", "removed", "moved", "session", "valid" })
                            yield return new object[] { conversationObject, choice, consume, change };
        }

        [TestMethod]
        [DynamicData(nameof(LateItemDialogueCases))]
        public void ItemDialogueRevalidatesSourceAfterFinalInventoryReads(
            bool conversationObject, bool choice, bool consume, string change)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallItemDialogue(harness, conversationObject, choice, consume);
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.AddNpc(77).EntityId, 339));
            var target = DialogueTarget(harness, conversationObject);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            Open();
            var session = harness.Client.MissionConversation;
            var assignment = harness.Context.ReadMission(339);
            var before = harness.Context.ReadRewardTotals();
            var inventory = harness.Client.Player.Inventory.PersonalInventory.ToArray();
            var position = conversationObject ? session.Target.Object.Position : session.Target.Creature.Position;
            var saved = false;
            var itemWriteSeen = false;
            var crossed = false;
            harness.Context.AfterSave = database =>
            {
                var itemReceiptSaved = database.ChangeTracker.Entries<CharacterMissionItemReceiptEntry>().Any(entry =>
                    entry.State == EntityState.Unchanged && entry.Entity.OperationKey == "choice-key");
                var owned = database.ChangeTracker.Entries<CharacterMissionItemEntry>().Count(entry =>
                    entry.State == EntityState.Unchanged && entry.Entity.AssignmentId == assignment.AssignmentId);
                saved = itemWriteSeen && itemReceiptSaved && owned == (consume ? 0 : 1);
            };
            harness.Context.AfterCommand = sql =>
            {
                if (sql.Contains(consume ? "DELETE FROM \"items\"" : "INSERT INTO \"character_mission_item\"",
                    StringComparison.Ordinal))
                    itemWriteSeen = true;
                if (!saved || crossed || !sql.StartsWith("SELECT", StringComparison.Ordinal) ||
                    !sql.Contains("FROM \"character_mission_item\"", StringComparison.Ordinal))
                    return;
                crossed = true;
                switch (change)
                {
                    case "disabled":
                        if (conversationObject) session.Target.Object.IsEnabled = false;
                        else session.Target.Creature.IsInteractable = false;
                        break;
                    case "removed": EntityManager.Instance.UnregisterEntity(target); break;
                    case "moved":
                        if (conversationObject) session.Target.Object.Position += new Vector3(6, 0, 0);
                        else session.Target.Creature.Position += new Vector3(6, 0, 0);
                        break;
                    case "session": harness.Client.InvalidateMissionSession(); break;
                }
            };
            try { Apply(); }
            finally
            {
                harness.Context.AfterSave = null;
                harness.Context.AfterCommand = null;
            }

            Assert.IsTrue(crossed, "Invalidate only in the inventory participant's post-save validation reader.");
            if (change != "valid")
            {
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    harness.Context.ReadProgress(339).Missions[339].Objectives[8].State);
                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[339].Objectives[8].State);
                Assert.IsFalse(harness.Context.ReadMission(339).Completeable);
                Assert.IsFalse(harness.Client.Player.Missions[339].Completeable);
                Assert.AreEqual(assignment.AssignmentId, harness.Context.ReadMission(339).AssignmentId);
                Assert.AreEqual(assignment.Generation, harness.Context.ReadMission(339).Generation);
                Assert.AreEqual(before, harness.Context.ReadRewardTotals());
                CollectionAssert.AreEqual(inventory, harness.Client.Player.Inventory.PersonalInventory);
                using (var unit = harness.Context.CreateChar())
                {
                    Assert.IsFalse(unit.CharacterFlags.Get(harness.Client.Player.Id).ContainsKey(701));
                    Assert.AreEqual(consume ? 1 : 0, unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
                    Assert.IsNull(unit.CharacterMissionItems.GetReceipt(
                        harness.Client.Player.Id, assignment.AssignmentId, "choice-key"));
                    Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                }
                Assert.IsEmpty(harness.Drain(), "Rollback cannot publish objective, flag, item or choice success.");
                Assert.IsNull(harness.Client.MissionConversation);
                if (conversationObject)
                {
                    session.Target.Object.IsEnabled = true;
                    session.Target.Object.Position = position;
                }
                else
                {
                    session.Target.Creature.IsInteractable = true;
                    session.Target.Creature.Position = position;
                }
                if (change == "removed")
                    EntityManager.Instance.RegisterEntity(target, conversationObject ? EntityType.Object : EntityType.Creature);
                Open();
                Assert.AreNotSame(session, harness.Client.MissionConversation);
                Apply();
            }

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[339].Objectives[8].State);
            Assert.IsTrue(harness.Client.Player.Missions[339].Completeable);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[701]);
            Assert.AreEqual(before.ItemCount + (consume ? -1 : 1), harness.Context.ReadRewardTotals().ItemCount);
            using (var unit = harness.Context.CreateChar())
            {
                var current = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 339);
                Assert.AreEqual(assignment.AssignmentId, current.AssignmentId);
                Assert.AreEqual(assignment.Generation, current.Generation);
                Assert.IsTrue(current.Completeable);
                Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[701]);
                Assert.AreEqual(consume ? 0 : 1, unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
                var receipt = unit.CharacterMissionItems.GetReceipt(harness.Client.Player.Id, assignment.AssignmentId, "choice-key");
                Assert.IsNotNull(receipt);
                Assert.AreEqual(assignment.Generation, receipt.Generation);
            }
            var packets = harness.Drain();
            Assert.HasCount(1, packets.OfType<ObjectiveCompletedPacket>().ToArray());
            Assert.HasCount(1, packets.OfType<PlayerFlagsPacket>().ToArray());
            var after = harness.Context.ReadRewardTotals();
            Apply();
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
            Assert.IsEmpty(harness.Drain());

            void Open()
            {
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
                Assert.IsNotNull(harness.Client.MissionConversation);
                harness.Drain();
            }

            void Apply()
            {
                if (choice)
                    npcs.PerformNPCChoice(harness.Client, new()
                    { EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 1 });
                else
                    npcs.CompleteNPCObjective(harness.Client, new()
                    { EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1 });
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MigratedChoiceRollbackLeavesNoFlagSceneInputOrReward(bool conversationObject)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionDialogueTestContent.Install(harness.WorldContext, conversationObject);
            harness.Manager.LoadMissions();
            var giver = harness.AddNpc(77);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 339));
            var target = DialogueTarget(harness, conversationObject);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
            harness.Drain();
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var session = harness.Client.MissionConversation;
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected choice transaction failure.");

            npcs.PerformNPCChoice(harness.Client, new()
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 3
            });

            Assert.AreSame(session, harness.Client.MissionConversation);
            Assert.AreEqual(before, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, harness.Drain().Count);
            using (var unit = harness.Context.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 339).Single();
                Assert.IsFalse(unit.CharacterMissions.Runtime.Messages(scene.RunId).Any(message => message.SequenceId == 203));
                Assert.IsFalse(unit.CharacterMissions.Runtime.HasReceipt(scene.RunId, 0, "choice-reward-3"));
                Assert.IsFalse(unit.CharacterFlags.Get(harness.Client.Player.Id).ContainsKey(703));
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    unit.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, 339)[8].ObjectiveState);
            }
            harness.Context.AfterSave = null;
            npcs.PerformNPCChoice(harness.Client, new()
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 1
            });
            Assert.AreEqual(before + 10, harness.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompletionAndChoiceUseTheSameSceneAndFlagTransitionPlanner(bool choice)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionDialogueTestContent.Install(harness.WorldContext,
                kind: choice ? MissionDialogueKind.Choice : MissionDialogueKind.Completion);
            harness.Manager.LoadMissions();
            var giver = harness.AddNpc(77);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 339));
            var target = DialogueTarget(harness, false);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
            harness.Drain();
            var before = harness.Client.Player.Credits[CurencyType.Credits];

            Assert.IsTrue(choice
                ? harness.Manager.TryPerformNpcChoice(harness.Client, target, 339, 8, 1, 1)
                : harness.Manager.TryCompleteNpcObjective(harness.Client, target, 339, 8, 1));

            Assert.AreEqual(before + 10, harness.Client.Player.Credits[CurencyType.Credits]);
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[701]);
            Assert.AreEqual(1U, unit.CharacterFlags.Get(harness.Client.Player.Id)[801]);
            Assert.AreEqual((byte)MissionObjectiveState.Completed,
                unit.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, 339)[8].ObjectiveState);
        }

        [TestMethod]
        [DataRow("unopened")]
        [DataRow("object-binding")]
        [DataRow("source-revision")]
        [DataRow("source-generation")]
        [DataRow("source-ended")]
        [DataRow("assignment")]
        public void ObjectChoicesRejectUnopenedOrStaleSources(string invalid)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionDialogueTestContent.Install(harness.WorldContext, conversationObject: true);
            harness.Manager.LoadMissions();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.AddNpc(77).EntityId, 339));
            var target = DialogueTarget(harness, true);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            if (invalid != "unopened")
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = target });
            harness.Drain();
            if (invalid == "object-binding")
            {
                Assert.IsTrue(EntityManager.Instance.TryGetObject(target, out var obj));
                obj.MissionConversation = obj.MissionConversation with { NpcPackageId = 134 };
            }
            if (invalid is "source-revision" or "source-generation" or "source-ended" or "assignment")
            {
                using var unit = harness.Context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 339).Single();
                    if (invalid == "source-revision") scene.Release = "unversioned";
                    if (invalid == "source-generation") scene.Generation++;
                    if (invalid == "source-ended") scene.Status = "Ended";
                    if (invalid == "assignment")
                        unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 339).AssignmentId =
                            Guid.NewGuid().ToString("N");
                });
            }

            npcs.PerformNPCChoice(harness.Client, new()
            {
                EntityId = target, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = 1
            });

            using var verify = harness.Context.CreateChar();
            Assert.IsFalse(verify.CharacterFlags.Get(harness.Client.Player.Id).ContainsKey(701));
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                verify.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, 339)[8].ObjectiveState);
            Assert.AreEqual(0, harness.Drain().Count);
        }

        [TestMethod]
        public void SourceChangesDuringChoicePlanningRollBackTheSelectedBranch()
        {
            using var fixture = new DialogueFixture();
            fixture.Open();
            fixture.Context.AfterSave = _ => fixture.Npc.Npc.NpcPackageId = 134;

            fixture.Choose(2);

            using var unit = fixture.Context.CreateChar();
            Assert.AreEqual(0, unit.CharacterFlags.Get(1).Count);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                unit.CharacterMissionProgress.GetTracked(1, 339)[8].ObjectiveState);
            Assert.IsNull(fixture.Context.Client.MissionConversation);
            Assert.AreEqual(0, fixture.Context.Drain().Count);
        }

        [TestMethod]
        public void ChoiceSpawnGroupInputCommitsBeforeObjectivePublication()
        {
            using var fixture = new DialogueFixture(activateSpawnSequence: true);
            fixture.Open();
            var observedInput = false;
            fixture.Context.BeforeSave = database =>
            {
                if (!database.ChangeTracker.Entries<MissionSceneMessageEntry>().Any(entry =>
                    entry.State == EntityState.Added && entry.Entity.SequenceId == 202))
                    return;
                observedInput = true;
                Assert.IsNotNull(database.Database.CurrentTransaction);
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    fixture.Context.Client.Player.Missions[339].Objectives[8].State);
            };

            fixture.Choose(2);

            Assert.IsTrue(observedInput);
            using var unit = fixture.Context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(1, 339).Single();
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(scene.RunId)
                .Count(message => message.SequenceId == 202 && message.Status == "Handled"));
            Assert.AreEqual(1U, unit.CharacterFlags.Get(1)[702]);
            Assert.AreEqual(1U, unit.CharacterFlags.Get(1)[802]);
        }

        [TestMethod]
        public void RolledBackSpawnGroupChoiceCanRetryWithoutLosingItsInput()
        {
            using var fixture = new DialogueFixture(activateSpawnSequence: true);
            fixture.Open();
            var session = fixture.Context.Client.MissionConversation;
            fixture.Context.AfterSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneMessageEntry>().Any(entry => entry.Entity.SequenceId == 202))
                    throw new DbUpdateException("Injected queued spawn-group rollback.");
            };

            fixture.Choose(2);

            Assert.AreSame(session, fixture.Context.Client.MissionConversation);
            Assert.AreEqual(0, fixture.Context.Drain().Count);
            using (var unit = fixture.Context.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.Scenes(1, 339).Count);
                Assert.AreEqual(0, unit.CharacterFlags.Get(1).Count);
                Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                    unit.CharacterMissionProgress.GetTracked(1, 339)[8].ObjectiveState);
            }
            fixture.Context.AfterSave = null;
            fixture.Choose(2);
            using var verify = fixture.Context.CreateChar();
            Assert.AreEqual(1U, verify.CharacterFlags.Get(1)[802]);
        }

        [TestMethod]
        public void MissionDialoguePreservesVendorTrainerClanAndAuctionTopics()
        {
            using var fixture = new DialogueFixture();
            fixture.Npc.DbId = 501001;
            fixture.Npc.Npc.Vendor = new Vendor(586);
            fixture.Npc.Npc.NpcIsTrainer = true;
            fixture.Npc.Npc.NpcIsClanMaster = true;
            fixture.Npc.Npc.NpcIsAuctioneer = true;
            fixture.Context.Client.Player.Class = (uint)CharacterClass.Recruit;
            fixture.Context.Client.Player.Level = 5;

            var packet = fixture.Open();

            Assert.IsTrue(packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice));
            Assert.AreEqual(586U, ((List<uint>)packet.ConvoDataDict[ConversationType.Vending]).Single());
            var training = (TrainingConverse)packet.ConvoDataDict[ConversationType.Training];
            Assert.IsTrue(training.CanTrain);
            Assert.AreEqual(5, training.DialogId);
            Assert.AreEqual(true, packet.ConvoDataDict[ConversationType.Clan]);
            Assert.AreEqual(true, packet.ConvoDataDict[ConversationType.Auctioneer]);
            Assert.IsTrue(MissionTestContext.Encode(packet).Length > 0);
        }

        private static void InstallItemDialogue(BootcampRuntimeTestHarness.Harness harness,
            bool conversationObject, bool choice, bool consume)
        {
            var scene = MissionDialogueTestContent.Install(harness.WorldContext, conversationObject,
                choice ? MissionDialogueKind.Choice : MissionDialogueKind.Completion);
            harness.Context.AddRewardTemplate(28, 3147);
            scene.Items = new()
            {
                new MissionItemBinding("choice-key", 28, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove)
            };
            if (consume)
                scene.AcceptanceItems = new() { new IssueMissionItemIntent("starter-key", 339, "choice-key", 28, 1) };
            harness.WorldContext.MissionActionEntries.RemoveRange(
                harness.WorldContext.MissionActionEntries.Where(action => action.MissionId == 339 &&
                    action.Kind == MissionActionKind.StartScenario));
            CharacterIntent item = consume
                ? new ConsumeMissionItemIntent("choice-key", 339, "choice-key", 1, MissionItemScope.AssignmentIssued)
                : new IssueMissionItemIntent("choice-key", 339, "choice-key", 28, 1);
            harness.WorldContext.MissionActionEntries.Add(new MissionActionEntry
            {
                MissionId = 339, ContentRevision = MissionDialogueTestContent.Revision,
                ObjectiveId = 8, TransitionId = 101, ActionId = 4, Sequence = 4,
                Kind = consume ? MissionActionKind.ConsumeMissionItem : MissionActionKind.IssueMissionItem,
                ItemIntentJson = JsonSerializer.Serialize(item, MissionContentCodec.Options),
                Comment = "Selected branch's assignment item"
            });
            MissionDialogueTestContent.SaveScene(harness.WorldContext, scene);
        }

        private static ulong DialogueTarget(BootcampRuntimeTestHarness.Harness harness, bool conversationObject)
        {
            if (conversationObject)
            {
                var obj = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "dialogue-object");
                Assert.IsNotNull(obj);
                harness.MovePlayerTo(obj);
                return obj.EntityId;
            }
            var npc = harness.AddNpc(500, 586);
            harness.MovePlayerTo(npc);
            return npc.EntityId;
        }

        private sealed class DialogueFixture : IDisposable
        {
            private readonly FieldInfo _singleton = typeof(NpcManager).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previous;
            private readonly PacketRouter<ClientPacketHandler, GameOpcode> _router = new();
            private readonly ClientPacketHandler _handler = new();
            internal MissionTestContext Context { get; }
            internal Creature Npc { get; }

            internal DialogueFixture(MissionDialogueKind kind = MissionDialogueKind.Choice, bool failSecondChoice = false,
                bool activateSpawnSequence = false)
            {
                var transitions = Enumerable.Range(1, 3).Select(index =>
                    new MissionObjectiveExecutableTransition((uint)(100 + index), (uint)index,
                        failSecondChoice && index == 2 ? MissionObjectiveState.Failed : MissionObjectiveState.Completed,
                        new[] { new MissionObjectiveConversation(586, 1, MissionObjectiveConversationType.Completion) },
                        null, null, null, new MissionActionDefinition[]
                        {
                            new() { Kind = MissionActionKind.ActivateObjective, Sequence = 1, TargetObjectiveId = (uint)(8 + index) },
                            new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 2, PlayerFlagId = (uint)(700 + index), PlayerFlagValue = 1 }
                        }.Concat(activateSpawnSequence ? new MissionActionDefinition[]
                        {
                            new() { Kind = MissionActionKind.ActivateSpawnGroup, Sequence = 3, SpawnGroupId = (uint)(100 + index) }
                        } : Array.Empty<MissionActionDefinition>()))).ToArray();
                var objectives = new[] { Objective(8, MissionObjectiveState.Incomplete, transitions, required: !failSecondChoice) }
                    .Concat(Enumerable.Range(9, 3).Select(id => Objective((uint)id, MissionObjectiveState.Inactive)));
                var mission = new Mission(339, "Native dialogue fixture", 4318, 77, 88, 1, 1, 1, false, false,
                    objectives, true, contentRevision: "dialogue-v1", dialogue: new[]
                    {
                        new MissionDialogueTopicDefinition(8, 586, 1, kind,
                            choices: kind == MissionDialogueKind.Choice
                                ? new Dictionary<int, uint> { [1] = 101, [2] = 102, [3] = 103 } : null)
                    });
                Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [339] = mission });
                Context.SeedMission(1, 339, (uint)MissionState.Active, false);
                Context.ReloadPlayerMissions();
                if (activateSpawnSequence)
                {
                    var scene = new MissionSceneDefinition { Script = "data.sequence" };
                    for (uint index = 1; index <= 3; index++)
                    {
                        scene.Names[$"spawn-group-{100 + index}"] = 200 + index;
                        scene.Sequences[200 + index] = new()
                        {
                            Character = new() { new SetCharacterFlagIntent($"spawn-{index}", 800 + index, 1) }
                        };
                    }
                    Context.Manager.Scenes.Bind(339, scene.Script, scene.Bindings("dialogue-v1"));
                }
                Npc = Context.AddNpc(500, npcPackageId: 586);
                _previous = _singleton.GetValue(null);
                _singleton.SetValue(null, new NpcManager(Context, Context.Manager));
                _handler.RegisterClient(Context.Client);
                Context.Drain();
            }

            internal ConversePacket Open()
            {
                _router.RoutePacket(_handler, new RequestNPCConversePacket { EntityId = Npc.EntityId });
                return Context.Drain().OfType<ConversePacket>().Single();
            }

            internal void Choose(int index) => Route(new PerformNPCChoicePacket
            {
                EntityId = Npc.EntityId, MissionId = 339, ObjectiveId = 8, PlayerFlagId = 1, ChoiceIdx = index
            });

            internal void Route(PerformNPCChoicePacket request) => _router.RoutePacket(_handler, request);

            public void Dispose()
            {
                _singleton.SetValue(null, _previous);
                Context.Dispose();
            }

            private static MissionObjectiveDefinition Objective(uint id, MissionObjectiveState state,
                IReadOnlyList<MissionObjectiveExecutableTransition> transitions = null, bool required = true) =>
                new(id, 4318, 4318, Array.Empty<uint?>(), id, state, required, null, null,
                    transitions?.SelectMany(transition => transition.Conversations),
                    Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                    executableTransitions: transitions);
        }

        private static byte[] ChoicePayload(Action<PythonWriter> index, int tupleSize = 5)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(tupleSize);
            writer.WriteULong(0x100000005UL);
            writer.WriteUInt(339);
            writer.WriteUInt(8);
            writer.WriteUInt(1);
            index(writer);
            return stream.ToArray();
        }

        private static PerformNPCChoicePacket DecodeChoice(byte[] payload)
        {
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(payload)));
            var packet = new PerformNPCChoicePacket();
            packet.Read(reader);
            return packet;
        }

        private static void AssertInvalidConversation(ConversationType kind, object value)
        {
            var packet = new ConversePacket(new Dictionary<ConversationType, object>
            {
                [ConversationType.Greeting] = 19,
                [kind] = value
            });
            using var stream = new MemoryStream();
            stream.WriteByte(0xFF);
            using var writer = new PythonWriter(new BinaryWriter(stream));
            Assert.ThrowsExactly<InvalidDataException>(() => packet.Write(writer));
            CollectionAssert.AreEqual(new byte[] { 0xFF }, stream.ToArray(),
                "Validation must precede the dictionary header and every key/value.");
        }
    }
}
