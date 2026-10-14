using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    /// <summary>
    /// An auction past its duration is its seller's again. It used to stay in the browse tab,
    /// and could be bought out, until the five-minute sweep or the seller's next login sent it
    /// back: neither the search nor the buyout looked at the time left.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AuctionRunOutTests
    {
        private const uint Buyer = 42;
        private const uint SellerId = 99;
        private const uint Price = 50;

        [TestMethod]
        public void AnAuctionPastItsDurationIsNotFoundAndGoesBackToItsSeller()
        {
            using var context = Listed(out var item);
            var manager = Manager(context);

            // In time: found.
            manager.RequestQueryAuctions(context.Client, Search(item));
            var row = Sent(context.Client).OfType<QuerySuccessPacket>().Single().AuctionItemList.Single();
            Assert.AreEqual(item.Id, EntityManager.Instance.GetItem(row.ItemId).Id);

            RunOut(context, item);

            manager.RequestQueryAuctions(context.Client, Search(item));

            var sent = Sent(context.Client);
            Assert.AreEqual(0, sent.OfType<QuerySuccessPacket>().Count(), "not in the browse tab");
            Assert.AreEqual(PlayerMessage.PmAuctionNoResultsFound, sent.OfType<QueryFailedPacket>().Single().PlayerMessage);

            AssertSentBack(context, item);
        }

        [TestMethod]
        public void AnAuctionPastItsDurationCannotBeBoughtOutAndGoesBackToItsSeller()
        {
            using var context = Listed(out var item);

            RunOut(context, item);

            Manager(context).RequestAuctionBuyout(context.Client, new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = Price
            });

            var failed = Sent(context.Client).OfType<AuctionBuyoutFailedPacket>().Single();
            Assert.AreEqual(PlayerMessage.PmAuctionItemNotFound, failed.PlayerMessageId, "as one already swept is answered");
            Assert.AreEqual(100, Credits(context, Buyer), "nothing paid");
            Assert.AreEqual(0, Credits(context, SellerId), "nothing received");
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);

            AssertSentBack(context, item);
        }

        [TestMethod]
        public void TheLastMomentOfItsDurationIsStillForSale()
        {
            var auction = new AuctionEntry(1, SellerId, "Seller", Price, 0, 12) { CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) };

            Assert.IsFalse(auction.HasRunOut(auction.CreatedAt.AddHours(12).AddSeconds(-1)));
            Assert.AreEqual(1u, auction.RemainingHours(auction.CreatedAt.AddHours(11).AddMinutes(1)), "less than an hour left reads as one");
            Assert.IsTrue(auction.HasRunOut(auction.CreatedAt.AddHours(12)));
            Assert.AreEqual(0u, auction.RemainingHours(auction.CreatedAt.AddHours(12)));
            Assert.AreEqual(12u, auction.RemainingHours(auction.CreatedAt.AddMinutes(-5)), "a clock behind the listing");
        }

        /// <summary>A listing of the seller's, 50 credits, and a buyer with 100.</summary>
        private static WeaponAmmoContext Listed(out Item item)
        {
            var context = new WeaponAmmoContext(characterId: Buyer);

            context.Client.Player.Credits[CurencyType.Credits] = 100;

            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == Buyer).Credit = 100;
                database.SaveChanges();
            }

            item = context.AddUnownedLoot(1);
            context.AddAuction(item, SellerId, Price);
            Sent(context.Client);

            return context;
        }

        private static void RunOut(WeaponAmmoContext context, Item item)
        {
            using var database = context.Open();

            // Its 12 hours over a minute ago; the sweep has not been round.
            database.AuctionEntries.Single(entry => entry.ItemId == item.Id).CreatedAt = DateTime.UtcNow.AddHours(-12).AddMinutes(-1);
            database.SaveChanges();
        }

        private static void AssertSentBack(WeaponAmmoContext context, Item item)
        {
            using var database = context.Open();

            Assert.AreEqual(0, database.AuctionEntries.AsNoTracking().Count(entry => entry.ItemId == item.Id), "the listing is gone");

            var row = database.CharacterInventoryEntries.AsNoTracking().Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(SellerId, row.CharacterId, "the seller's");
            Assert.AreEqual((uint)InventoryType.InboxInventory, row.InventoryType, "in their pick-up box");
        }

        private static RequestQueryAuctionsPacket Search(Item item) => new RequestQueryAuctionsPacket
        {
            CategoryId = AuctionCategory.Codes.Keys.First(id => AuctionItemCategories.InCategory(item.ItemTemplate, id)),
            QualityId = (uint)item.ItemTemplate.QualityId,
            MinLevel = 0,
            MaxLevel = 50
        };

        private static int Credits(WeaponAmmoContext context, uint characterId)
        {
            using var database = context.Open();

            return database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == characterId).Credit;
        }

        private static AuctionHouseManager Manager(WeaponAmmoContext context) =>
            (AuctionHouseManager)typeof(AuctionHouseManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { context });

        private static List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message)
            .OfType<CallMethodMessage>()
            .Select(message => message.Packet)
            .OfType<PythonPacket>()
            .ToList();
    }
}
