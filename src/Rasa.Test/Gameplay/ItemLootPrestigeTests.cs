using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;
    using LootFixture = LootConsolidationTests.LootFixture;

    /// <summary>
    /// Prestige for a rare item taken off a creature's corpse (ItemLootPrestige), and the
    /// client's line for it (ReceivedItemLootPrestige).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ItemLootPrestigeTests
    {
        [TestMethod]
        [DataRow(LootQuality.Mission, 0)]
        [DataRow(LootQuality.Normal, 0)]
        [DataRow(LootQuality.Uncommon, 0)]
        [DataRow(LootQuality.Rare, 17)]
        [DataRow(LootQuality.Epic, 30)]
        [DataRow(LootQuality.Legendary, 0)]
        [DataRow(LootQuality.Junk, 0)]
        [DataRow((LootQuality)0, 0)]
        public void OnlyExperimentalAndPrototypeItemsAreWorthPrestige(LootQuality quality, int amount)
        {
            Assert.AreEqual(amount, ItemLootPrestige.Amount((int)quality));
        }

        [TestMethod]
        public void TheNoticeIsAmountQualityTemplateAndModules()
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                new ReceivedItemLootPrestigePacket(30, 5, 41234, new[] { 7, 300 }).Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(30, reader.ReadInt());
            Assert.AreEqual(5, reader.ReadInt());
            Assert.AreEqual(41234u, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(7, reader.ReadInt());
            Assert.AreEqual(300, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
            Assert.AreEqual(GameOpcode.ReceivedItemLootPrestige, new ReceivedItemLootPrestigePacket(1, 4, 1).Opcode);
        }

        [TestMethod]
        public void AnItemWithoutModulesSendsAnEmptyList()
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                new ReceivedItemLootPrestigePacket(17, 4, 28).Write(writer);

            // A tuple of four: 17, 4, 28, and a list of none.
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x1D, 17, 0x14, 0x1D, 28, 0x70 }, stream.ToArray());
        }

        [TestMethod]
        [DataRow(LootQuality.Rare, 17)]
        [DataRow(LootQuality.Epic, 30)]
        public void TakingARareItemOffACorpsePaysPrestigeAndSaysSo(LootQuality quality, int amount)
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)quality;

            context.Manager.RequestLootItemFromCorpse(context.Client,
                new RequestLootItemFromCorpsePacket { EntityId = context.Loot.EntityId, ItemId = context.Item.EntityId });

            var sent = Sent(context);
            var notice = sent.Single(message => message.Packet is ReceivedItemLootPrestigePacket);
            var packet = (ReceivedItemLootPrestigePacket)notice.Packet;

            Assert.AreEqual((ulong)SysEntity.ClientPrestigeSystemId, notice.EntityId, "on the prestige system");
            Assert.AreEqual(amount, packet.Amount);
            Assert.AreEqual((int)quality, packet.QualityId);
            Assert.AreEqual(context.Item.ItemTemplate.ItemTemplateId, packet.ItemTemplateId);
            Assert.AreEqual(0, packet.ModuleIds.Count);

            var balance = sent.Select(message => message.Packet).OfType<UpdateCreditsPacket>()
                .Single(update => update.Type == CurencyType.Prestige);
            Assert.AreEqual(amount, balance.Amount);
            Assert.AreEqual(amount, balance.Delta);

            Assert.AreEqual(amount, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(amount, StoredPrestige(context));
            Assert.AreEqual(1, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
        }

        [TestMethod]
        public void ThePrestigeIsAddedToWhatThePlayerHolds()
        {
            using var context = new LootFixture();
            Hold(context, 1000);
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Epic;

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            var balance = Sent(context).Select(message => message.Packet).OfType<UpdateCreditsPacket>()
                .Single(update => update.Type == CurencyType.Prestige);
            Assert.AreEqual(1030, balance.Amount);
            Assert.AreEqual(30, balance.Delta);
            Assert.AreEqual(1030, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(1030, StoredPrestige(context));
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits], "the corpse's credits as before");
        }

        [TestMethod]
        [DataRow(LootQuality.Mission)]
        [DataRow(LootQuality.Normal)]
        [DataRow(LootQuality.Uncommon)]
        [DataRow(LootQuality.Legendary)]
        [DataRow(LootQuality.Junk)]
        public void AnOrdinaryItemPaysNothingAndSaysNothing(LootQuality quality)
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)quality;

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            var packets = Sent(context).Select(message => message.Packet).ToList();
            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(0, packets.OfType<ReceivedItemLootPrestigePacket>().Count());
            Assert.AreEqual(0, packets.OfType<UpdateCreditsPacket>().Count(update => update.Type == CurencyType.Prestige));
            Assert.AreEqual(0, context.Client.Player.Credits.GetValueOrDefault(CurencyType.Prestige));
            Assert.AreEqual(0, StoredPrestige(context));
        }

        [TestMethod]
        public void EachRareItemHasItsLineAndTheBalanceMovesOnce()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Rare;
            var second = context.Storage.AddUnownedLoot(1);
            second.ItemTemplate.QualityId = (int)LootQuality.Epic;
            context.Loot.LootItems.Add(new LootItem(second, context.Client.Player.EntityId, 0));
            var third = context.Storage.AddUnownedLoot(1);
            third.ItemTemplate.QualityId = (int)LootQuality.Normal;
            context.Loot.LootItems.Add(new LootItem(third, context.Client.Player.EntityId, 0));

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            var packets = Sent(context).Select(message => message.Packet).ToList();
            CollectionAssert.AreEqual(new[] { (17, 4), (30, 5) },
                packets.OfType<ReceivedItemLootPrestigePacket>().Select(notice => (notice.Amount, notice.QualityId)).ToArray());

            var balance = packets.OfType<UpdateCreditsPacket>().Single(update => update.Type == CurencyType.Prestige);
            Assert.AreEqual(47, balance.Amount);
            Assert.AreEqual(47, balance.Delta);
            Assert.AreEqual(47, StoredPrestige(context));
        }

        [TestMethod]
        public void AnItemPaysOnceHoweverOftenItIsAskedFor()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Rare;
            var take = new RequestLootItemFromCorpsePacket { EntityId = context.Loot.EntityId, ItemId = context.Item.EntityId };

            context.Manager.RequestLootItemFromCorpse(context.Client, take);
            Sent(context);
            context.Manager.RequestLootItemFromCorpse(context.Client, take);
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(0, Sent(context).Count(message => message.Packet is ReceivedItemLootPrestigePacket));
            Assert.AreEqual(17, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(17, StoredPrestige(context));
        }

        [TestMethod]
        public void AStackIsOneItem()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Rare;
            Assert.AreEqual(3u, context.Item.StackSize);

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(17, context.Client.Player.Credits[CurencyType.Prestige]);
        }

        [TestMethod]
        public void AClaimThatFailsGivesNeitherTheItemNorThePrestige()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Epic;
            context.Storage.AfterSave = _ => throw new DbUpdateException("Injected loot failure.");

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(0, Sent(context).Count);
            Assert.AreEqual(0, context.Client.Player.Credits.GetValueOrDefault(CurencyType.Prestige));
            Assert.AreEqual(0, StoredPrestige(context));
            Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));

            context.Storage.AfterSave = null;
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(1, Sent(context).Count(message => message.Packet is ReceivedItemLootPrestigePacket));
            Assert.AreEqual(30, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(30, StoredPrestige(context));
        }

        [TestMethod]
        public void PrestigeTheServerDoesNotHoldRefusesTheRareItemAndNoOther()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Rare;
            var ordinary = context.Storage.AddUnownedLoot(1);
            ordinary.ItemTemplate.QualityId = (int)LootQuality.Normal;
            context.Loot.LootItems.Add(new LootItem(ordinary, context.Client.Player.EntityId, 0));

            // The stored balance and the one in the world disagree.
            using (var database = context.Storage.Open())
            {
                database.CharacterEntries.Single().Prestige = 5;
                database.SaveChanges();
            }

            context.Manager.RequestLootItemFromCorpse(context.Client,
                new RequestLootItemFromCorpsePacket { EntityId = context.Loot.EntityId, ItemId = context.Item.EntityId });

            // Refused, and the balance in the world put back to the stored one, shown, and the
            // player told to try again (CharacterManager.PutPurseRight) - nothing else.
            var sent = Sent(context).Select(message => message.Packet).ToList();
            var shown = sent.OfType<UpdateCreditsPacket>().Single();
            Assert.AreEqual(CurencyType.Prestige, shown.Type);
            Assert.AreEqual(5, shown.Amount);
            Assert.AreEqual(1, sent.OfType<Rasa.Packets.Communicator.Server.SystemMessagePacket>().Count());
            Assert.AreEqual(2, sent.Count);
            Assert.AreEqual(5, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(5, StoredPrestige(context));
            Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));

            context.Manager.RequestLootItemFromCorpse(context.Client,
                new RequestLootItemFromCorpsePacket { EntityId = context.Loot.EntityId, ItemId = ordinary.EntityId });

            Assert.AreEqual(1, context.Client.Player.Inventory.PersonalInventory.Count(id => id == ordinary.EntityId));
            Assert.AreEqual(5, StoredPrestige(context));
        }

        [TestMethod]
        public void PrestigeStopsAtTheMostABalanceCanBe()
        {
            using var context = new LootFixture();
            Hold(context, int.MaxValue - 10);
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Epic;

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(int.MaxValue, context.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(int.MaxValue, StoredPrestige(context));
        }

        [TestMethod]
        public void AMissionsRewardObjectIsNotACorpse()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Epic;

            Assert.AreEqual(1, ItemLootPrestige.Awards(context.Loot, context.Loot.LootItems).Count);

            var crate = new LootDispenser { AttachedObject = new DynamicObject() };
            crate.LootItems.AddRange(context.Loot.LootItems);

            Assert.AreEqual(0, ItemLootPrestige.Awards(crate, crate.LootItems).Count);
        }

        private static List<CallMethodMessage> Sent(LootFixture context) =>
            WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

        private static int StoredPrestige(LootFixture context)
        {
            using var database = context.Storage.Open();
            return database.CharacterEntries.AsNoTracking().Single().Prestige;
        }

        private static void Hold(LootFixture context, int prestige)
        {
            context.Client.Player.Credits[CurencyType.Prestige] = prestige;
            using var database = context.Storage.Open();
            database.CharacterEntries.Single().Prestige = prestige;
            database.SaveChanges();
        }
    }
}
