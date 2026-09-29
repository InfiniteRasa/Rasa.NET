using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Definitions;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionGmCompletionTests
    {
        [TestMethod]
        public void ForceCompleteObjectiveReadsAnIntOrLongUserAndRejectsOtherShapes()
        {
            foreach (var wide in new[] { false, true })
            {
                var packet = Decode(writer =>
                {
                    writer.WriteTuple(3);
                    if (wide)
                        writer.WriteULong(2);
                    else
                        writer.WriteUInt(2);
                    writer.WriteUInt(321);
                    writer.WriteUInt(1);
                });
                Assert.AreEqual(2UL, packet.UserId);
                Assert.AreEqual(321U, packet.MissionId);
                Assert.AreEqual(1U, packet.ObjectiveId);
                Assert.AreEqual(GameOpcode.ForceCompleteObjective, packet.Opcode);
                Assert.AreEqual(639, (int)packet.Opcode);
            }
            foreach (var size in new[] { 2, 4 })
                Assert.ThrowsExactly<InvalidDataException>(() => Decode(writer => writer.WriteTuple(size)));
            Assert.ThrowsExactly<InvalidDataException>(() => Decode(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteString("2");
            }));
            Assert.AreEqual(typeof(ForceCompleteObjectivePacket),
                new PacketRouter<ClientPacketHandler, GameOpcode>().GetPacketType(GameOpcode.ForceCompleteObjective));
        }

        [TestMethod]
        public void TheUserMissionsAckCarriesTheLogAsTheClientReadsIt()
        {
            using var context = Context();
            Accept(context);
            var ack = new GmShowUserMissionsAckPacket(2, "Name Family",
                context.Manager.BuildStatusSnapshot(context.Client.Player));
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(ack))));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(PythonType.UnicodeString, reader.PeekType());
            reader.ReadUnicodeString();
            Assert.AreEqual(1, reader.ReadDictionary(), "missionInfo is {missionId: info}, not one info.");
            Assert.AreEqual(321U, reader.ReadUInt());
        }

        [TestMethod]
        public void AForcedCompletionRunsTheObjectivesActionsAndMakesTheMissionCompleteable()
        {
            using var context = Context();
            Accept(context);
            var mission = context.Client.Player.Missions[321];
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[2].State);

            Assert.IsTrue(context.Manager.TryForceCompleteObjective(context.Client, 321, 1, "test"));

            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[2].State,
                "The objective's authored activation ran.");
            Assert.IsFalse(mission.Completeable, "Objective 2 is still required.");
            Assert.AreEqual(0U, mission.Objectives[1].Counters[0], "Counters are left where they were.");
            Assert.IsTrue(context.Drain().Count > 0, "The player was told.");

            Assert.IsTrue(context.Manager.TryForceCompleteObjective(context.Client, 321, 2, "test"));
            Assert.IsTrue(mission.Completeable);
            Assert.AreEqual(MissionState.Active, mission.State);
            using var unit = context.CreateChar();
            Assert.IsTrue(context.ReadMission(321).Completeable);
            var durable = unit.CharacterMissionProgress.GetTracked(1, 321);
            Assert.AreEqual((byte)MissionObjectiveState.Completed, durable[1].ObjectiveState);
            Assert.AreEqual((byte)MissionObjectiveState.Completed, durable[2].ObjectiveState);
        }

        [TestMethod]
        [DataRow("again")]
        [DataRow("inactive")]
        [DataRow("unknown-objective")]
        [DataRow("unknown-mission")]
        [DataRow("not-assigned")]
        public void OnlyAnIncompleteObjectiveOfAnActiveMissionCanBeForced(string change)
        {
            using var context = Context();
            if (change != "not-assigned")
                Accept(context);
            if (change == "again")
                Assert.IsTrue(context.Manager.TryForceCompleteObjective(context.Client, 321, 1, "test"));
            var (missionId, objectiveId) = change switch
            {
                "inactive" => (321U, 2U),
                "unknown-objective" => (321U, 9U),
                "unknown-mission" => (999U, 1U),
                _ => (321U, 1U)
            };
            Assert.IsFalse(context.Manager.TryForceCompleteObjective(context.Client, missionId, objectiveId, "test"));
            if (change == "inactive")
                Assert.AreEqual(MissionObjectiveState.Inactive, context.Client.Player.Missions[321].Objectives[2].State);
        }

        [TestMethod]
        public void TheGmButtonCompletesForAGameMasterAndSendsTheLogAgain()
        {
            using var context = Context();
            var target = context.CreateAdditionalClient(2);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(target, context.AddNpc(77).EntityId, 321));
            context.Client.AccountEntry.Level = (byte)GmLevel.GameMaster;
            context.Drain();

            Route(context, context.Client, target, new ForceCompleteObjectivePacket
                { UserId = target.Player.Id, MissionId = 321, ObjectiveId = 1 });

            Assert.AreEqual(MissionObjectiveState.Completed, target.Player.Missions[321].Objectives[1].State);
            var ack = context.Drain().OfType<GmShowUserMissionsAckPacket>().Single();
            Assert.AreEqual(target.Player.Id, ack.UserId);
            Assert.IsTrue(ack.MissionInfo.ContainsKey(321));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(4)]
        public void BelowGameMasterTheButtonDoesNothing(int level)
        {
            using var context = Context();
            var target = context.CreateAdditionalClient(2);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(target, context.AddNpc(77).EntityId, 321));
            context.Client.AccountEntry.Level = (byte)level;
            context.Drain();

            Route(context, context.Client, target, new ForceCompleteObjectivePacket
                { UserId = target.Player.Id, MissionId = 321, ObjectiveId = 1 });

            Assert.AreEqual(MissionObjectiveState.Incomplete, target.Player.Missions[321].Objectives[1].State);
            Assert.IsEmpty(context.Drain().OfType<GmShowUserMissionsAckPacket>().ToArray());
        }

        private static void Accept(MissionTestContext context) =>
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 321));

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = TwoObjectives() },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) });

        /// <summary>Objective 1 counts kills of creature 55 and activates objective 2; both are required.</summary>
        private static Mission TwoObjectives() =>
            new(321, "GM completion fixture", 321, 77, 88, 1, 1, 2, false, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { 1003, null, null }, 0,
                        MissionObjectiveState.Incomplete, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), new uint[] { 2 },
                        Array.Empty<MissionIndicator>(),
                        MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5)),
                    new MissionObjectiveDefinition(2, 2001, 2002, new uint?[] { null, null, null }, 1,
                        MissionObjectiveState.Inactive, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(),
                        MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 56))
                }, enableOperational: true);

        private static void Route(MissionTestContext context, Client gm, Client target, ClientPythonPacket packet)
        {
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            GmMissionCommands.OnlineClients = () => new[] { gm, target };
            try
            {
                var handler = new ClientPacketHandler();
                handler.RegisterClient(gm);
                new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler, packet);
            }
            finally
            {
                GmMissionCommands.OnlineClients = null;
                singleton.SetValue(null, previous);
            }
        }

        private static ForceCompleteObjectivePacket Decode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                write(new PythonWriter(writer));
            stream.Position = 0;
            var packet = new ForceCompleteObjectivePacket();
            packet.Read(new PythonReader(new BinaryReader(stream)));
            return packet;
        }
    }
}
