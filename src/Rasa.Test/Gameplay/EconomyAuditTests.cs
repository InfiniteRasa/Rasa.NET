extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Repositories.Char.EconomyLog;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    /// <summary>
    /// The economy log (EconomyAudit, the economy_log table): what changes hands - items, credits
    /// and prestige - between players and between a player and the world, an event a transfer id
    /// and a row for each thing on each side it moved.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EconomyAuditTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>The log kept in memory: each event's rows, as they were given.</summary>
        internal sealed class MemoryStore : EconomyAudit.IStore
        {
            public List<List<EconomyLogEntry>> Events { get; } = new List<List<EconomyLogEntry>>();

            public List<EconomyLogEntry> Rows => Events.SelectMany(rows => rows).ToList();

            /// <summary>Set, every call throws it.</summary>
            public Exception Fails { get; set; }

            public int Add(IReadOnlyCollection<EconomyLogEntry> entries)
            {
                if (Fails != null)
                    throw Fails;

                Events.Add(entries.ToList());

                return entries.Count;
            }
        }

        private MemoryStore _store;
        private EconomyAudit.IStore _previousStore;
        private Func<DateTime> _previousClock;
        private Func<string> _previousTransferId;
        private int _transfers;

        [TestInitialize]
        public void KeepTheLogInMemory()
        {
            var audit = EconomyAudit.Instance;

            _previousStore = audit.Store;
            _previousClock = audit.UtcNow;
            _previousTransferId = audit.NewTransferId;
            _store = new MemoryStore();
            _transfers = 0;
            audit.Load(_store);
            audit.UtcNow = () => Noon;
            audit.NewTransferId = () => $"transfer{++_transfers}";
        }

        [TestCleanup]
        public void RestoreTheLog()
        {
            var audit = EconomyAudit.Instance;

            audit.Load(_previousStore);
            audit.UtcNow = _previousClock;
            audit.NewTransferId = _previousTransferId;
        }

        #region The log itself

        [TestMethod]
        public void AnEventIsOneTransferAndLeavesOutWhatDidNotMove()
        {
            var id = EconomyAudit.Instance.Record(EconomyLogKind.Trade, 5,
                EconomyAudit.ItemLine(10, 1, 700, 28, -3, 20, 1220),
                EconomyAudit.ItemLine(20, 2, 700, 28, 3, 10),
                EconomyAudit.MoneyLine(10, 1, CurencyType.Credits, 0, 90),
                EconomyAudit.MoneyLine(20, 2, CurencyType.Prestige, -15, 85, 10),
                EconomyAudit.ItemLine(0, 1, 701, 28, 1),
                null);

            Assert.AreEqual("transfer1", id);

            var rows = _store.Events.Single();

            Assert.HasCount(3, rows, "no sum of nothing, no character, no line");
            Assert.IsTrue(rows.All(row => row.TransferId == "transfer1" && row.CreatedAt == Noon && row.Kind == (byte)EconomyLogKind.Trade && row.ReferenceId == 5));

            Assert.AreEqual((10u, 1u, 20u, 700u, 28u, -3, (byte)0, 0L, 1220u), (rows[0].CharacterId, rows[0].AccountId, rows[0].OtherCharacterId, rows[0].ItemId, rows[0].ItemTemplateId, rows[0].Quantity, rows[0].Currency, rows[0].Amount, rows[0].MapContextId));
            Assert.AreEqual((20u, 3), (rows[1].CharacterId, rows[1].Quantity));
            Assert.AreEqual((20u, 0u, 0, (byte)CurencyType.Prestige, -15L, 85L, 10u), (rows[2].CharacterId, rows[2].ItemId, rows[2].Quantity, rows[2].Currency, rows[2].Amount, rows[2].Balance, rows[2].OtherCharacterId));

            // Nothing that moved is nothing on the log.
            Assert.IsNull(EconomyAudit.Instance.Record(EconomyLogKind.Loot, 0, EconomyAudit.MoneyLine(10, 1, CurencyType.Credits, 0, 0)));
            Assert.IsNull(EconomyAudit.Instance.Record(EconomyLogKind.Loot, 0));
            Assert.IsNull(EconomyAudit.Instance.Record(EconomyLogKind.Loot, 0, (IEnumerable<EconomyLogEntry>)null));
            Assert.HasCount(1, _store.Events);
        }

        [TestMethod]
        public void ALogThatCannotBeWrittenStopsNothingAndNoLogKeepsNone()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var credits = Credit(client);

            _store.Fails = new InvalidOperationException("the database is away");

            Assert.IsNull(EconomyAudit.Instance.Record(EconomyLogKind.VendorRepair, 0, EconomyAudit.MoneyLine(client, CurencyType.Credits, -5)));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(client, context.Receiver.EntityId, 429, 0, null), "the mission is rewarded all the same");
            Assert.AreEqual(credits + 7, Credit(client));
            Assert.IsEmpty(_store.Events);

            EconomyAudit.Instance.Load(null);
            Assert.IsNull(EconomyAudit.Instance.Record(EconomyLogKind.VendorRepair, 0, EconomyAudit.MoneyLine(client, CurencyType.Credits, -5)));
        }

        [TestMethod]
        public void ALineIsTheCharactersWithWhatTheyNowHold()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = 77 });
            client.Player.Credits[CurencyType.Credits] = 120;

            var line = EconomyAudit.MoneyLine(client, CurencyType.Credits, 20);

            Assert.AreEqual((client.Player.Id, 77u, 20L, 120L, world.Map.MapInfo.MapContextId), (line.CharacterId, line.AccountId, line.Amount, line.Balance, line.MapContextId));
            Assert.IsNull(EconomyAudit.MoneyLine(null, CurencyType.Credits, 20));
            Assert.IsNull(EconomyAudit.ItemLine(client, null, 1));
            Assert.IsNull(EconomyAudit.ItemLine(world.CreateClient(), null, 1));
        }

        #endregion

        #region Between players

        [TestMethod]
        public void ATradeIsARowOnEachSideOfEachItemAndEachSum()
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
                SetCredits(context, initiator, 100);
                WorldTestContext.Drain(target);

                var ammo = context.AddAmmo(5);
                var session = new TradeSession(initiator, target) { Accepted = true, InitiatorCredits = 10, TargetCredits = 4 };

                session.InitiatorItems.Add(new TradeSession.OfferedItem(ammo));
                typeof(TradeManager).GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(TradeManager.Instance, new object[] { session });

                Assert.AreEqual(94, Credit(initiator), "the trade went through");

                var a = initiator.Player.Id;
                var b = target.Player.Id;
                var rows = _store.Events.Single();

                Assert.IsTrue(rows.All(row => row.Kind == (byte)EconomyLogKind.Trade));
                CollectionAssert.AreEquivalent(new[]
                {
                    $"{a} item {ammo.Id} -5 from {b}",
                    $"{b} item {ammo.Id} 5 from {a}",
                    $"{a} credits -10 =94 from {b}",
                    $"{b} credits 10 =106 from {a}",
                    $"{b} credits -4 =106 from {a}",
                    $"{a} credits 4 =94 from {b}"
                }, rows.Select(Describe).ToList());
            }
            finally
            {
                Server.GameUnitOfWorkFactory = factoryBefore;
            }
        }

        [TestMethod]
        public void ABuyoutIsTheItemAndThePriceToTheBuyerAndThePriceToTheSeller()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            const uint sellerId = 99;
            var buyer = context.Client;

            SetCredits(context, buyer, 100);
            buyer.Player.Credits[CurencyType.Prestige] = 0;

            var item = context.AddUnownedLoot(1);
            context.AddAuction(item, sellerId, 50);

            var seller = context.World.CreateClient(factory: context);
            seller.State = ClientState.Ingame;
            seller.Player.Id = sellerId;
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(seller, new GameAccountEntry { Id = 2, SelectedSlot = 1 });
            seller.Player.Credits[CurencyType.Credits] = 0;
            seller.Player.Credits[CurencyType.Prestige] = 0;
            seller.Player.Inventory.AuctionItems.Add(item.EntityId);

            lock (Server.Clients)
                Server.Clients.Add(seller);

            try
            {
                Auctions(context).RequestAuctionBuyout(buyer, new RequestAuctionBuyoutPacket
                {
                    ItemId = checked((uint)item.EntityId),
                    Price = 50
                });

                Assert.AreEqual(50, Credit(buyer), "bought");

                var rows = _store.Events.Single();

                Assert.IsTrue(rows.All(row => row.Kind == (byte)EconomyLogKind.AuctionSold && row.ReferenceId == item.Id));
                CollectionAssert.AreEquivalent(new[]
                {
                    $"42 item {item.Id} 1 from 99",
                    "42 credits -50 =50 from 99",
                    "99 credits 50 =50 from 42"
                }, rows.Select(Describe).ToList(), "the item left the seller when it was listed");
                Assert.AreEqual(2u, rows.Single(row => row.CharacterId == sellerId).AccountId);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(seller);
            }
        }

        #endregion

        #region Vendors

        [TestMethod]
        public void AVendorPurchaseIsTheItemInAndTheCurrencyItIsPricedInOut()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (npcs, vendor) = Counter(harness, 139);
            Fund(harness, credits: 1000, prestige: 100);
            _store.Events.Clear();

            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntityId = EntityManager.Instance.VendorItems[vendor.EntityId].Single(),
                Quantity = 5
            });

            var rows = _store.Events.Single();
            var id = harness.Client.Player.Id;
            var bought = harness.Client.Player.Inventory.PersonalInventory.Select(e => EntityManager.Instance.GetItem(e))
                .Single(i => i?.ItemTemplate?.ItemTemplateId == SnowballTemplate);

            Assert.IsTrue(rows.All(row => row.Kind == (byte)EconomyLogKind.VendorBuy && row.ReferenceId == 139));
            CollectionAssert.AreEquivalent(new[]
            {
                $"{id} item {bought.Id} 5 from 0",
                $"{id} prestige -15 =85 from 0"
            }, rows.Select(Describe).ToList());
            Assert.AreEqual(SnowballTemplate, rows.Single(row => row.ItemId != 0).ItemTemplateId);
        }

        [TestMethod]
        public void ASaleAndItsBuybackAreTheItemOutAndInAndTheCredits()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (npcs, vendor) = Counter(harness, 0);
            var vest = ToyTests.Grant(harness, VestTemplate);
            var price = vest.ItemTemplate.SellPrice;
            var id = harness.Client.Player.Id;
            Assert.IsGreaterThan(0, price, "sold for something");
            _store.Events.Clear();

            npcs.RequestVendorSale(harness.Client, new RequestVendorSalePacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntityId = vest.EntityId,
                Quantity = 1
            });

            var held = Credit(harness.Client);
            var sold = _store.Events.Single();

            Assert.IsTrue(sold.All(row => row.Kind == (byte)EconomyLogKind.VendorSell));
            CollectionAssert.AreEquivalent(new[]
            {
                $"{id} item {vest.Id} -1 from 0",
                $"{id} credits {price} ={held} from 0"
            }, sold.Select(Describe).ToList());

            npcs.RequestVendorBuyback(harness.Client, new RequestVendorBuybackPacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntityId = vest.EntityId
            });

            var back = _store.Events.Last();

            Assert.HasCount(2, _store.Events);
            Assert.IsTrue(back.All(row => row.Kind == (byte)EconomyLogKind.VendorBuyback));
            CollectionAssert.AreEquivalent(new[]
            {
                $"{id} item {vest.Id} 1 from 0",
                $"{id} credits -{price} ={held - price} from 0"
            }, back.Select(Describe).ToList());
        }

        [TestMethod]
        public void ARepairIsItsCostWithTheItemItMended()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var vest = ToyTests.Grant(harness, VestTemplate);
            vest.CurrentHitPoints = 10;
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var (npcs, vendor) = Counter(harness, 0);
            var cost = Durability.RepairCost(vest);
            _store.Events.Clear();

            npcs.RequestVendorRepair(harness.Client, new RequestVendorRepairPacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntitesId = { vest.EntityId }
            });

            var row = _store.Events.Single().Single();

            Assert.AreEqual((byte)EconomyLogKind.VendorRepair, row.Kind);
            Assert.AreEqual((-(long)cost, (long)Credit(harness.Client), vest.Id, VestTemplate, 0), (row.Amount, row.Balance, row.ItemId, row.ItemTemplateId, row.Quantity));
        }

        #endregion

        #region The world

        [TestMethod]
        public void AMissionRewardIsItsCreditsAndPrestigeUnderTheMission()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;

            Assert.IsTrue(context.Manager.CompleteOfferedMission(client, context.Receiver.EntityId, 429, 0, null));

            var rows = _store.Events.Single(e => e[0].Kind == (byte)EconomyLogKind.MissionReward);
            var totals = context.ReadRewardTotals();

            Assert.IsTrue(rows.All(row => row.ReferenceId == 429 && row.CharacterId == client.Player.Id && row.AccountId == client.AccountEntry.Id));
            CollectionAssert.AreEquivalent(new[]
            {
                $"{client.Player.Id} credits 7 ={totals.Credits} from 0",
                $"{client.Player.Id} prestige 3 ={totals.Prestige} from 0"
            }, rows.Select(Describe).ToList());
        }

        [TestMethod]
        public void LootedCreditsAreTheLootersShareAndEachSquadMatesFromTheLooter()
        {
            using var context = new LootConsolidationTests.LootFixture();
            var mate = context.Storage.World.CreateClient(factory: context.Storage);

            AddCharacter(context.Storage, mate, 0);
            mate.Player.Credits[CurencyType.Credits] = 0;
            mate.Player.Credits[CurencyType.Prestige] = 0;
            context.Loot.CreditSharers.Add(mate.Player.EntityId);
            WorldTestContext.Drain(mate);

            context.Manager.RequestCorpseLooting(context.Client, new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            context.Drain();
            context.Manager.RequestLootAllFromCorpse(context.Client, new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.IsTrue(context.Loot.FullyLooted);

            var looter = context.Client.Player.Id;
            var mateShare = Credit(mate);
            var rows = _store.Events.Single();

            Assert.IsTrue(rows.All(row => row.Kind == (byte)EconomyLogKind.Loot && row.ReferenceId == (uint)context.Loot.EntityClassId));
            CollectionAssert.AreEquivalent(new[]
            {
                $"{looter} credits {7 - mateShare} ={Credit(context.Client)} from 0",
                $"{mate.Player.Id} credits {mateShare} ={mateShare} from {looter}"
            }, rows.Select(Describe).ToList());
        }

        [TestMethod]
        public void AFeudKillIsWhatTheKillerGainedAndWhatTheVictimLost()
        {
            var change = PvpPrestige.Change;
            var bonus = PvpPrestige.WagerBonusPercent;

            PvpPrestige.Reset();
            PvpPrestige.WagerBonusPercent = _ => 0;
            PvpPrestige.Change = (client, amount) =>
            {
                client.Player.Credits[CurencyType.Prestige] += amount;
                return true;
            };

            try
            {
                using var world = new WorldTestContext();
                var killer = world.CreateClient();
                var victim = world.CreateClient(10, 0);

                killer.Player.Level = victim.Player.Level = 10;
                killer.Player.Credits[CurencyType.Prestige] = 5;
                victim.Player.Credits[CurencyType.Prestige] = 1000;

                Assert.AreEqual((30, 10), PvpPrestige.FeudKill(killer, victim));

                var rows = _store.Events.Single();

                Assert.IsTrue(rows.All(row => row.Kind == (byte)EconomyLogKind.PvpPrestige));
                CollectionAssert.AreEquivalent(new[]
                {
                    $"{killer.Player.Id} prestige 40 =45 from {victim.Player.Id}",
                    $"{victim.Player.Id} prestige -10 =990 from {killer.Player.Id}"
                }, rows.Select(Describe).ToList());
            }
            finally
            {
                PvpPrestige.Change = change;
                PvpPrestige.WagerBonusPercent = bonus;
                PvpPrestige.Reset();
            }
        }

        #endregion

        #region Banks

        [TestMethod]
        public void ALockboxDepositIsTheCreditsOutOfThePurse()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context, context.Manager);

            SetCredits(context, client, 1100);
            using (var database = context.Open())
            {
                database.CharacterLockboxEntries.Add(new CharacterLockboxEntry(client.AccountEntry.Id, 0, 1));
                database.SaveChanges();
            }

            inventory.TransferCreditToLockbox(client, 500);
            inventory.TransferCreditToLockbox(client, -500);

            Assert.AreEqual(1100, Credit(client));
            CollectionAssert.AreEqual(new[]
            {
                $"{client.Player.Id} credits -500 =600 from 0",
                $"{client.Player.Id} credits 500 =1100 from 0"
            }, _store.Rows.Select(Describe).ToList());
            Assert.IsTrue(_store.Rows.All(row => row.Kind == (byte)EconomyLogKind.LockboxCredits));
        }

        [TestMethod]
        [DataRow(1u, DisplayName = "credits")]
        [DataRow(2u, DisplayName = "prestige")]
        public void AClanBankDepositIsTheSumOutOfThePurseUnderTheClan(uint creditType)
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
                    unit.Characters.UpdateCharacterCurrencies(client.Player.Id, 1100, 1100);
                client.Player.Credits[CurencyType.Credits] = 1100;
                client.Player.Credits[CurencyType.Prestige] = 1100;
                context.Drain();
                _store.Events.Clear();

                inventory.ClanCreditTransfer(client, 500, creditType);

                var row = _store.Events.Single().Single();

                Assert.AreEqual(((byte)EconomyLogKind.ClanBank, clan.Id, (byte)currency, -500L, 600L), (row.Kind, row.ReferenceId, row.Currency, row.Amount, row.Balance));
            }
            finally
            {
                clans.Clans = previousClans;
                clans.ClanMembers = previousMembers;
                context.Client.Player.ClanId = 0;
            }
        }

        #endregion

        #region The table

        [TestMethod]
        public void TheServersLogIsTheCharacterDatabase()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;

            EconomyAudit.Instance.Load(new EconomyAudit.ServerStore(context));

            Assert.IsTrue(context.Manager.CompleteOfferedMission(client, context.Receiver.EntityId, 429, 0, null));
            EconomyAudit.Instance.Record(EconomyLogKind.VendorSell, 3, EconomyAudit.ItemLine(client.Player.Id, client.AccountEntry.Id, 9001, 28, -2));

            using var unit = context.CreateChar();
            var rows = unit.EconomyLogs.GetByCharacter(client.Player.Id, 10);

            Assert.HasCount(3, rows);
            Assert.AreEqual((byte)EconomyLogKind.VendorSell, rows[0].Kind, "newest first");
            Assert.AreEqual(("transfer1", "transfer1", "transfer2"), (rows[2].TransferId, rows[1].TransferId, rows[0].TransferId));
            Assert.AreEqual(Noon, rows[1].CreatedAt);
            Assert.AreEqual(-2, unit.EconomyLogs.GetByItem(9001).Single().Quantity);
        }

        [TestMethod]
        public void TheTableKeepsTheRowsItIsGiven()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            var database = Path.Combine(directory, "database");

            try
            {
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                    context.Database.Migrate();

                T Log<T>(Func<EconomyLogRepository, T> action)
                {
                    using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database);
                    return action(new EconomyLogRepository(context));
                }

                Assert.AreEqual(2, Log(r => r.Add(new[]
                {
                    new EconomyLogEntry
                    {
                        CreatedAt = Noon, TransferId = "a", Kind = (byte)EconomyLogKind.AuctionSold, ReferenceId = 700, CharacterId = 10, AccountId = 1,
                        OtherCharacterId = 20, ItemId = 700, ItemTemplateId = 28, Quantity = 1, MapContextId = 1220
                    },
                    new EconomyLogEntry
                    {
                        CreatedAt = Noon, TransferId = "a", Kind = (byte)EconomyLogKind.AuctionSold, ReferenceId = 700, CharacterId = 20,
                        OtherCharacterId = 10, Currency = 1, Amount = 3_000_000_000L, Balance = -1
                    }
                })));
                Assert.AreEqual(1, Log(r => r.Add(new[] { new EconomyLogEntry { CreatedAt = Noon.AddMinutes(1), TransferId = null, CharacterId = 10, Currency = 2, Amount = -5 } })));
                Assert.AreEqual(0, Log(r => r.Add(Array.Empty<EconomyLogEntry>())));

                CollectionAssert.AreEqual(new uint[] { 3, 1 }, Log(r => r.GetByCharacter(10, 10)).Select(e => e.Id).ToArray(), "newest first");
                CollectionAssert.AreEqual(new uint[] { 3 }, Log(r => r.GetByCharacter(10, 1)).Select(e => e.Id).ToArray());
                Assert.IsEmpty(Log(r => r.GetByCharacter(10, -1)));
                CollectionAssert.AreEqual(new uint[] { 1 }, Log(r => r.GetByItem(700)).Select(e => e.Id).ToArray());

                var item = Log(r => r.GetByItem(700)).Single();
                var sum = Log(r => r.GetByCharacter(20, 10)).Single();
                var plain = Log(r => r.GetByCharacter(10, 1)).Single();

                Assert.AreEqual((Noon, "a", (byte)EconomyLogKind.AuctionSold, 700u, 1u, 20u, 28u, 1, 1220u), (item.CreatedAt, item.TransferId, item.Kind, item.ReferenceId, item.AccountId, item.OtherCharacterId, item.ItemTemplateId, item.Quantity, item.MapContextId));
                Assert.AreEqual(((byte)1, 3_000_000_000L, -1L, 0u), (sum.Currency, sum.Amount, sum.Balance, sum.ItemId));
                Assert.AreEqual(("", (byte)2, -5L), (plain.TransferId, plain.Currency, plain.Amount));
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        [DataRow(typeof(SqliteCharContext))]
        [DataRow(typeof(MySqlCharContext))]
        public void TheMigrationAddsTheTableAndTakesItAway(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(PreviousMigration, Migration);
            var down = migrator.GenerateScript(Migration, PreviousMigration);

            StringAssert.Contains(up, "CREATE TABLE");
            StringAssert.Contains(up, "economy_log");
            foreach (var index in new[] { "character_id", "created_at", "item_id", "transfer_id" })
                StringAssert.Contains(up, $"economy_log_index_{index}");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "DROP TABLE");

            if (contextType == typeof(MySqlCharContext))
            {
                StringAssert.Contains(up, "`transfer_id` varchar(32) CHARACTER SET utf8mb4 NOT NULL");
                StringAssert.Contains(up, "`amount` bigint NOT NULL");
                StringAssert.Contains(up, "`id` int unsigned NOT NULL AUTO_INCREMENT");
            }
        }

        #endregion

        #region Fixture

        private const string PreviousMigration = "20261206000000_Add_account_hybrid_unlocks";
        private const string Migration = "20261209000000_Add_economy_log";
        private const uint SnowballTemplate = 131481;
        private const uint VestTemplate = 13186;

        private static int Credit(Client client) => client.Player.Credits[CurencyType.Credits];

        /// <summary>"character credits|prestige amount =balance from other", or "character item id quantity from other".</summary>
        internal static string Describe(EconomyLogEntry row) => row.ItemId != 0 && row.Currency == 0
            ? $"{row.CharacterId} item {row.ItemId} {row.Quantity} from {row.OtherCharacterId}"
            : $"{row.CharacterId} {(row.Currency == (byte)CurencyType.Credits ? "credits" : "prestige")} {row.Amount} ={row.Balance} from {row.OtherCharacterId}";

        /// <summary>The purse and the row both holding <paramref name="credits"/>.</summary>
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

        private static AuctionHouseManager Auctions(IGameUnitOfWorkFactory context) =>
            (AuctionHouseManager)typeof(AuctionHouseManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { context });

        /// <summary>A vendor of <paramref name="package"/> beside the player, selling snowballs at 3.</summary>
        private static (NpcManager Npcs, Creature Vendor) Counter(BootcampRuntimeTestHarness.Harness harness, uint package)
        {
            ToyTests.LoadTemplate(harness, SnowballTemplate);
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(package) { ItemPrice = 3, VendorItems = { SnowballTemplate } };

            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });

            return (npcs, npc);
        }

        /// <summary>Credits added, and prestige set to exactly this much.</summary>
        private static void Fund(BootcampRuntimeTestHarness.Harness harness, int credits, int prestige)
        {
            if (credits > 0)
                Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, credits));

            var held = harness.Client.Player.Credits[CurencyType.Prestige];

            if (held > prestige)
                Assert.IsTrue(ManifestationManager.Instance.LossCurrency(harness.Client, CurencyType.Prestige, held - prestige));
            else if (held < prestige)
                Assert.IsTrue(ManifestationManager.Instance.RefundCurrency(harness.Client, CurencyType.Prestige, prestige - held));
        }

        #endregion
    }
}
