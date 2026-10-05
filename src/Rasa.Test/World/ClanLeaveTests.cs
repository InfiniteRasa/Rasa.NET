using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Client;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Leaving a clan: "LeaveClan", (member.characterId, member.clanId) - client/clan.py
    // SendLeaveClan, from /clanleave and the clan window's Leave. The member is the client's own
    // line of the roster, and its characterId is what the server sent it with: a Python long, the
    // leaver's entity id (ClanMemberData.Write).
    [TestClass]
    [DoNotParallelize]
    public class ClanLeaveTests
    {
        private const uint ClanId = 7;
        private const ulong EntityId = 133079561962676UL;

        private readonly List<Client> _online = new List<Client>();

        [TestCleanup]
        public void Cleanup()
        {
            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        [TestMethod]
        public void TheMembersOwnLineGoesOutWithALongAndComesBackInLeaveClan()
        {
            // The leaver's own line, as their client is sent it.
            var line = new ClanMemberData(1, 1, EntityId, "Leaver", "Fixture", ClanId, 10, 1220, ClanRank.Member, true, false, "");
            var reader = Reader(line.Write);

            Assert.AreEqual(11, reader.ReadTuple());
            Assert.AreEqual(1u, reader.ReadUInt());
            Assert.AreEqual(PythonType.Long, reader.PeekType(), "characterId: a long, whatever its size");
            var characterId = reader.ReadULong();
            reader.ReadString();
            reader.ReadString();
            Assert.AreEqual(PythonType.Int, reader.PeekType(), "clanId");
            var clanId = reader.ReadUInt();

            // What the client sends back is those two values, as it holds them.
            var message = Message(GameOpcode.LeaveClan, pw =>
            {
                pw.WriteTuple(2);
                pw.WriteULong(characterId);
                pw.WriteUInt(clanId);
            });

            Assert.IsTrue(message.ReadPacket(), "a throw or a false here closes the connection");

            var packet = (LeaveClanPacket)message.Packet;

            Assert.AreEqual(EntityId, packet.CharacterId);
            Assert.AreEqual(ClanId, packet.ClanId);
        }

        [TestMethod]
        [DataRow(1UL, DisplayName = "a long that would fit an int")]
        [DataRow(0UL, DisplayName = "0L")]
        [DataRow(4294967296UL, DisplayName = "past 32 bits")]
        public void AMemberIdOfAnySizeIsRead(ulong id)
        {
            var message = Message(GameOpcode.LeaveClan, pw =>
            {
                pw.WriteTuple(2);
                pw.WriteULong(id);
                pw.WriteUInt(ClanId);
            });

            Assert.IsTrue(message.ReadPacket());
            Assert.AreEqual(id, ((LeaveClanPacket)message.Packet).CharacterId);
            Assert.AreEqual(ClanId, ((LeaveClanPacket)message.Packet).ClanId);
        }

        [TestMethod]
        public void AnIntMemberIdIsStillRead()
        {
            var message = Message(GameOpcode.LeaveClan, pw =>
            {
                pw.WriteTuple(2);
                pw.WriteUInt(1234);
                pw.WriteUInt(ClanId);
            });

            Assert.IsTrue(message.ReadPacket());
            Assert.AreEqual(1234UL, ((LeaveClanPacket)message.Packet).CharacterId);
        }

        [TestMethod]
        public void AMemberWhoSendsLeaveClanAsTheClientDoesIsOutOfTheClan()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var leaver = context.Client;
            var leader = context.CreateAdditionalClient(5);
            var other = context.CreateAdditionalClient(2);
            var clan = Clan(context, "Fixture Clan", (5, ClanRank.Leader), (1, ClanRank.Member), (2, ClanRank.Member));

            foreach (var client in new[] { leaver, leader, other })
                Join(client, clan);

            WithLiveClans(context, clan, clans =>
            {
                context.Drain();
                MissionTestContext.Drain(leader);
                MissionTestContext.Drain(other);

                // Their own line's id is their entity id, and it is not what says who leaves.
                var message = Message(GameOpcode.LeaveClan, pw =>
                {
                    pw.WriteTuple(2);
                    pw.WriteULong(leaver.Player.EntityId);
                    pw.WriteUInt(clan.Id);
                });

                Assert.IsTrue(message.ReadPacket());
                clans.LeaveClan(leaver, (LeaveClanPacket)message.Packet);

                using (var unit = context.CreateChar())
                    CollectionAssert.AreEquivalent(new uint[] { 5, 2 },
                        unit.ClanMembers.GetAllClanMembersByClanId(clan.Id).Select(member => member.CharacterId).ToArray());

                Assert.IsNull(clans.GetClanMember(clan.Id, leaver.Player.Id));
                Assert.AreEqual(0u, leaver.Player.ClanId);
                Assert.AreEqual(clan.Id, leader.Player.ClanId);

                // The leaver: taken out of their own roster under their entity id, and out of the clan.
                var told = context.Drain().ToList();
                var left = told.OfType<PlayerLeftClanPacket>().Single();

                Assert.AreEqual(leaver.Player.EntityId, left.CharacterId);
                Assert.AreEqual(clan.Id, left.ClanId);
                Assert.IsFalse(left.WasKicked);
                Assert.AreEqual(0u, told.OfType<ClanIdPacket>().Single().ClanId);

                // Everyone else: the member gone, under their character id.
                foreach (var member in new[] { leader, other })
                {
                    var gone = MissionTestContext.Drain(member).OfType<PlayerLeftClanPacket>().Single();

                    Assert.AreEqual((ulong)leaver.Player.Id, gone.CharacterId);
                    Assert.AreEqual(clan.Id, gone.ClanId);
                    Assert.IsFalse(gone.WasKicked);
                }
            });
        }

        [TestMethod]
        public void TheIdSentDoesNotChooseWhoLeaves()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var sender = context.Client;
            var leader = context.CreateAdditionalClient(5);
            var other = context.CreateAdditionalClient(2);
            var clan = Clan(context, "Fixture Clan", (5, ClanRank.Leader), (1, ClanRank.Member), (2, ClanRank.Member));

            foreach (var client in new[] { sender, leader, other })
                Join(client, clan);

            WithLiveClans(context, clan, clans =>
            {
                // Another member's id, in either form a client could send it.
                foreach (var named in new Action<PythonWriter>[] { pw => pw.WriteULong(other.Player.EntityId), pw => pw.WriteUInt(other.Player.Id) })
                {
                    var message = Message(GameOpcode.LeaveClan, pw =>
                    {
                        pw.WriteTuple(2);
                        named(pw);
                        pw.WriteUInt(clan.Id);
                    });

                    Assert.IsTrue(message.ReadPacket());
                    clans.LeaveClan(sender, (LeaveClanPacket)message.Packet);

                    Assert.IsNotNull(clans.GetClanMember(clan.Id, other.Player.Id), "the one named stays");
                    Assert.AreEqual(clan.Id, other.Player.ClanId);
                }

                Assert.IsNull(clans.GetClanMember(clan.Id, sender.Player.Id), "the one who sent it left, the first time");
            });
        }

        private static ClanEntry Clan(MissionTestContext context, string name, params (uint CharacterId, byte Rank)[] members)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, false);

            foreach (var (characterId, rank) in members)
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, characterId, rank, ""));

            return clan;
        }

        private void Join(Client client, ClanEntry clan)
        {
            client.Player.ClanId = clan.Id;
            client.Player.Inventory.ResetClanInventory();

            lock (Server.Clients)
                Server.Clients.Add(client);

            _online.Add(client);
        }

        /// <summary>A clan manager over the context's database with the clan cached, as it is on a running server, in place of the instance.</summary>
        private static void WithLiveClans(MissionTestContext context, ClanEntry clan, Action<ClanManager> body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                using (var unit = context.CreateChar())
                {
                    var members = unit.ClanMembers.GetAllClanMembersByClanId(clan.Id);

                    clans.Clans[clan.Id] = new Lazy<ClanEntry>(() => clan);
                    clans.ClanMembers[clan.Id] = new Lazy<List<ClanMemberEntry>>(() => members);
                }

                instance.SetValue(null, clans);
                body(clans);
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }

        private static PythonReader Reader(Action<PythonWriter> write)
        {
            var stream = new MemoryStream();

            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                write(writer);

            stream.Position = 0;

            return new PythonReader(new BinaryReader(stream));
        }

        /// <summary>A method call as it arrives from the client: the arguments between the payload's markers.</summary>
        private static CallServerMethodMessage Message(GameOpcode method, Action<PythonWriter> arguments)
        {
            byte[] payload;

            using (var buffer = new MemoryStream())
            {
                using (var binary = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
                {
                    binary.Write((byte)0x4F);

                    using (var writer = new PythonWriter(binary))
                        arguments(writer);

                    binary.Write((byte)0x66);
                }

                payload = buffer.ToArray();
            }

            using var stream = new MemoryStream();

            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteUInt((uint)method);
                writer.WriteArray(payload);
            }

            stream.Position = 0;

            using var input = new BinaryReader(stream);
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);
            var message = new CallServerMethodMessage { Subtype = CallServerMethodSubtype.UserMethodById };

            message.Read(reader);

            return message;
        }
    }
}
