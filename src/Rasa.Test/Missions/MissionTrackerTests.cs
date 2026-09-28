using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    [TestClass]
    public class MissionTrackerTests
    {
        [TestMethod]
        public void OneFieldLifecyclePacketsUseTheObservedMissionIdTuple()
        {
            var packets = new PythonPacket[]
            {
                new MissionCompletedPacket(321),
                new MissionRewardedPacket(321),
                new MissionFailedPacket(321),
                new MissionDiscardedPacket(321),
                new MissionClearedPacket(321)
            };

            foreach (var packet in packets)
            {
                var bytes = MissionTestContext.Encode(packet);
                CollectionAssert.AreEqual(new byte[] { 0x81, 0x1E, 0x41, 0x01 }, bytes,
                    packet.GetType().Name);
                using var stream = new MemoryStream(bytes);
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.AreEqual(1, reader.ReadTuple(), packet.GetType().Name);
                Assert.AreEqual(321U, reader.ReadUInt(), packet.GetType().Name);
                Assert.AreEqual(stream.Length, stream.Position, packet.GetType().Name);
            }
        }

        [TestMethod]
        public void MissionCompleteableKeepsItsObservedTwoFieldTuple()
        {
            var bytes = MissionTestContext.Encode(new MissionCompleteablePacket(321, true));

            CollectionAssert.AreEqual(new byte[] { 0x82, 0x1E, 0x41, 0x01, 0x01 }, bytes);
            using var stream = new MemoryStream(bytes);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(321U, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void MissionInfoWritesEightFieldObjectiveWithBothCounterDictionariesAndXYZIndicator()
        {
            var info = new MissionInfo
            {
                MissionState = MissionState.Active,
                Completeable = true
            };
            var objective = new MissionObjective
            {
                ObjectiveId = 21,
                State = MissionObjectiveState.Incomplete,
                Ordinal = 3,
                TimeRemaining = null,
                IsRequired = true
            };
            objective.Counters.Add(9, new MissionObjectiveCounter
            {
                CounterValue = 4,
                InitialValue = 2,
                TargetValue = 10
            });
            objective.ItemCounters.Add(200, new MissionObjectiveItemCounter
            {
                CounterValue = 5,
                TargetValue = 8
            });
            objective.IndicatorList.Add(new MissionIndicator
            {
                Position = new Vector3(1.25f, 2.5f, 3.75f),
                Radius = 4.5,
                IndicatorId = 7,
                Show3DEffect = true
            });
            info.ObjectivesList.Add(objective);

            var bytes = MissionTestContext.Encode(new ObjectiveRevealedPacket(5, 21, info));
            using var stream = new MemoryStream(bytes);
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(5U, reader.ReadUInt());
            Assert.AreEqual(21U, reader.ReadUInt());
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual((int)MissionState.Active, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
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
            Assert.AreEqual(1, reader.ReadList());

            Assert.AreEqual(8, reader.ReadTuple());
            Assert.AreEqual(21U, reader.ReadUInt());
            Assert.AreEqual((uint)MissionObjectiveState.Incomplete, reader.ReadUInt());
            Assert.AreEqual(3U, reader.ReadUInt());
            reader.ReadNoneStruct();

            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual(9U, reader.ReadUInt());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(4U, reader.ReadUInt());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(10U, reader.ReadUInt());

            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual(200U, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(5U, reader.ReadUInt());
            Assert.AreEqual(8U, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());

            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(1.25, reader.ReadDouble(), 0.001);
            Assert.AreEqual(2.5, reader.ReadDouble(), 0.001);
            Assert.AreEqual(3.75, reader.ReadDouble(), 0.001);
            Assert.AreEqual(4.5, reader.ReadDouble(), 0.001);
            Assert.AreEqual(7U, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
