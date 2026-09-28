using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class MissionProtocolTests
    {
        [TestMethod]
        public void NativeRadioCompletionIsRegisteredWithoutAnNpcField()
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            var packetType = new PacketRouter<ClientPacketHandler, GameOpcode>()
                .GetPacketType(GameOpcode.CompleteRadioMission);
            Assert.AreEqual(432, (int)GameOpcode.CompleteRadioMission);
            Assert.AreEqual("Rasa.Packets.MapChannel.Client.CompleteRadioMissionPacket", packetType?.FullName);
        }

        [TestMethod]
        public void NullableMissionFieldsAcceptNoneOrIntAndRejectBoolLongAndWrongTupleSize()
        {
            var none = Decode<CompleteNPCMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteULong(55);
                writer.WriteUInt(321);
                writer.WriteNoneStruct();
                writer.WriteNoneStruct();
            }));
            Assert.IsNull(none.SelectionIdx);
            Assert.IsNull(none.Rating);

            var integers = Decode<RewardNPCMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteULong(55);
                writer.WriteUInt(321);
                writer.WriteInt(0);
                writer.WriteInt(2);
            }));
            Assert.AreEqual(0, integers.SelectionIdx);
            Assert.AreEqual(2, integers.Rating);

            var assignment = Decode<AssignNPCMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteULong(55);
                writer.WriteUInt(321);
            }));
            Assert.AreEqual(55UL, assignment.NpcEntityId);
            Assert.AreEqual(321U, assignment.MissionId);

            var radio = Decode<AssignRadioMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(1);
                writer.WriteUInt(1990);
            }));
            Assert.AreEqual(1990U, radio.MissionId);

            var radioCompletion = Decode<CompleteRadioMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteUInt(731);
                writer.WriteInt(1);
                writer.WriteNoneStruct();
            }));
            Assert.AreEqual(731U, radioCompletion.MissionId);
            Assert.AreEqual(1, radioCompletion.SelectionIdx);
            Assert.IsNull(radioCompletion.Rating);
            var ratedRadio = Decode<CompleteRadioMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteUInt(731);
                writer.WriteNoneStruct();
                writer.WriteInt(2);
            }));
            Assert.IsNull(ratedRadio.SelectionIdx);
            Assert.AreEqual(2, ratedRadio.Rating);

            var objective = Decode<CompleteNPCObjectivePacket>(WritePayload(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteULong(55);
                writer.WriteUInt(321);
                writer.WriteUInt(7);
                writer.WriteUInt(1);
            }));
            Assert.AreEqual(55UL, objective.EntityId);
            Assert.AreEqual(321U, objective.MissionId);
            Assert.AreEqual(7U, objective.ObjectiveId);
            Assert.AreEqual(1U, objective.PlayerFlagId);

            var abandonment = Decode<AbandonMissionPacket>(WritePayload(writer =>
            {
                writer.WriteTuple(1);
                writer.WriteUInt(321);
            }));
            Assert.AreEqual(321U, abandonment.MissionId);

            foreach (var invalidNullable in new Action<PythonWriter>[]
            {
                writer => writer.WriteTrueStruct(),
                writer => writer.WriteZeroStruct(),
                writer => writer.WriteLong(2)
            })
            {
                Assert.ThrowsExactly<InvalidDataException>(() =>
                    Decode<CompleteNPCMissionPacket>(WritePayload(writer =>
                    {
                        writer.WriteTuple(4);
                        writer.WriteULong(55);
                        writer.WriteUInt(321);
                        invalidNullable(writer);
                        writer.WriteNoneStruct();
                    })));
                Assert.ThrowsExactly<InvalidDataException>(() =>
                    Decode<RewardNPCMissionPacket>(WritePayload(writer =>
                    {
                        writer.WriteTuple(4);
                        writer.WriteULong(55);
                        writer.WriteUInt(321);
                        writer.WriteNoneStruct();
                        invalidNullable(writer);
                    })));
                foreach (var invalidSelection in new[] { true, false })
                    Assert.ThrowsExactly<InvalidDataException>(() =>
                        Decode<CompleteRadioMissionPacket>(WritePayload(writer =>
                        {
                            writer.WriteTuple(3);
                            writer.WriteUInt(731);
                            if (invalidSelection)
                                invalidNullable(writer);
                            else
                                writer.WriteNoneStruct();
                            if (invalidSelection)
                                writer.WriteNoneStruct();
                            else
                                invalidNullable(writer);
                        })));
            }

            AssertWrongTuple<AssignNPCMissionPacket>(1);
            AssertWrongTuple<AssignRadioMissionPacket>(0);
            AssertWrongTuple<AssignRadioMissionPacket>(2);
            AssertWrongTuple<CompleteNPCObjectivePacket>(3);
            AssertWrongTuple<CompleteNPCMissionPacket>(3);
            AssertWrongTuple<CompleteRadioMissionPacket>(2);
            AssertWrongTuple<CompleteRadioMissionPacket>(4);
            AssertWrongTuple<RewardNPCMissionPacket>(3);
            AssertWrongTuple<AbandonMissionPacket>(2);
        }

        [TestMethod]
        public void RecoveredMissionRequestsExposeExactOpcodesAndHandlers()
        {
            var packets = new (ClientPythonPacket Packet, GameOpcode Opcode, int Value, Type Type)[]
            {
                (new AbandonMissionPacket(), GameOpcode.AbandonMission, 392, typeof(AbandonMissionPacket)),
                (new AssignNPCMissionPacket(), GameOpcode.AssignNPCMission, 407, typeof(AssignNPCMissionPacket)),
                (new AssignRadioMissionPacket(), GameOpcode.AssignRadioMission, 408, typeof(AssignRadioMissionPacket)),
                (new CompleteNPCMissionPacket(), GameOpcode.CompleteNPCMission, 430, typeof(CompleteNPCMissionPacket)),
                (new CompleteNPCObjectivePacket(), GameOpcode.CompleteNPCObjective, 431,
                    typeof(CompleteNPCObjectivePacket)),
                (new CompleteRadioMissionPacket(), GameOpcode.CompleteRadioMission, 432, typeof(CompleteRadioMissionPacket)),
                (new RewardNPCMissionPacket(), GameOpcode.RewardNPCMission, 540, typeof(RewardNPCMissionPacket))
            };
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();

            foreach (var (packet, opcode, value, type) in packets)
            {
                Assert.AreEqual(opcode, packet.Opcode);
                Assert.AreEqual(value, (int)opcode);
                Assert.AreEqual(type, router.GetPacketType(opcode));
            }
        }

        [TestMethod]
        public void RadioMissionOfferUsesTheSixFieldConversationContractInsteadOfMissionStatusInfo()
        {
            var info = new MissionInfo
            {
                MissionConstantData = new MissionConstantData { Level = 1, GroupType = 2 }
            };
            info.ItemRequired.Add(3147);
            var offer = new DispenseRadioMissionPacket(1990, info, true);
            var expected = WritePayload(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteUInt(1990);
                writer.WriteTuple(6);
                writer.WriteUInt(1);
                writer.WriteTuple(2);
                writer.WriteTuple(2);
                writer.WriteList(0);
                writer.WriteList(0);
                writer.WriteList(0);
                writer.WriteNoneStruct();
                writer.WriteList(1);
                writer.WriteInt(3147);
                writer.WriteList(0);
                writer.WriteUInt(2);
                writer.WriteTrueStruct();
            });
            Assert.AreEqual(GameOpcode.DispenseRadioMission, offer.Opcode);
            Assert.AreEqual(444, (int)offer.Opcode);
            CollectionAssert.AreEqual(expected, MissionTestContext.Encode(offer));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void CompleteMissionRequestClaimsRewardsWithoutAnotherAcceptStep(
            bool selectableReward,
            bool resumeSuccess)
        {
            const uint missionId = 429;
            using var context = MissionTestContext.WithObjectiveMission(missionId, selectableReward);
            var giver = context.AddNpc(77);
            var firstObjectiveNpc = context.AddNpc(500, npcPackageId: 700);
            var secondObjectiveNpc = context.AddNpc(501, npcPackageId: 701);
            var receiver = context.AddNpc(88);
            var before = context.ReadRewardTotals();
            var npcSingleton = typeof(NpcManager).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previousNpcManager = npcSingleton.GetValue(null);
            npcSingleton.SetValue(null, new NpcManager(context, context.Manager));
            try
            {
                var handler = new ClientPacketHandler();
                handler.RegisterClient(context.Client);
                var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
                if (resumeSuccess)
                {
                    context.SeedMission(context.Client.Player.Id, missionId, (uint)MissionState.Success, false);
                    context.ReloadPlayerMissions();
                    router.RoutePacket(handler, new AssignNPCMissionPacket
                    {
                        NpcEntityId = giver.EntityId,
                        MissionId = missionId
                    });
                    Assert.AreEqual(MissionState.Success, context.Client.Player.Missions[missionId].State);
                    Assert.AreEqual(before, context.ReadRewardTotals(),
                        "Accept Mission must never claim rewards for an existing mission.");
                }
                else
                {
                    router.RoutePacket(handler, new RequestNPCConversePacket { EntityId = giver.EntityId });
                    Assert.IsTrue(context.Drain().OfType<ConversePacket>().Single()
                        .ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
                    router.RoutePacket(handler, new AssignNPCMissionPacket
                    {
                        NpcEntityId = giver.EntityId,
                        MissionId = missionId
                    });
                    Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[missionId].State);
                    foreach (var (npc, objectiveId, flagId) in new[]
                    {
                        (firstObjectiveNpc, 5U, 11U),
                        (secondObjectiveNpc, 9U, 12U)
                    })
                    {
                        Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, npc.EntityId));
                        router.RoutePacket(handler, new CompleteNPCObjectivePacket
                        {
                            EntityId = npc.EntityId,
                            MissionId = missionId,
                            ObjectiveId = objectiveId,
                            PlayerFlagId = flagId
                        });
                    }
                    Assert.IsTrue(context.Client.Player.Missions[missionId].Completeable);
                    Assert.AreEqual(before, context.ReadRewardTotals());
                }
                context.Drain();
                router.RoutePacket(handler, new RequestNPCConversePacket { EntityId = receiver.EntityId });
                var conversation = context.Drain().OfType<ConversePacket>().Single();
                Assert.IsTrue(conversation.ConvoDataDict.ContainsKey(ConversationType.MissionComplete),
                    "An unrewarded mission must offer Complete Mission, including after reconnect.");
                Assert.IsFalse(conversation.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
                Assert.IsFalse(conversation.ConvoDataDict.ContainsKey(ConversationType.MissionReward));

                var request = Decode<CompleteNPCMissionPacket>(WritePayload(writer =>
                {
                    writer.WriteTuple(4);
                    writer.WriteULong(receiver.EntityId);
                    writer.WriteUInt(missionId);
                    if (selectableReward)
                        writer.WriteInt(0);
                    else
                        writer.WriteNoneStruct();
                    writer.WriteNoneStruct();
                }));
                Assert.AreEqual(selectableReward ? 0 : (int?)null, request.SelectionIdx);

                router.RoutePacket(handler, request);

                Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[missionId].State,
                    "Complete Mission must claim rewards, not leave a second turn-in step.");
                Assert.IsFalse(context.Client.Player.Missions[missionId].Completeable);
                Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(missionId).MissionState);
                Assert.IsFalse(context.ReadMission(missionId).Completeable);
                var after = context.ReadRewardTotals();
                Assert.AreEqual(before.Experience + context.Reward.Experience, after.Experience);
                Assert.AreEqual(before.ItemCount + (selectableReward ? 2 : 0), after.ItemCount);
                var completionPackets = context.Drain();
                Assert.AreEqual(1, completionPackets.OfType<MissionRewardedPacket>().Count());
                Assert.AreEqual(resumeSuccess ? 0 : 1, completionPackets.OfType<MissionCompletedPacket>().Count());
                Assert.AreEqual(0, completionPackets.OfType<MissionGainedPacket>().Count());
                var statuses = completionPackets.OfType<NPCConversationStatusPacket>().ToArray();
                Assert.AreEqual(4, statuses.Length);
                Assert.IsTrue(statuses.All(status => status.ConvoStatusId == ConversationStatus.None));

                router.RoutePacket(handler, new RequestNPCConversePacket { EntityId = receiver.EntityId });
                Assert.AreEqual(0, context.Drain().OfType<ConversePacket>().Single().ConvoDataDict.Count);
                context.ReloadPlayerMissions();
                router.RoutePacket(handler, request);
                router.RoutePacket(handler, new AssignNPCMissionPacket
                {
                    NpcEntityId = giver.EntityId,
                    MissionId = missionId
                });
                router.RoutePacket(handler, new RewardNPCMissionPacket
                {
                    EntityId = receiver.EntityId,
                    MissionId = missionId,
                    SelectionIdx = request.SelectionIdx
                });
                Assert.AreEqual(after, context.ReadRewardTotals());
                Assert.AreEqual(0, context.Drain().Count);
            }
            finally
            {
                npcSingleton.SetValue(null, previousNpcManager);
            }
        }

        [TestMethod]
        public void InactiveDefinitionsMakeEveryRecoveredRequestAStatePreservingNoOp()
        {
            const uint missionId = 1449;
            using var context = MissionTestContext.WithDatabaseDefinitions(missionId);
            context.SeedMission(context.Client.Player.Id, missionId, (uint)MissionState.Active, true);
            context.Client.Player.Missions[missionId] =
                new MissionLog(missionId, MissionState.Active, true);
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            var beforeRewards = context.ReadRewardTotals();
            context.ResetCharUnitCount();

            var missionSingleton = typeof(MissionApplication).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var npcSingleton = typeof(NpcManager).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previousMissionManager = missionSingleton.GetValue(null);
            var previousNpcManager = npcSingleton.GetValue(null);
            missionSingleton.SetValue(null, context.Manager);
            npcSingleton.SetValue(null, Activator.CreateInstance(
                typeof(NpcManager),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { context },
                culture: null));
            try
            {
                var handler = new ClientPacketHandler();
                handler.RegisterClient(context.Client);
                var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
                var requests = new ClientPythonPacket[]
                {
                    new AssignNPCMissionPacket { NpcEntityId = giver.EntityId, MissionId = missionId },
                    new CompleteNPCObjectivePacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = missionId,
                        ObjectiveId = 1,
                        PlayerFlagId = 1
                    },
                    new CompleteNPCMissionPacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = missionId,
                        SelectionIdx = 0
                    },
                    new RewardNPCMissionPacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = missionId,
                        SelectionIdx = 0
                    },
                    new AbandonMissionPacket { MissionId = missionId }
                };

                foreach (var request in requests)
                    router.RoutePacket(handler, request);

                foreach (var request in new ClientPythonPacket[]
                {
                    new AssignNPCMissionPacket { NpcEntityId = giver.EntityId, MissionId = 999 },
                    new CompleteNPCObjectivePacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = 999,
                        ObjectiveId = 1,
                        PlayerFlagId = 1
                    },
                    new CompleteNPCMissionPacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = 999,
                        SelectionIdx = 0
                    },
                    new RewardNPCMissionPacket
                    {
                        EntityId = receiver.EntityId,
                        MissionId = 999,
                        SelectionIdx = 0
                    },
                    new AbandonMissionPacket { MissionId = 999 }
                })
                    router.RoutePacket(handler, request);
            }
            finally
            {
                npcSingleton.SetValue(null, previousNpcManager);
                missionSingleton.SetValue(null, previousMissionManager);
            }

            Assert.AreEqual(beforeRewards, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[missionId].State);
            Assert.IsTrue(context.Client.Player.Missions[missionId].Completeable);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(missionId).MissionState);
            Assert.IsTrue(context.ReadMission(missionId).Completeable);
            Assert.AreEqual(0, context.CharUnitsCreated);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void ObjectiveServerPacketsUseExactClientTuples()
        {
            var missionInfo = new MissionInfo();
            AssertPacket(
                new ObjectiveRevealedPacket(5, 6, missionInfo),
                GameOpcode.ObjectiveRevealed,
                new byte[]
                {
                    0x83, 0x15, 0x16, 0x85, 0x10, 0x02, 0x86, 0x10, 0x10, 0x10,
                    0x02, 0x02, 0x82, 0x82, 0x70, 0x70, 0x70, 0x10, 0x70
                },
                reader =>
                {
                    Assert.AreEqual(3, reader.ReadTuple());
                    Assert.AreEqual(5U, reader.ReadUInt());
                    Assert.AreEqual(6U, reader.ReadUInt());
                    AssertMissionInfoHeader(reader);
                });
            AssertPacket(
                new ObjectiveActivatedPacket(5, 6),
                GameOpcode.ObjectiveActivated,
                new byte[] { 0x82, 0x15, 0x16 },
                AssertTwoFieldObjective);
            AssertPacket(
                new ObjectiveCompletedPacket(5, 6),
                GameOpcode.ObjectiveCompleted,
                new byte[] { 0x82, 0x15, 0x16 },
                AssertTwoFieldObjective);
            AssertPacket(
                new ObjectiveFailedPacket(5, 6),
                GameOpcode.ObjectiveFailed,
                new byte[] { 0x82, 0x15, 0x16 },
                AssertTwoFieldObjective);
            AssertPacket(
                new UpdateObjectiveCounterPacket(5, 6, 7, 8, 9, 10),
                GameOpcode.UpdateObjectiveCounter,
                new byte[] { 0x86, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A },
                reader =>
                {
                    Assert.AreEqual(6, reader.ReadTuple());
                    CollectionAssert.AreEqual(new uint[] { 5, 6, 7, 8, 9, 10 },
                        Enumerable.Range(0, 6).Select(_ => reader.ReadUInt()).ToArray());
                });
            AssertPacket(
                new UpdateObjectiveItemCounterPacket(5, 6, 7, 8, 9),
                GameOpcode.UpdateObjectiveItemCounter,
                new byte[] { 0x85, 0x15, 0x16, 0x17, 0x18, 0x19 },
                reader =>
                {
                    Assert.AreEqual(5, reader.ReadTuple());
                    CollectionAssert.AreEqual(new uint[] { 5, 6, 7, 8, 9 },
                        Enumerable.Range(0, 5).Select(_ => reader.ReadUInt()).ToArray());
                });
        }

        [TestMethod]
        public void MissionWireEnumsUseClientValues()
        {
            Assert.AreEqual(0, (int)MissionState.Active);
            Assert.AreEqual(1, (int)MissionState.Success);
            Assert.AreEqual(2, (int)MissionState.Failed);
            Assert.AreEqual(3, (int)MissionState.NotAssigned);
            Assert.AreEqual(4, (int)MissionState.Completed);

            Assert.AreEqual(0, (int)MissionObjectiveState.NotAssigned);
            Assert.AreEqual(1, (int)MissionObjectiveState.Incomplete);
            Assert.AreEqual(2, (int)MissionObjectiveState.Completed);
            Assert.AreEqual(3, (int)MissionObjectiveState.Failed);
            Assert.AreEqual(4, (int)MissionObjectiveState.Inactive);

            Assert.AreEqual(1, (int)MissionObjectiveConversationType.Completion);
            Assert.AreEqual(2, (int)MissionObjectiveConversationType.Reminder);
            Assert.AreEqual(3, (int)MissionObjectiveConversationType.ChoiceBody);
            Assert.AreEqual(4, (int)MissionObjectiveConversationType.Choice1);
            Assert.AreEqual(5, (int)MissionObjectiveConversationType.Choice2);
            Assert.AreEqual(6, (int)MissionObjectiveConversationType.Choice3);
        }

        private static byte[] WritePayload(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            using var writer = new PythonWriter(binary);
            write(writer);
            return stream.ToArray();
        }

        private static T Decode<T>(byte[] bytes) where T : ClientPythonPacket, new()
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new T();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return packet;
        }

        private static void AssertWrongTuple<T>(int tupleLength) where T : ClientPythonPacket, new()
        {
            var bytes = WritePayload(writer =>
            {
                writer.WriteTuple(tupleLength);
                for (var index = 0; index < tupleLength; index++)
                    writer.WriteNoneStruct();
            });
            Assert.ThrowsExactly<InvalidDataException>(() => Decode<T>(bytes));
        }

        private static void AssertPacket(
            ServerPythonPacket packet,
            GameOpcode opcode,
            byte[] expected,
            Action<PythonReader> read)
        {
            Assert.AreEqual(opcode, packet.Opcode);
            var bytes = MissionTestContext.Encode(packet);
            CollectionAssert.AreEqual(expected, bytes, packet.GetType().Name);
            using var stream = new MemoryStream(bytes);
            using var reader = new PythonReader(new BinaryReader(stream));
            read(reader);
            Assert.AreEqual(stream.Length, stream.Position, packet.GetType().Name);
        }

        private static void AssertTwoFieldObjective(PythonReader reader)
        {
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(5U, reader.ReadUInt());
            Assert.AreEqual(6U, reader.ReadUInt());
        }

        private static void AssertMissionInfoHeader(PythonReader reader)
        {
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual((int)MissionState.Active, reader.ReadInt());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.IsFalse(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadList());
        }
    }
}
