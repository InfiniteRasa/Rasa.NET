using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// The control point Prestige Vendors sell the two respec tokens and the Experience Booster
    /// at 999, 500 and 150 prestige, not the Pistol they were seeded with (Cp_prestige_stock).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CpPrestigeStockTests
    {
        private const string Migration = "20270101000000_Cp_prestige_stock";
        private const string Previous = "20261231000000_Alia_das_npcs";

        /// <summary>A Prestige Supply Vendor on the same package that is not at a control point: Twin Pillars' generated one.</summary>
        private const uint OtherPrestigeVendor = 500003;

        private static readonly uint[] Tokens = CpPrestigeStock.Stock.Select(s => s.Template).ToArray();

        [TestMethod]
        public void TheFourSellTheTokensAtTheirPricesAndDownPutsThePistolBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            foreach (var vendor in CpPrestigeStock.Vendors)
            {
                Assert.AreEqual(CpPrestigeStock.PrestigePackage, Package(database, vendor), $"{vendor}");
                CollectionAssert.AreEqual(new[] { CpPrestigeStock.Placeholder }, Stock(database, vendor), $"{vendor} before");
            }
            CollectionAssert.AreEqual(new[] { 1, 1, 1 }, Prices(database));

            migrator.Migrate(Migration);
            foreach (var vendor in CpPrestigeStock.Vendors)
                CollectionAssert.AreEqual(Tokens, Stock(database, vendor), $"{vendor}: in counter order");
            CollectionAssert.AreEqual(new[] { 999, 500, 150 }, Prices(database));
            CollectionAssert.AreEqual(new[] { CpPrestigeStock.Placeholder }, Stock(database, OtherPrestigeVendor), "only the control points'");

            foreach (var sql in CpPrestigeStock.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            foreach (var vendor in CpPrestigeStock.Vendors)
                CollectionAssert.AreEqual(Tokens, Stock(database, vendor), $"{vendor}: once");
            CollectionAssert.AreEqual(new[] { 999, 500, 150 }, Prices(database), "once");

            migrator.Migrate(Previous);
            foreach (var vendor in CpPrestigeStock.Vendors)
                CollectionAssert.AreEqual(new[] { CpPrestigeStock.Placeholder }, Stock(database, vendor), $"{vendor} after Down");
            CollectionAssert.AreEqual(new[] { 1, 1, 1 }, Prices(database));
        }

        [TestMethod]
        public void APriceOrAVendorChangedSinceIsLeftAsItIs()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE itemtemplate SET buy_price = 7 WHERE id = {CpPrestigeStock.ExperienceBooster}");
            database.Context.Database.ExecuteSqlRaw("UPDATE vendor SET package_id = 13 WHERE id = 500308");

            migrator.Migrate(Migration);

            CollectionAssert.AreEqual(new[] { 999, 500, 7 }, Prices(database));
            CollectionAssert.AreEqual(new[] { CpPrestigeStock.Placeholder }, Stock(database, 500308), "no longer a prestige vendor");
            CollectionAssert.AreEqual(Tokens, Stock(database, 500100));
        }

        [TestMethod]
        public void TheCounterChargesEachTokensPriceInPrestige()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            foreach (var template in Tokens)
                ToyTests.LoadTemplate(harness, template);

            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(CpPrestigeStock.PrestigePackage);
            npc.Npc.Vendor.VendorItems.AddRange(Tokens);

            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });

            var counter = EntityManager.Instance.VendorItems[npc.EntityId].Select(id => EntityManager.Instance.GetItem(id)).ToList();
            CollectionAssert.AreEqual(Tokens, counter.Select(item => item.ItemTemplate.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { 999, 500, 150 }, counter.Select(item => npc.Npc.Vendor.PriceOf(item)).ToArray(), "as the window shows them");

            SetPrestige(harness, 999 + 500 + 150 + 100);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];

            Buy(harness, npcs, npc, counter[0]);
            Assert.AreEqual(500 + 150 + 100, harness.Client.Player.Credits[CurencyType.Prestige], "an Attribute Respec Token for 999");
            Buy(harness, npcs, npc, counter[1]);
            Assert.AreEqual(150 + 100, harness.Client.Player.Credits[CurencyType.Prestige], "a Skill Respec Token for 500");
            Buy(harness, npcs, npc, counter[2]);
            Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Prestige], "an Experience Booster for 150");

            foreach (var template in Tokens)
                Assert.AreEqual(1U, Held(harness, template), $"{template}");
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits], "no credits taken");

            harness.Drain();
            Buy(harness, npcs, npc, counter[2]);
            Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Prestige], "not a second booster with 100 left");
            Assert.AreEqual(1U, Held(harness, CpPrestigeStock.ExperienceBooster));
            Assert.IsTrue(harness.Drain().OfType<DisplayClientMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmInsufficientFunds));
        }

        private static void Buy(BootcampRuntimeTestHarness.Harness harness, NpcManager npcs, Creature vendor, Item item)
        {
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntityId = item.EntityId,
                Quantity = 1
            });
        }

        private static void SetPrestige(BootcampRuntimeTestHarness.Harness harness, int prestige)
        {
            var held = harness.Client.Player.Credits[CurencyType.Prestige];

            if (held > prestige)
                Assert.IsTrue(ManifestationManager.Instance.LossCurrency(harness.Client, CurencyType.Prestige, held - prestige));
            else if (held < prestige)
                Assert.IsTrue(ManifestationManager.Instance.RefundCurrency(harness.Client, CurencyType.Prestige, prestige - held));
        }

        private static uint Held(BootcampRuntimeTestHarness.Harness harness, uint template) =>
            (uint)harness.Client.Player.Inventory.PersonalInventory
                .Select(id => EntityManager.Instance.GetItem(id))
                .Where(item => item?.ItemTemplate?.ItemTemplateId == template)
                .Sum(item => item.StackSize);

        private static uint Package(OrdinaryLootTestDatabase database, uint vendor) =>
            database.Context.Database.SqlQueryRaw<uint>($"SELECT package_id AS Value FROM vendor WHERE id = {vendor}").Single();

        private static uint[] Stock(OrdinaryLootTestDatabase database, uint vendor) =>
            database.Context.Database.SqlQueryRaw<uint>($"SELECT item_template_id AS Value FROM vendor_item WHERE id = {vendor} ORDER BY rowid").ToArray();

        private static int[] Prices(OrdinaryLootTestDatabase database) =>
            Tokens.Select(template => database.Context.Database
                .SqlQueryRaw<int>($"SELECT buy_price AS Value FROM itemtemplate WHERE id = {template}").Single()).ToArray();
    }
}
