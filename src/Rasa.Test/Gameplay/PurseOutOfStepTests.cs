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
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    // A purse that no longer holds what its character's row does.
    //
    // The row is what the server keeps and the purse is the session's copy of it: every credit
    // write pays the row first and the purse after, and most of them check the purse against the
    // row before they write. A write that paid the row alone (an auction sale to a seller on a
    // loading screen did) left the purse behind until the next login. Every write that checks
    // the two was refused without a word in the meantime, and the ones that did not check wrote
    // the old purse over the row.
    //
    // Where a write finds the two apart, the purse is put back to the row, the client is shown
    // it and the player is told to try again; and nothing writes a purse over a row that holds
    // something else.
    [TestClass]
    [DoNotParallelize]
    public class PurseOutOfStepTests
    {
        /// <summary>What the row holds that the purse does not: a sale paid to the row alone.</summary>
        private const int Sale = 30;

        #region Writes that check the purse against the row

        [TestMethod]
        public void AVendorChargeOnAPurseBehindItsRowPutsThePurseRight()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var characters = new CharacterManager(context);

            Behind(context, client.Player.Id, Credit(client) + Sale);

            Assert.IsFalse(characters.UpdateCharacter(client, CharacterUpdate.Credits, -20), "refused: the purse is not the row");
            AssertPutRight(client, 130, Sale);
            Assert.AreEqual(130, context.ReadRewardTotals().Credits, "the row as it was");

            // And the next one goes through.
            Assert.IsTrue(characters.UpdateCharacter(client, CharacterUpdate.Credits, -20));
            Assert.AreEqual(110, Credit(client));
            Assert.AreEqual(110, context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        public void AMissionRewardOnAPurseBehindItsRowPutsThePurseRight()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;

            Behind(context, client.Player.Id, Credit(client) + Sale);

            Assert.IsFalse(context.Manager.CompleteOfferedMission(client, context.Receiver.EntityId, 429, 0, null));
            AssertPutRight(client, 130, Sale);

            // Turned in again: 7 credits and 3 prestige.
            Assert.IsTrue(context.Manager.CompleteOfferedMission(client, context.Receiver.EntityId, 429, 0, null));
            Assert.AreEqual(137, Credit(client));
            Assert.AreEqual(137, context.ReadRewardTotals().Credits);
            Assert.AreEqual(53, context.ReadRewardTotals().Prestige);
        }

        [TestMethod]
        public void LootOnAPurseBehindItsRowPutsThePurseRight()
        {
            using var context = new LootConsolidationTests.LootFixture();
            var client = context.Client;

            Behind(context.Storage, client.Player.Id, 100 + Sale);
            LootAll(context);

            Assert.IsFalse(context.Loot.FullyLooted, "refused");
            AssertPutRight(client, 130, Sale);

            LootAll(context);

            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(137, Credit(client));
            Assert.AreEqual(137, Row(context.Storage, client.Player.Id));
        }

        [TestMethod]
        public void LootSharedWithASquadMateWhosePurseIsBehindPutsTheirsRight()
        {
            using var context = new LootConsolidationTests.LootFixture();
            var mate = context.Storage.World.CreateClient(factory: context.Storage);

            AddCharacter(context.Storage, mate, Sale);
            mate.Player.Credits[CurencyType.Credits] = 0;
            mate.Player.Credits[CurencyType.Prestige] = 0;
            context.Loot.CreditSharers.Add(mate.Player.EntityId);
            WorldTestContext.Drain(mate);

            // The claim writes the mate's share too, checked against their row.
            LootAll(context);

            Assert.IsFalse(context.Loot.FullyLooted, "refused");
            AssertPutRight(mate, Sale, Sale);

            LootAll(context);

            Assert.IsTrue(context.Loot.FullyLooted);
            var share = Credit(mate) - Sale;
            Assert.IsTrue(share > 0, "a share of the corpse's 7");
            Assert.AreEqual(Credit(mate), Row(context.Storage, mate.Player.Id));
            Assert.AreEqual(107 - share, Credit(context.Client));
        }

        [TestMethod]
        public void ABuyoutByABuyerWhosePurseIsBehindPutsTheirsRight()
        {
            using var auction = new Auction(sellerState: null);

            Behind(auction.Context, Auction.Buyer, 100 + Sale);
            auction.Buy();

            Assert.AreEqual(0, auction.Row(Auction.SellerId), "refused");
            AssertPutRight(auction.Context.Client, 130, Sale);

            auction.Buy();

            Assert.AreEqual(80, auction.Row(Auction.Buyer));
            Assert.AreEqual(80, Credit(auction.Context.Client));
            Assert.AreEqual(50, auction.Row(Auction.SellerId));
        }

        [TestMethod]
        public void ABuyoutOfASellerWhosePurseIsBehindPutsTheirsRight()
        {
            using var auction = new Auction(ClientState.Ingame);

            Behind(auction.Context, Auction.SellerId, Sale);
            auction.Buy();

            Assert.AreEqual(100, auction.Row(Auction.Buyer), "refused");
            AssertPutRight(auction.Seller, Sale, Sale);

            auction.Buy();

            Assert.AreEqual(Sale + 50, auction.Row(Auction.SellerId));
            Assert.AreEqual(Sale + 50, Credit(auction.Seller));
        }

        #endregion

        #region Writes that used to write the purse over the row

        [TestMethod]
        public void ALockboxDepositDoesNotWriteAPurseBehindItsRowOverIt()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context, context.Manager);

            Purse(context, client, 1100, Sale);
            using (var database = context.Open())
            {
                database.CharacterLockboxEntries.Add(new CharacterLockboxEntry(client.AccountEntry.Id, 0, 1));
                database.SaveChanges();
            }

            inventory.TransferCreditToLockbox(client, 500);

            // Not 600: the 30 the row held and the purse did not would have gone.
            Assert.AreEqual(1130, context.ReadRewardTotals().Credits);
            Assert.AreEqual(0, Lockbox(context, client));
            Assert.AreEqual(0, client.Player.LockboxCredits);
            AssertPutRight(client, 1130, Sale);

            inventory.TransferCreditToLockbox(client, 500);

            Assert.AreEqual(630, context.ReadRewardTotals().Credits);
            Assert.AreEqual(630, Credit(client));
            Assert.AreEqual(500, Lockbox(context, client));
        }

        [TestMethod]
        [DataRow(1u, DisplayName = "credits")]
        [DataRow(2u, DisplayName = "prestige")]
        public void AClanDepositDoesNotWriteAPurseBehindItsRowOverIt(uint creditType)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var clans = ClanManager.Instance;
            var previousClans = clans.Clans;
            var previousMembers = clans.ClanMembers;

            try
            {
                var client = context.Client;
                var inventory = new InventoryManager(context, context.Manager);
                var currency = creditType == 1 ? CurencyType.Credits : CurencyType.Prestige;
                var clan = context.CreateClanForPlayer();

                using (var unit = context.CreateChar())
                    if (currency == CurencyType.Credits)
                        unit.Characters.UpdateCharacterCurrencies(client.Player.Id, 1100 + Sale, 50);
                    else
                        unit.Characters.UpdateCharacterCurrencies(client.Player.Id, 100, 1100 + Sale);
                client.Player.Credits[currency] = 1100;
                context.Drain();

                inventory.ClanCreditTransfer(client, 500, creditType);

                Assert.AreEqual(1130, Held(context, client.Player.Id, currency), "the row as it was");
                Assert.AreEqual(0u, ClanHolds(context, clan.Id, currency));
                AssertPutRight(client, 1130, Sale, currency);

                inventory.ClanCreditTransfer(client, 500, creditType);

                Assert.AreEqual(630, Held(context, client.Player.Id, currency));
                Assert.AreEqual(630, client.Player.Credits[currency]);
                Assert.AreEqual(500u, ClanHolds(context, clan.Id, currency));
            }
            finally
            {
                clans.Clans = previousClans;
                clans.ClanMembers = previousMembers;
                context.Client.Player.ClanId = 0;
            }
        }

        [TestMethod]
        public void ATradeDoesNotWriteAPurseBehindItsRowOverIt()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            var factoryBefore = Server.GameUnitOfWorkFactory;

            try
            {
                Server.GameUnitOfWorkFactory = context;

                var initiator = context.Client;
                var target = context.World.CreateClient(factory: context);

                AddCharacter(context, target, 100);
                target.Player.Credits[CurencyType.Credits] = 100;
                target.Player.Credits[CurencyType.Prestige] = 0;
                initiator.Player.Credits[CurencyType.Prestige] = 0;
                Purse(context, initiator, 100, Sale);
                WorldTestContext.Drain(target);

                // The initiator gives 10.
                Complete(initiator, target, 10);

                Assert.AreEqual(130, Row(context, initiator.Player.Id), "not 90: the 30 would have gone");
                Assert.AreEqual(100, Row(context, target.Player.Id));
                Assert.AreEqual(100, Credit(target));
                AssertPutRight(initiator, 130, Sale);

                Complete(initiator, target, 10);

                Assert.AreEqual(120, Row(context, initiator.Player.Id));
                Assert.AreEqual(120, Credit(initiator));
                Assert.AreEqual(110, Row(context, target.Player.Id));
                Assert.AreEqual(110, Credit(target));
            }
            finally
            {
                Server.GameUnitOfWorkFactory = factoryBefore;
            }
        }

        #endregion

        #region A purse in step

        [TestMethod]
        public void APurseInStepIsLeftAlone()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;

            using (var unit = context.CreateChar())
                Assert.IsFalse(CharacterManager.PutPurseRight(client, unit.Characters.Find(client.Player.Id)), "nothing to put right");
            Assert.AreEqual(0, Sent(client).Count);

            Assert.IsTrue(new CharacterManager(context).UpdateCharacter(client, CharacterUpdate.Credits, -20));

            var sent = Sent(client);
            Assert.AreEqual(0, sent.OfType<SystemMessagePacket>().Count());
            Assert.AreEqual(-20, sent.OfType<UpdateCreditsPacket>().Single().Delta);
        }

        #endregion

        #region Fixture

        private static int Credit(Client client) => client.Player.Credits[CurencyType.Credits];

        /// <summary>The character's row holding <paramref name="credits"/>, which the purse does not.</summary>
        private static void Behind(IGameUnitOfWorkFactory context, uint characterId, int credits)
        {
            using var unit = context.CreateChar();
            var row = unit.Characters.Find(characterId);

            unit.Characters.UpdateCharacterCurrencies(characterId, credits, row.Prestige);
        }

        /// <summary>A purse of <paramref name="purse"/> over a row that holds <paramref name="behindBy"/> more.</summary>
        private static void Purse(IGameUnitOfWorkFactory context, Client client, int purse, int behindBy)
        {
            Behind(context, client.Player.Id, purse + behindBy);
            client.Player.Credits[CurencyType.Credits] = purse;
            WorldTestContext.Drain(client);
        }

        private static int Row(WeaponAmmoContext context, uint characterId)
        {
            using var database = context.Open();

            return database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == characterId).Credit;
        }

        private static int Held(MissionTestContext context, uint characterId, CurencyType currency)
        {
            using var database = context.Open();
            var row = database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == characterId);

            return currency == CurencyType.Credits ? row.Credit : row.Prestige;
        }

        private static int Lockbox(MissionTestContext context, Client client)
        {
            using var database = context.Open();

            return database.CharacterLockboxEntries.AsNoTracking().Single(entry => entry.AccountId == client.AccountEntry.Id).Credits;
        }

        private static uint ClanHolds(MissionTestContext context, uint clanId, CurencyType currency)
        {
            using var database = context.Open();
            var clan = database.ClanEntries.AsNoTracking().Single(entry => entry.Id == clanId);

            return currency == CurencyType.Credits ? clan.Credits : clan.Prestige;
        }

        /// <summary>A row for a test client's character, on an account of its own.</summary>
        private static void AddCharacter(WeaponAmmoContext context, Client client, int credits)
        {
            var accountId = 10 + client.Player.Id;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                new GameAccountEntry { Id = accountId, SelectedSlot = 1 });
            client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();

            using var database = context.Open();
            var account = new GameAccountEntry
            {
                Id = accountId, Name = $"account{accountId}", Email = $"account{accountId}@example.invalid", FamilyName = $"Family{accountId}"
            };

            database.GameAccountEntries.Add(account);
            database.CharacterEntries.Add(new CharacterEntry
            {
                Id = client.Player.Id, GameAccount = account, Name = client.Player.Name, Level = 1, Credit = credits
            });
            database.SaveChanges();
        }

        private static void LootAll(LootConsolidationTests.LootFixture context)
        {
            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            context.Drain();
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });
        }

        /// <summary>Both sides confirmed, the initiator giving <paramref name="credits"/>: the exchange.</summary>
        private static void Complete(Client initiator, Client target, int credits)
        {
            var session = new TradeSession(initiator, target) { Accepted = true, InitiatorCredits = credits };

            typeof(TradeManager).GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(TradeManager.Instance, new object[] { session });
        }

        private static List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message)
            .OfType<CallMethodMessage>()
            .Select(message => message.Packet)
            .ToList();

        /// <summary>The purse back to the row, the client shown the difference, and told to try again.</summary>
        private static void AssertPutRight(Client client, int row, int difference, CurencyType currency = CurencyType.Credits)
        {
            Assert.AreEqual(row, client.Player.Credits[currency], "the purse put back to the row");

            var sent = Sent(client);
            var shown = sent.OfType<UpdateCreditsPacket>().Single();

            Assert.AreEqual(currency, shown.Type);
            Assert.AreEqual(row, shown.Amount);
            Assert.AreEqual(difference, shown.Delta);
            StringAssert.Contains(sent.OfType<SystemMessagePacket>().Single().TextMessage, "try again");
        }

        /// <summary>One listing of character 99's, 50 credits, and a buyer, 42, with 100.</summary>
        private sealed class Auction : IDisposable
        {
            internal const uint Buyer = 42;
            internal const uint SellerId = 99;

            internal WeaponAmmoContext Context { get; } = new(characterId: Buyer);
            internal Client Seller { get; }
            private readonly Item _item;

            internal Auction(ClientState? sellerState)
            {
                Context.Client.Player.Credits[CurencyType.Credits] = 100;
                Context.Client.Player.Credits[CurencyType.Prestige] = 0;
                using (var database = Context.Open())
                {
                    database.CharacterEntries.Single(entry => entry.Id == Buyer).Credit = 100;
                    database.SaveChanges();
                }

                _item = Context.AddUnownedLoot(1);
                Context.AddAuction(_item, SellerId, 50);

                if (sellerState == null)
                    return;

                Seller = Context.World.CreateClient(factory: Context);
                Seller.State = sellerState.Value;
                Seller.Player.Id = SellerId;
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(Seller,
                    new GameAccountEntry { Id = 2, SelectedSlot = 1 });
                Seller.Player.Credits[CurencyType.Credits] = 0;
                Seller.Player.Credits[CurencyType.Prestige] = 0;
                Seller.Player.Inventory.AuctionItems.Add(_item.EntityId);

                lock (Server.Clients)
                    Server.Clients.Add(Seller);
            }

            internal void Buy()
            {
                var manager = (AuctionHouseManager)typeof(AuctionHouseManager)
                    .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(IGameUnitOfWorkFactory) }, null)
                    .Invoke(new object[] { Context });

                manager.RequestAuctionBuyout(Context.Client, new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)_item.EntityId),
                    Price = 50
                });
            }

            internal int Row(uint characterId) => PurseOutOfStepTests.Row(Context, characterId);

            public void Dispose()
            {
                if (Seller != null)
                    lock (Server.Clients)
                        Server.Clients.Remove(Seller);

                Context.Dispose();
            }
        }

        #endregion
    }
}
