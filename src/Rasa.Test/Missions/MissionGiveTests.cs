using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Packets.Communicator.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Missions;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionGiveTests
    {
        [TestMethod]
        public void TheGiveMissionListIsATupleOfIdAndStatusPairs()
        {
            var packet = new QAGiveMissionAckPacket(new[] { (321U, MissionState.NotAssigned), (322U, MissionState.Active) });
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(321U, reader.ReadUInt());
            Assert.AreEqual(3, reader.ReadInt(), "MISSION_NOT_ASSIGNED");
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(322U, reader.ReadUInt());
            Assert.AreEqual(0, reader.ReadInt(), "MISSION_ACTIVE");
            Assert.AreEqual(503, (int)packet.Opcode);
        }

        [TestMethod]
        public void TheListCarriesEveryOperationalMissionWithThePlayersState()
        {
            using var context = Context();
            var before = context.Manager.GiveMissionList(context.Client.Player).ToArray();
            CollectionAssert.AreEqual(new[] { (321U, MissionState.NotAssigned), (322U, MissionState.NotAssigned) }, before,
                string.Join(", ", before));
            Assert.IsTrue(context.Manager.TryGiveMission(context.Client, 322, "test"));
            CollectionAssert.AreEqual(new[] { (321U, MissionState.NotAssigned), (322U, MissionState.Active) },
                context.Manager.GiveMissionList(context.Client.Player).ToArray());
        }

        [TestMethod]
        public void AGiftNeedsNoChannelNpcOfferOrRequirement()
        {
            using var context = Context();
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 321), "Mission 321 is NPC-only.");

            Assert.IsTrue(context.Manager.TryGiveMission(context.Client, 321, "test"));

            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[321].State);
            Assert.AreEqual(1U, context.ReadMission(321).Generation);
            Assert.HasCount(1, context.Drain().OfType<MissionGainedPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(MissionState.Active, false)]
        [DataRow(MissionState.Success, false)]
        [DataRow(MissionState.Completed, false)]
        [DataRow(MissionState.Failed, true)]
        public void AGiftFollowsTheAvailableColumn(MissionState state, bool given)
        {
            using var context = Context();
            context.SeedMission(1, 321, (uint)state, false);
            context.ReloadPlayerMissions();
            context.Drain();

            Assert.AreEqual(given, context.Manager.TryGiveMission(context.Client, 321, "test"));

            if (given)
            {
                Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[321].State);
                Assert.HasCount(1, context.Drain().OfType<MissionGainedPacket>().ToArray());
            }
            else
                Assert.AreEqual(state, context.Client.Player.Missions[321].State);
        }

        [TestMethod]
        public void AnUnknownMissionIsNotGiven()
        {
            using var context = Context();
            Assert.IsFalse(context.Manager.TryGiveMission(context.Client, 999, "test"));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(999));
        }

        [TestMethod]
        [DataRow((byte)GmLevel.GameMaster, "", true, false)]
        [DataRow((byte)GmLevel.GameMaster, "321", false, true)]
        [DataRow((byte)0, "", false, false)]
        [DataRow((byte)0, "321", false, false)]
        public void TheSlashCommandOpensThePickerOrGivesAtGameMaster(byte level, string args, bool picker, bool given)
        {
            using var context = Context();
            context.Client.AccountEntry.Level = level;
            context.Drain();
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                new ChatCommandsManager(null).PrivilegedCommand(context.Client,
                    new PrivilegedCommandPacket { Command = "givemission", Args = args });
            }
            finally { singleton.SetValue(null, previous); }

            var packets = context.Drain();
            Assert.AreEqual(picker ? 1 : 0, packets.OfType<QAGiveMissionAckPacket>().Count());
            Assert.AreEqual(given, context.Client.Player.Missions.ContainsKey(321));
        }

        private static MissionTestContext Context() =>
            MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = Definition(321, MissionChannel.Npc, new LevelRequirement(50)), [322] = Definition(322, MissionChannel.Radio, null) },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null), [322] = new(0, null, null, null) });

        private static Mission Definition(uint missionId, MissionChannel acceptance, MissionRequirement requirement) =>
            new(missionId, "Give fixture", missionId, acceptance.HasFlag(MissionChannel.Npc) ? 77U : null, 88, 1, 1, 2, false, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { 1003, null, null }, 0,
                        MissionObjectiveState.Incomplete, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(),
                        MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5))
                }, enableOperational: true, requirement: requirement,
                acceptanceChannel: acceptance,
                radioSources: acceptance.HasFlag(MissionChannel.Radio)
                    ? new[] { new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "fixture.arrival") } : null);
    }
}
