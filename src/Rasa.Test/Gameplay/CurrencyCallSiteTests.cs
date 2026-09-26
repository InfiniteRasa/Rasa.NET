using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class CurrencyCallSiteTests
    {
        [TestMethod]
        public void AuctionBuyoutDoesNotDeliverOrRemoveListingAfterFailedCharge()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }
            var item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId: 99, price: 50);
            var saves = context.SaveAttempts;
            context.BeforeSave = _ =>
            {
                if (context.SaveAttempts == saves + 1)
                    throw new DbUpdateException("Injected auction charge failure.");
            };
            var manager = CreateAuctionManager(context);

            manager.RequestAuctionBuyout(context.Client, new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = 50
            });

            using var verify = context.Open();
            Assert.AreEqual(100,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.IsNotNull(verify.AuctionEntries.AsNoTracking()
                .SingleOrDefault(entry => entry.ItemId == item.Id));
            var inventory = verify.CharacterInventoryEntries.AsNoTracking()
                .Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(99u, inventory.CharacterId);
            Assert.AreEqual((uint)InventoryType.AuctionInventory, inventory.InventoryType);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .OfType<AuctionBuyoutSuccessPacket>()
                .Count());
        }

        [TestMethod]
        public void AuctionBuyoutRollsBackBuyerSellerDeliveryAndListingWhenDeliverySaveFails()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }
            var item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId: 99, price: 50);
            var saves = context.SaveAttempts;
            context.BeforeSave = _ =>
            {
                if (context.SaveAttempts == saves + 3)
                    throw new DbUpdateException("Injected auction inbox delivery failure.");
            };

            CreateAuctionManager(context).RequestAuctionBuyout(context.Client,
                new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)item.EntityId),
                    Price = 50
                });

            using var verify = context.Open();
            Assert.AreEqual(100,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.AreEqual(0,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 99).Credit);
            Assert.IsNotNull(verify.AuctionEntries.AsNoTracking()
                .SingleOrDefault(entry => entry.ItemId == item.Id));
            var inventory = verify.CharacterInventoryEntries.AsNoTracking()
                .Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(99u, inventory.CharacterId);
            Assert.AreEqual((uint)InventoryType.AuctionInventory, inventory.InventoryType);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(99u, item.OwnerId);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);
        }

        [TestMethod]
        public void ConcurrentAuctionBuyoutRetriesTransferExactlyOnce()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }
            var item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId: 99, price: 50);
            var manager = CreateAuctionManager(context);
            var packet = new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = 50
            };

            Task.WaitAll(
                Task.Run(() => manager.RequestAuctionBuyout(context.Client, packet)),
                Task.Run(() => manager.RequestAuctionBuyout(context.Client, packet)));

            var auctionPackets = WorldTestContext.Drain(context.Client)
                .Select(queued => queued.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
            Assert.AreEqual(1,
                auctionPackets.OfType<AuctionBuyoutSuccessPacket>().Count(),
                string.Join(", ", auctionPackets
                    .OfType<AuctionBuyoutFailedPacket>()
                    .Select(failure => failure.PlayerMessageId)));
            using var verify = context.Open();
            Assert.AreEqual(50,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.AreEqual(50,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 99).Credit);
            Assert.AreEqual(0, verify.AuctionEntries.AsNoTracking()
                .Count(entry => entry.ItemId == item.Id));
            var inventory = verify.CharacterInventoryEntries.AsNoTracking()
                .Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(42u, inventory.CharacterId);
            Assert.AreEqual((uint)InventoryType.InboxInventory, inventory.InventoryType);
            Assert.AreEqual(50, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1, context.Client.Player.Inventory.InboxItems
                .Count(entityId => entityId == item.EntityId));
            Assert.AreEqual(1,
                auctionPackets.OfType<AuctionBuyoutSuccessPacket>().Count());
        }

        [TestMethod]
        public void AuctionBuyoutRejectsBuyerWhoIsAlsoTheSellerWithoutChangingState()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }
            var item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId: 99, price: 50);
            using (var database = context.Open())
            {
                database.AuctionEntries.Single(entry => entry.ItemId == item.Id).SellerId = 42;
                var inventory = database.CharacterInventoryEntries
                    .Single(entry => entry.ItemId == item.Id);
                inventory.AccountId = context.Client.AccountEntry.Id;
                inventory.CharacterId = 42;
                database.SaveChanges();
            }
            item.OwnerId = 42;

            CreateAuctionManager(context).RequestAuctionBuyout(context.Client,
                new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)item.EntityId),
                    Price = 50
                });

            using var verify = context.Open();
            Assert.AreEqual(100,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.IsNotNull(verify.AuctionEntries.AsNoTracking()
                .SingleOrDefault(entry => entry.ItemId == item.Id));
            Assert.AreEqual((uint)InventoryType.AuctionInventory,
                verify.CharacterInventoryEntries.AsNoTracking()
                    .Single(entry => entry.ItemId == item.Id).InventoryType);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);
        }

        [TestMethod]
        public void AuctionBuyoutWinningExpiryRaceKeepsItemWithBuyerAndPaysSellerOnce()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            ExpireAuction(context, item);
            var manager = CreateAuctionManager(context);
            using var saveEntered = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            using var expiryStarted = new ManualResetEventSlim();
            var blocked = 0;
            context.BeforeSave = _ =>
            {
                if (Interlocked.CompareExchange(ref blocked, 1, 0) != 0)
                    return;

                saveEntered.Set();
                releaseSave.Wait();
            };

            var buyout = Task.Run(() => Buyout(manager, context.Client, item));
            Assert.IsTrue(saveEntered.Wait(5000), "Buyout did not reach its transaction.");
            var expiry = Task.Run(() =>
            {
                expiryStarted.Set();
                manager.ExpireAuctions();
            });
            Assert.IsTrue(expiryStarted.Wait(5000), "Expiry did not start.");
            Assert.IsFalse(expiry.Wait(100), "Expiry crossed the in-progress buyout.");
            releaseSave.Set();
            Task.WaitAll(buyout, expiry);

            AssertBoughtExactlyOnce(context, item);
            Assert.AreEqual(0, seller.Player.Inventory.InboxItems.Count);
            Assert.AreEqual(0, seller.Player.Inventory.PersonalInventory.Count(id => id == item.EntityId));
        }

        [TestMethod]
        public void AuctionBuyoutWinningCancellationRaceKeepsItemWithBuyerAndPaysSellerOnce()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            var manager = CreateAuctionManager(context);
            using var saveEntered = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            using var cancelStarted = new ManualResetEventSlim();
            var blocked = 0;
            context.BeforeSave = _ =>
            {
                if (Interlocked.CompareExchange(ref blocked, 1, 0) != 0)
                    return;

                saveEntered.Set();
                releaseSave.Wait();
            };

            var buyout = Task.Run(() => Buyout(manager, context.Client, item));
            Assert.IsTrue(saveEntered.Wait(5000), "Buyout did not reach its transaction.");
            var cancel = Task.Run(() =>
            {
                cancelStarted.Set();
                Cancel(manager, seller, item);
            });
            Assert.IsTrue(cancelStarted.Wait(5000), "Cancellation did not start.");
            Assert.IsFalse(cancel.Wait(100), "Cancellation crossed the in-progress buyout.");
            releaseSave.Set();
            Task.WaitAll(buyout, cancel);

            AssertBoughtExactlyOnce(context, item);
            Assert.AreEqual(0, seller.Player.Inventory.PersonalInventory.Count(id => id == item.EntityId));
        }

        [TestMethod]
        public void AuctionExpiryWinningBuyoutRaceReturnsItemWithoutChargingOrDelivering()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            ExpireAuction(context, item);
            var manager = CreateAuctionManager(context);
            using var saveEntered = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            using var buyoutStarted = new ManualResetEventSlim();
            var blocked = 0;
            context.BeforeSave = _ =>
            {
                if (Interlocked.CompareExchange(ref blocked, 1, 0) != 0)
                    return;

                saveEntered.Set();
                releaseSave.Wait();
            };

            var expiry = Task.Run(manager.ExpireAuctions);
            Assert.IsTrue(saveEntered.Wait(5000), "Expiry did not reach its transaction.");
            var buyout = Task.Run(() =>
            {
                buyoutStarted.Set();
                Buyout(manager, context.Client, item);
            });
            Assert.IsTrue(buyoutStarted.Wait(5000), "Buyout did not start.");
            Assert.IsFalse(buyout.Wait(100), "Buyout crossed the in-progress expiry.");
            releaseSave.Set();
            Task.WaitAll(expiry, buyout);

            AssertReturnedWithoutBuyout(context, item, InventoryType.InboxInventory);
        }

        [TestMethod]
        public void AuctionCancellationWinningBuyoutRaceReturnsItemWithoutChargingOrDelivering()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            var manager = CreateAuctionManager(context);
            using var saveEntered = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            using var buyoutStarted = new ManualResetEventSlim();
            var blocked = 0;
            context.BeforeSave = _ =>
            {
                if (Interlocked.CompareExchange(ref blocked, 1, 0) != 0)
                    return;

                saveEntered.Set();
                releaseSave.Wait();
            };

            var cancel = Task.Run(() => Cancel(manager, seller, item));
            Assert.IsTrue(saveEntered.Wait(5000), "Cancellation did not consume its listing.");
            var buyout = Task.Run(() =>
            {
                buyoutStarted.Set();
                Buyout(manager, context.Client, item);
            });
            Assert.IsTrue(buyoutStarted.Wait(5000), "Buyout did not start.");
            Assert.IsFalse(buyout.Wait(100), "Buyout crossed the in-progress cancellation.");
            releaseSave.Set();
            Task.WaitAll(cancel, buyout);

            AssertReturnedWithoutBuyout(context, item, InventoryType.Personal);
            Assert.AreEqual(1, seller.Player.Inventory.PersonalInventory.Count(id => id == item.EntityId));
        }

        [TestMethod]
        public void RepeatedAuctionExpiryConsumesListingAndReturnsItemOnce()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            ExpireAuction(context, item);
            var manager = CreateAuctionManager(context);

            manager.ExpireAuctionsFor(seller);
            manager.ExpireAuctions();

            AssertReturnedWithoutBuyout(context, item, InventoryType.InboxInventory);
        }

        [TestMethod]
        public void RepeatedAuctionCancellationConsumesListingAndReturnsItemOnce()
        {
            using var context = CreateAuctionRace(out var item, out var seller);
            var manager = CreateAuctionManager(context);

            Cancel(manager, seller, item);
            Cancel(manager, seller, item);

            AssertReturnedWithoutBuyout(context, item, InventoryType.Personal);
            Assert.AreEqual(1, seller.Player.Inventory.PersonalInventory.Count(id => id == item.EntityId));
        }

        private static WeaponAmmoContext CreateAuctionRace(out Item item, out Client seller)
        {
            var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }

            item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId: 99, price: 50);
            seller = context.World.CreateClient(factory: context);
            seller.Player.Id = 99;
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(seller,
                new GameAccountEntry { Id = 2, SelectedSlot = 1 });
            seller.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
            seller.Player.Inventory.AuctionItems.Add(item.EntityId);
            return context;
        }

        private static void ExpireAuction(WeaponAmmoContext context, Item item)
        {
            using var database = context.Open();
            database.AuctionEntries.Single(entry => entry.ItemId == item.Id).CreatedAt =
                System.DateTime.UtcNow.AddDays(-1);
            database.SaveChanges();
        }

        private static void Buyout(AuctionHouseManager manager, Client buyer, Item item) =>
            manager.RequestAuctionBuyout(buyer, new RequestAuctionBuyoutPacket
            {
                ItemId = checked((uint)item.EntityId),
                Price = 50
            });

        private static void Cancel(AuctionHouseManager manager, Client seller, Item item) =>
            manager.RequestCancelAuction(seller, new RequestCancelAuctionPacket
            {
                ItemEntityId = item.EntityId
            });

        private static void AssertBoughtExactlyOnce(WeaponAmmoContext context, Item item)
        {
            using var verify = context.Open();
            Assert.AreEqual(50,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.AreEqual(50,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 99).Credit);
            Assert.AreEqual(0, verify.AuctionEntries.AsNoTracking()
                .Count(entry => entry.ItemId == item.Id));
            var inventory = verify.CharacterInventoryEntries.AsNoTracking()
                .Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(42u, inventory.CharacterId);
            Assert.AreEqual((uint)InventoryType.InboxInventory, inventory.InventoryType);
            Assert.AreEqual(42u, item.OwnerId);
            Assert.AreEqual(1, context.Client.Player.Inventory.InboxItems
                .Count(entityId => entityId == item.EntityId));
        }

        private static void AssertReturnedWithoutBuyout(
            WeaponAmmoContext context,
            Item item,
            InventoryType inventoryType)
        {
            using var verify = context.Open();
            Assert.AreEqual(100,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 42).Credit);
            Assert.AreEqual(0,
                verify.CharacterEntries.AsNoTracking().Single(entry => entry.Id == 99).Credit);
            Assert.AreEqual(0, verify.AuctionEntries.AsNoTracking()
                .Count(entry => entry.ItemId == item.Id));
            var inventory = verify.CharacterInventoryEntries.AsNoTracking()
                .Single(entry => entry.ItemId == item.Id);
            Assert.AreEqual(99u, inventory.CharacterId);
            Assert.AreEqual((uint)inventoryType, inventory.InventoryType);
            Assert.AreEqual(99u, item.OwnerId);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems
                .Count(entityId => entityId == item.EntityId));
        }

        private static AuctionHouseManager CreateAuctionManager(WeaponAmmoContext context) =>
            (AuctionHouseManager)typeof(AuctionHouseManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { context });
    }
}
