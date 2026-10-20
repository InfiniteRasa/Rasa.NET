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
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A message that goes to several clients is encoded once and the bytes sent to each
    /// (IBroadcastMessage, ProtocolPacket.Write). Every client encoded it afresh: a hit, an effect
    /// or a move among forty players was marshalled forty times. What each client is sent is
    /// unchanged, byte for byte; what can differ - the movement channel's sequence number, and
    /// the encryption - is outside what is shared.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BroadcastEncodingTests
    {
        [TestMethod]
        public void ABroadcastIsEncodedOnceAndEveryoneIsSentWhatTheyWereSentBefore()
        {
            using var world = new WorldTestContext();
            var players = Crowd(world, 6);
            var packet = new Counted { Value = 41 };

            CellManager.Instance.CellCallMethod(world.Map, players[0].Player, packet);

            var frames = players.Select(client => Frame(WorldTestContext.Drain(client).Single())).ToList();

            Assert.AreEqual(1, packet.Writes, "marshalled once, for six");

            var alone = Frame(new ProtocolPacket(new CallMethodMessage(players[0].Player.EntityId, packet), ClientMessageOpcode.CallMethod, false, 0));
            foreach (var frame in frames)
                CollectionAssert.AreEqual(alone, frame, "the frame a client was sent when it was encoded for that client alone");
        }

        [TestMethod]
        public void OneClientIsSentThePacketAsItIsWhenItGoesOut()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var packet = new Counted { Value = 1 };

            client.CallMethod(SysEntity.ClientMethodId, packet);
            packet.Value = 2;

            var frame = Frame(WorldTestContext.Drain(client).Single());

            CollectionAssert.AreEqual(Alone(SysEntity.ClientMethodId, new Counted { Value = 2 }), frame, "encoded when it is sent, as it always was");
        }

        [TestMethod]
        public void APacketSentOnAgainAfterItWasEncodedIsSentAsItIsThen()
        {
            using var world = new WorldTestContext();
            var players = Crowd(world, 3);
            var (first, second, later) = (players[0], players[1], players[2]);
            var packet = new Counted { Value = 7 };

            first.CallMethod(SysEntity.ClientMethodId, packet);
            second.CallMethod(SysEntity.ClientMethodId, packet);

            var firstFrame = Frame(WorldTestContext.Drain(first).Single());

            // Changed after it went to the first of the two, and sent to someone else.
            packet.Value = 8;
            later.CallMethod(SysEntity.ClientMethodId, packet);

            CollectionAssert.AreEqual(firstFrame, Frame(WorldTestContext.Drain(second).Single()), "the two it was broadcast to are sent the same");
            CollectionAssert.AreEqual(Alone(SysEntity.ClientMethodId, new Counted { Value = 8 }), Frame(WorldTestContext.Drain(later).Single()),
                "and not the bytes made for them to whoever it is sent to afterwards");
            Assert.AreEqual(2, packet.Writes);
        }

        [TestMethod]
        public void OnePacketOnTwoEntitiesGoesOutOnEach()
        {
            using var world = new WorldTestContext();
            var players = Crowd(world, 2);
            var packet = new Counted { Value = 3 };

            players[0].CallMethod(players[0].Player.EntityId, packet);
            players[1].CallMethod(players[1].Player.EntityId, packet);
            players[0].CallMethod(players[1].Player.EntityId, packet);

            var toFirst = WorldTestContext.Drain(players[0]).Select(p => (CallMethodMessage)p.Message).ToList();
            var toSecond = WorldTestContext.Drain(players[1]).Select(p => (CallMethodMessage)p.Message).Single();

            CollectionAssert.AreEqual(new[] { players[0].Player.EntityId, players[1].Player.EntityId }, toFirst.Select(m => m.EntityId).ToList());
            Assert.AreEqual(players[1].Player.EntityId, toSecond.EntityId);
            CollectionAssert.AreEqual(Alone(players[1].Player.EntityId, packet), Frame(new ProtocolPacket(toSecond, ClientMessageOpcode.CallMethod, false, 0)));
        }

        [TestMethod]
        public void InterleavedPacketsAreEachSharedAndKeepTheirOrder()
        {
            using var world = new WorldTestContext();
            var players = Crowd(world, 4);
            var health = new Counted { Value = 10 };
            var state = new Counted { Value = 20 };

            // As a loop that sends two packets to each client (PartyManager.AddMemberEntry).
            foreach (var client in players)
            {
                client.CallMethod(SysEntity.ClientMethodId, health);
                client.CallMethod(SysEntity.ClientMethodId, state);
            }

            foreach (var client in players)
            {
                var frames = WorldTestContext.Drain(client).Select(Frame).ToList();

                Assert.HasCount(2, frames);
                CollectionAssert.AreEqual(Alone(SysEntity.ClientMethodId, new Counted { Value = 10 }), frames[0]);
                CollectionAssert.AreEqual(Alone(SysEntity.ClientMethodId, new Counted { Value = 20 }), frames[1]);
            }

            Assert.AreEqual(1, health.Writes);
            Assert.AreEqual(1, state.Writes);
        }

        [TestMethod]
        public void AMoveSharesItsBodyAndEachClientKeepsItsOwnSequence()
        {
            using var world = new WorldTestContext();
            var players = Crowd(world, 3);
            var creature = new Creature { Name = "Mover", RuntimeMapChannel = world.Map };
            var movement = new Movement(new Vector3(12.5f, 3f, -7.25f), new Vector2(1.5f, 0f));

            creature.Cells = (uint[,])players[0].Player.Cells.Clone();
            CellManager.Instance.CellMoveObject(creature, movement);

            var moves = players.Select(client => WorldTestContext.Drain(client).Single()).ToList();

            Assert.AreEqual(1, moves.Select(p => p.Message).Distinct().Count(), "one message for all of them");

            for (var i = 0; i < moves.Count; i++)
            {
                // Client.Stamp gives each its channel's next number as it is flushed.
                moves[i].SequenceNumber = (uint)(100 * (i + 1));

                var alone = new ProtocolPacket(new MoveObjectMessage(creature.EntityId, movement), ClientMessageOpcode.MoveObject, false, 1)
                {
                    SequenceNumber = moves[i].SequenceNumber
                };

                Assert.AreEqual(1, moves[i].Channel);
                CollectionAssert.AreEqual(Frame(alone), Frame(moves[i]));
            }
        }

        /// <summary>Players stood together, with what they were told of each other arriving read.</summary>
        private static List<Client> Crowd(WorldTestContext world, int count)
        {
            var players = Enumerable.Range(0, count).Select(i => PlayerDeathTests.Player(world, i, 0)).ToList();

            foreach (var client in players)
                WorldTestContext.Drain(client);

            return players;
        }

        private static byte[] Alone(ulong entityId, PythonPacket packet) =>
            Frame(new ProtocolPacket(new CallMethodMessage(entityId, packet), ClientMessageOpcode.CallMethod, false, 0));

        private static byte[] Alone(SysEntity entityId, PythonPacket packet) => Alone((ulong)entityId, packet);

        private static byte[] Frame(ProtocolPacket packet)
        {
            using var stream = new MemoryStream();

            using (var writer = new BinaryWriter(stream))
                packet.Write(writer);

            return stream.ToArray();
        }

        /// <summary>A packet that counts how often it is marshalled.</summary>
        private sealed class Counted : ServerPythonPacket
        {
            public override GameOpcode Opcode { get; } = GameOpcode.GameEffectDetached;

            public int Value { get; set; }
            public int Writes { get; private set; }

            public override void Write(PythonWriter pw)
            {
                Writes++;
                pw.WriteTuple(1);
                pw.WriteInt(Value);
            }
        }
    }
}
