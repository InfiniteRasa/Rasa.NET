extern alias RasaGame;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    // The footlocker's credit transfer takes whatever the client sends: its box is int(text), a
    // Python int, or a long past 2147483647. A long used to throw in the packet's read, which closes
    // the connection; now it is read, and an amount neither side holds is refused with the message
    // a too-large amount always got.
    [TestClass]
    [DoNotParallelize]
    public class LockboxCreditAmountTests
    {
        [TestMethod]
        [DataRow(9_999_999_999L, DisplayName = "ten digits, deposited")]
        [DataRow(-9_999_999_999L, DisplayName = "ten digits, withdrawn")]
        [DataRow(2_147_483_648L, DisplayName = "one past an int")]
        [DataRow(long.MaxValue)]
        public void TheTransferReadsAnAmountTheClientSendsAsALong(long amount)
        {
            var packet = new TransferCreditToLockboxPacket();

            Read(packet, pw => pw.WriteLong(amount));

            Assert.AreEqual(amount, packet.Ammount);
        }

        [TestMethod]
        [DataRow(-100, DisplayName = "a small withdrawal, one signed byte")]
        [DataRow(500)]
        [DataRow(70000)]
        [DataRow(int.MaxValue)]
        public void AndStillAnAmountItSendsAsAnInt(int amount)
        {
            var packet = new TransferCreditToLockboxPacket();

            Read(packet, pw => pw.WriteInt(amount));

            Assert.AreEqual(amount, packet.Ammount);
        }

        [TestMethod]
        public void TheClanBankReadsBothFormsTheSameWay()
        {
            var big = new ClanCreditTransferPacket();
            Read(big, pw => { pw.WriteUInt(1); pw.WriteLong(-9_999_999_999L); }, 2);
            Assert.AreEqual((1u, -9_999_999_999L), (big.CreditsType, big.Ammount));

            var small = new ClanCreditTransferPacket();
            Read(small, pw => { pw.WriteUInt(2); pw.WriteInt(-100); }, 2);
            Assert.AreEqual((2u, -100L), (small.CreditsType, small.Ammount));
        }

        [TestMethod]
        [DataRow(9_999_999_999L, "inventory", DisplayName = "a deposit past the purse")]
        [DataRow(long.MaxValue, "inventory")]
        [DataRow(-9_999_999_999L, "Lockbox", DisplayName = "a withdrawal past the lockbox")]
        [DataRow(long.MinValue, "Lockbox")]
        public void AnAmountNeitherSideHoldsIsRefusedAndNothingMoves(long amount, string side)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context, context.Manager);

            SetCredits(context, client, 1100);
            using (var database = context.Open())
            {
                database.CharacterLockboxEntries.Add(new CharacterLockboxEntry(client.AccountEntry.Id, 400, 1));
                database.SaveChanges();
            }
            client.Player.LockboxCredits = 400;

            inventory.TransferCreditToLockbox(client, amount);

            var message = Sent(client).OfType<SystemMessagePacket>().Single();
            StringAssert.Contains(message.TextMessage, side);
            Assert.AreEqual(1100, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(400, client.Player.LockboxCredits);
            Assert.AreEqual(1100, context.ReadRewardTotals().Credits);
            Assert.AreEqual(400, Lockbox(context, client));
        }

        [TestMethod]
        public void EverythingEitherSideHoldsStillGoesAcross()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context, context.Manager);

            SetCredits(context, client, 1100);
            using (var database = context.Open())
            {
                database.CharacterLockboxEntries.Add(new CharacterLockboxEntry(client.AccountEntry.Id, 400, 1));
                database.SaveChanges();
            }
            client.Player.LockboxCredits = 400;

            inventory.TransferCreditToLockbox(client, 1100L);
            Assert.AreEqual((0, 1500), (client.Player.Credits[CurencyType.Credits], client.Player.LockboxCredits));

            inventory.TransferCreditToLockbox(client, -1500L);
            Assert.AreEqual((1500, 0), (client.Player.Credits[CurencyType.Credits], client.Player.LockboxCredits));
            Assert.AreEqual(1500, context.ReadRewardTotals().Credits);
            Assert.AreEqual(0, Lockbox(context, client));
        }

        private static void Read(ClientPythonPacket packet, System.Action<PythonWriter> write, int count = 1)
        {
            using var stream = new MemoryStream();
            var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(count);
            write(writer);
            stream.Position = 0;

            packet.Read(new PythonReader(new BinaryReader(stream)));

            Assert.AreEqual(stream.Length, stream.Position, "bytes left over");
        }

        private static void SetCredits(IGameUnitOfWorkFactory context, Client client, int credits)
        {
            using (var unit = context.CreateChar())
            {
                var row = unit.Characters.Find(client.Player.Id);
                unit.Characters.UpdateCharacterCurrencies(client.Player.Id, credits, row.Prestige);
            }

            client.Player.Credits[CurencyType.Credits] = credits;
            WorldTestContext.Drain(client);
        }

        private static int Lockbox(MissionTestContext context, Client client)
        {
            using var database = context.Open();

            return database.CharacterLockboxEntries.AsNoTracking().Single(entry => entry.AccountId == client.AccountEntry.Id).Credits;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
    }
}
