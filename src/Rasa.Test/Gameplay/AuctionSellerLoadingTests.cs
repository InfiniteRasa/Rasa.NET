extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    // An auction that sells, or runs out, while its seller is between maps or on a loading screen.
    //
    // A character is loaded, purse and all, from the moment it is chosen until its connection is
    // done with it: on the login loading screen (Loading), on a dropship or map link between maps
    // (Teleporting), and in the world. The sale pays the seller's row; the purse the session
    // goes by has to be paid with it, or every later write that checks the purse against the
    // row is refused, and every one that does not writes the old purse over the sale.
    [TestClass]
    [DoNotParallelize]
    public class AuctionSellerLoadingTests
    {
        private const uint Buyer = 42;
        private const uint SellerId = 99;
        private const uint Price = 50;

        #region Paid

        [TestMethod]
        public void ASellerOnTheLoginLoadingScreenIsPaidInTheirPurse()
        {
            using var sale = new Sale(ClientState.Loading);

            sale.Buy();

            Assert.AreEqual((int)Price, sale.Row(SellerId), "the row");
            Assert.AreEqual((int)Price, sale.Seller.Player.Credits[CurencyType.Credits], "and the purse the session goes by");

            // Told it sold. The purse itself goes to the client with the rest of the character
            // when it arrives (AllCredits): there is no manifestation on the client to tell yet.
            var sent = Sent(sale.Seller);
            Assert.AreEqual(1, sent.OfType<AuctionSoldPacket>().Count());
            Assert.AreEqual(0, sent.OfType<UpdateCreditsPacket>().Count());
        }

        [TestMethod]
        public void ASellerBetweenMapsIsPaidInTheirPurseAndShownIt()
        {
            using var sale = new Sale(ClientState.Teleporting);

            sale.Buy();

            Assert.AreEqual((int)Price, sale.Row(SellerId));
            Assert.AreEqual((int)Price, sale.Seller.Player.Credits[CurencyType.Credits]);

            var credits = Sent(sale.Seller).OfType<UpdateCreditsPacket>().Single();
            Assert.AreEqual((int)Price, credits.Amount);
            Assert.AreEqual((int)Price, credits.Delta);
        }

        [TestMethod]
        public void ASaleBetweenMapsLeavesTheSellersAuctionList()
        {
            using var sale = new Sale(ClientState.Teleporting);

            sale.Buy();

            // The arrival shows Your Auctions from this list (InventoryManager.ResendForMap): the
            // buyer's item, still alive in their inbox, used to be listed as the seller's.
            Assert.IsFalse(sale.Seller.Player.Inventory.AuctionItems.Contains(sale.Item.EntityId));
            Assert.AreEqual(1, Sent(sale.Seller).OfType<RemoveAuctionItemPacket>().Count());
        }

        [TestMethod]
        public void ASellerPaidOnTheLoadingScreenCanSpendItOnceTheyArrive()
        {
            using var sale = new Sale(ClientState.Loading);

            sale.Buy();
            sale.Seller.State = ClientState.Ingame;

            // A vendor purchase: the purse checked against the row, and both written.
            Assert.IsTrue(new CharacterManager(sale.Context).UpdateCharacter(sale.Seller, CharacterUpdate.Credits, -20),
                "refused while the purse and the row disagreed");
            Assert.AreEqual(30, sale.Row(SellerId));
            Assert.AreEqual(30, sale.Seller.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void TheConnectionTheCharacterLeftBehindIsNotTheOnePaid()
        {
            // The same character's previous connection, closed and not yet cleared away, ahead
            // of the new one in the server's list.
            using var sale = new Sale(ClientState.Loading, leftBehind: true);

            sale.Buy();

            Assert.AreEqual((int)Price, sale.Seller.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, sale.LeftBehind.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void ASellerOnTheCharacterScreenIsPaidInTheRowOnly()
        {
            // The character has no session: its next choice loads the purse from the row.
            using var sale = new Sale(ClientState.CharacterSelection);

            sale.Buy();

            Assert.AreEqual((int)Price, sale.Row(SellerId));
            Assert.AreEqual(0, sale.Seller.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, Sent(sale.Seller).Count);
        }

        #endregion

        #region Run out

        [TestMethod]
        public void AnAuctionThatRunsOutWhileTheSellerIsBetweenMapsComesBackToTheirPickUpBox()
        {
            using var sale = new Sale(ClientState.Teleporting);

            sale.Expire();

            // In the list the arrival shows (ResendForMap, ShowInbox), and out of the other one.
            var inventory = sale.Seller.Player.Inventory;
            Assert.IsTrue(inventory.InboxItems.Contains(sale.Item.EntityId), "in the pick-up box");
            Assert.IsFalse(inventory.AuctionItems.Contains(sale.Item.EntityId), "and no longer listed");
            Assert.IsNotNull(EntityManager.Instance.GetItem(sale.Item.EntityId), "and still an entity");
            Assert.AreEqual(1, Sent(sale.Seller).OfType<AuctionExpiredPacket>().Count());
        }

        [TestMethod]
        public void AnAuctionThatRunsOutDuringTheLoginLoadingScreenIsLeftToTheArrival()
        {
            using var sale = new Sale(ClientState.Loading);

            sale.Expire();

            // The arrival loads the inventory from the rows (InventoryManager.InitForClient), and
            // the row is in the pick-up box.
            Assert.IsFalse(sale.Seller.Player.Inventory.InboxItems.Contains(sale.Item.EntityId));
            Assert.AreEqual((uint)InventoryType.InboxInventory, sale.ItemRow().InventoryType);
            Assert.AreEqual(1, Sent(sale.Seller).OfType<AuctionExpiredPacket>().Count(), "told it ran out");
        }

        #endregion

        #region Fixture

        private static List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message)
            .OfType<CallMethodMessage>()
            .Select(message => message.Packet)
            .OfType<PythonPacket>()
            .ToList();

        /// <summary>One listing of the seller's, 50 credits, and a buyer with 100.</summary>
        private sealed class Sale : IDisposable
        {
            private readonly List<Client> _registered = new();

            internal WeaponAmmoContext Context { get; } = new(characterId: Buyer);
            internal Client Seller { get; }
            internal Client LeftBehind { get; }
            internal Item Item { get; }

            internal Sale(ClientState sellerState, bool leftBehind = false)
            {
                Context.Client.Player.Credits[CurencyType.Credits] = 100;
                using (var database = Context.Open())
                {
                    database.CharacterEntries.Single(entry => entry.Id == Buyer).Credit = 100;
                    database.SaveChanges();
                }

                Item = Context.AddUnownedLoot(1);
                Context.AddAuction(Item, SellerId, Price);

                if (leftBehind)
                    LeftBehind = SellerClient(ClientState.Disconnected);

                Seller = SellerClient(sellerState);
            }

            private Client SellerClient(ClientState state)
            {
                var client = Context.World.CreateClient(factory: Context);

                client.State = state;
                client.Player.Id = SellerId;
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                    new GameAccountEntry { Id = 2, SelectedSlot = 1 });
                client.Player.Credits[CurencyType.Credits] = 0;
                client.Player.Credits[CurencyType.Prestige] = 0;
                client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
                client.Player.Inventory.AuctionItems.Add(Item.EntityId);

                lock (Server.Clients)
                    Server.Clients.Add(client);

                _registered.Add(client);
                return client;
            }

            private AuctionHouseManager Manager() =>
                (AuctionHouseManager)typeof(AuctionHouseManager)
                    .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                    .Invoke(new object[] { Context });

            internal void Buy()
            {
                Manager().RequestAuctionBuyout(Context.Client, new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)Item.EntityId),
                    Price = Price
                });

                Assert.AreEqual(100 - (int)Price, Row(Buyer), "bought");
            }

            internal void Expire()
            {
                using (var database = Context.Open())
                {
                    database.AuctionEntries.Single(entry => entry.ItemId == Item.Id).CreatedAt = DateTime.UtcNow.AddDays(-1);
                    database.SaveChanges();
                }

                Manager().ExpireAuctions();

                using var verify = Context.Open();
                Assert.AreEqual(0, verify.AuctionEntries.AsNoTracking().Count(entry => entry.ItemId == Item.Id), "ran out");
            }

            internal int Row(uint characterId)
            {
                using var database = Context.Open();

                return database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == characterId).Credit;
            }

            internal CharacterInventoryEntry ItemRow()
            {
                using var database = Context.Open();

                return database.CharacterInventoryEntries.AsNoTracking().Single(entry => entry.ItemId == Item.Id);
            }

            public void Dispose()
            {
                lock (Server.Clients)
                    foreach (var client in _registered)
                        Server.Clients.Remove(client);

                Context.Dispose();
            }
        }

        #endregion
    }
}
