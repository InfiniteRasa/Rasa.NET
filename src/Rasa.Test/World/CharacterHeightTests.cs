using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A character's height is the scale of Recv_BodyAttributes, and nothing else in the client
    /// sizes an entity the server made: a player is sent it with their entity, to themselves and
    /// to everyone who sees them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CharacterHeightTests
    {
        [TestMethod]
        public void APlayersBodyIsTheirHeightUntintedAndCollides()
        {
            var packet = BodyAttributesPacket.ForPlayer(0.94);

            Assert.AreEqual(0.94, packet.Scale);
            Assert.IsNull(packet.Hue);
            Assert.IsNull(packet.Hue2);
            Assert.AreEqual(BodyAttributesPacket.Collide, packet.IgnoreABVs);
            Assert.AreEqual(BodyAttributesPacket.Collide, packet.IgnoreWS);

            // (scale, hue, ignoreABVs, ignoreWS, hue2): a hue of None is no tint.
            var sent = Decode(packet);
            Assert.HasCount(5, sent);
            Assert.AreEqual(0.94, (double)sent[0], 0.000001);
            Assert.IsNull(sent[1]);
            Assert.AreEqual(0L, sent[2]);
            Assert.AreEqual(0L, sent[3]);
            Assert.IsNull(sent[4]);
        }

        [TestMethod]
        [DataRow(0.9, 0.9)]
        [DataRow(1.06, 1.06)]
        [DataRow(1.0, 1.0)]
        [DataRow(0.0, 1.0)]
        [DataRow(-1.0, 1.0)]
        [DataRow(double.NaN, 1.0)]
        [DataRow(double.PositiveInfinity, 1.0)]
        public void AHeightThatIsNoHeightIsTheDefault(double stored, double sent)
        {
            Assert.AreEqual(sent, BodyAttributesPacket.ForPlayer(stored).Scale);
        }

        [TestMethod]
        public void ACreaturesBodyIsSentWhatItIsGiven()
        {
            var packet = new BodyAttributesPacket(1.5, new Color(10, 20, 30), BodyAttributesPacket.Ignore, BodyAttributesPacket.Ignore, new Color(40, 50, 60));
            var sent = Decode(packet);

            Assert.AreEqual(1.5, (double)sent[0], 0.000001);
            CollectionAssert.AreEqual(new object[] { 10L, 20L, 30L, 255L }, (List<object>)sent[1]);
            Assert.AreEqual(1L, sent[2]);
            Assert.AreEqual(1L, sent[3]);
            CollectionAssert.AreEqual(new object[] { 40L, 50L, 60L, 255L }, (List<object>)sent[4]);

            // Each switch is its own: the second used to be thrown away, and both written as 1.
            var mixed = new BodyAttributesPacket(1.0, null, BodyAttributesPacket.Collide, BodyAttributesPacket.Ignore, null);
            Assert.AreEqual(BodyAttributesPacket.Collide, mixed.IgnoreABVs);
            Assert.AreEqual(BodyAttributesPacket.Ignore, mixed.IgnoreWS);

            sent = Decode(mixed);
            Assert.AreEqual(0L, sent[2]);
            Assert.AreEqual(1L, sent[3]);
        }

        [TestMethod]
        public void TheTwoTintsAreSentBothOrNeither()
        {
            // The client reads hue2 without looking once hue is there.
            var first = Decode(new BodyAttributesPacket(1.0, new Color(10, 20, 30), 1, 1, null));
            Assert.IsNull(first[1]);
            Assert.IsNull(first[4]);

            var second = Decode(new BodyAttributesPacket(1.0, null, 1, 1, new Color(10, 20, 30)));
            Assert.IsNull(second[1]);
            Assert.IsNull(second[4]);
        }

        [TestMethod]
        public void APlayerIsSentTheirHeightWithTheirEntityAndSoIsEveryoneWhoSeesThem()
        {
            using var world = new WorldTestContext();
            var tall = world.CreateClient();
            var onlooker = world.CreateClient(2, 0);

            tall.Player.Scale = 1.06;
            onlooker.Player.Scale = 0.9;

            foreach (var recipient in new[] { tall, onlooker })
            {
                var data = ManifestationManager.Instance.CreatePlayerEntityData(tall, recipient);
                var body = data.OfType<BodyAttributesPacket>().Single();

                Assert.AreEqual(1.06, body.Scale);
                Assert.IsNull(body.Hue, "the bare avatar mesh is not tinted");
                Assert.AreEqual(BodyAttributesPacket.Collide, body.IgnoreABVs);
                Assert.AreEqual(BodyAttributesPacket.Collide, body.IgnoreWS);

                // With the rest of what a physical entity is, ahead of what a manifestation is.
                Assert.AreEqual(data.FindIndex(packet => packet is WorldLocationDescriptorPacket) + 1, data.IndexOf(body));
            }

            Assert.AreEqual(0.9, ManifestationManager.Instance.CreatePlayerEntityData(onlooker, tall).OfType<BodyAttributesPacket>().Single().Scale);

            // A character with no height kept stands at the default.
            onlooker.Player.Scale = 0;
            Assert.AreEqual(1.0, ManifestationManager.Instance.CreatePlayerEntityData(onlooker, tall).OfType<BodyAttributesPacket>().Single().Scale);
        }

        [TestMethod]
        public void ItGoesOutInThePacketThatMakesThePlayerOnAClient()
        {
            using var world = new WorldTestContext();
            var tall = world.CreateClient();

            tall.Player.Scale = 1.06;
            WorldTestContext.Drain(tall);

            ManifestationManager.Instance.CellIntroduceClientToSefl(tall);

            var created = WorldTestContext.Drain(tall)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == tall.Player.EntityId);

            Assert.AreEqual(1.06, created.EntityData.OfType<BodyAttributesPacket>().Single().Scale);
        }

        /// <summary>What a packet writes, as the client reads it: tuples as lists, whole numbers as longs, None as null.</summary>
        private static List<object> Decode(PythonPacket packet) =>
            (List<object>)Read(new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet)))));

        private static object Read(PythonReader reader)
        {
            switch (reader.PeekType())
            {
                case PythonType.Tuple:
                    return Items(reader, reader.ReadTuple());
                case PythonType.List:
                    return Items(reader, reader.ReadList());
                case PythonType.Int:
                    return (long)reader.ReadInt();
                case PythonType.Long:
                    return reader.ReadLong();
                case PythonType.Double:
                    return reader.ReadDouble();
                case PythonType.Structs:
                    return reader.ReadUnkStruct() switch
                    {
                        PythonStruct.None => null,
                        PythonStruct.True => (object)true,
                        _ => false
                    };
                default:
                    throw new InvalidDataException($"Unexpected {reader.PeekType()} in body attributes.");
            }
        }

        private static List<object> Items(PythonReader reader, int count)
        {
            var items = new List<object>(count);

            for (var i = 0; i < count; i++)
                items.Add(Read(reader));

            return items;
        }
    }
}
