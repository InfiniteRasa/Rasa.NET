using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The clan lockbox window's tab count, balance and history reach the client on the lockbox's
    /// own entity, which is the only place its client handles them (clanlockbox.py): sent to the
    /// player when they open one (InventoryManager.SendClanLockboxState), and to the clan on every
    /// lockbox when one of them changes. The tab count went to the client's inventory manager and
    /// the history to its clan manager, and neither arrived.
    /// </summary>
    [TestClass]
    public class ClanLockboxStateTests
    {
        [TestMethod]
        public void OpeningALockboxSendsItsTabsBalanceAndHistoryOnTheLockbox()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var lockbox = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };
            ClanEntry clan;

            using (var unit = context.CreateChar())
            {
                clan = unit.Clans.CreateClan("Red Company", true);
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, client.Player.Id, 3, ""));
                unit.Clans.UpdatePrestige(clan.Id, 40);
                unit.Clans.UpdateCredits(clan.Id, 7000);
                unit.Clans.UpdatePurashedTabs(clan.Id, 3);
                unit.ClanLockboxLogs.Add(ClanLockboxLogEntry.ForCredits(clan.Id, InventoryTransactionType.Deposit,
                    client.Player.Id, client.Player.Name, client.Player.FamilyName, (byte)CurencyType.Credits, 7000));
            }

            client.Player.ClanId = clan.Id;
            EntityManager.Instance.RegisterDynamicObject(lockbox);

            try
            {
                WorldTestContext.Drain(client);

                inventory.SendClanLockboxState(client, lockbox.EntityId);

                var calls = Calls(client);

                var tabs = calls.Single(c => c.Packet is UpdateClanLockboxTabCountPacket);
                var funds = calls.Single(c => c.Packet is UpdateClanLockboxCreditsPacket);
                var logs = calls.Single(c => c.Packet is ClanLockboxLogsPacket);

                Assert.AreEqual(lockbox.EntityId, tabs.EntityId, "the tab count, on the lockbox");
                Assert.AreEqual(3u, ((UpdateClanLockboxTabCountPacket)tabs.Packet).Count);
                Assert.AreEqual(lockbox.EntityId, funds.EntityId, "the balance, on the lockbox");
                CollectionAssert.AreEqual(new uint[] { 7000, 40 }, ((UpdateClanLockboxCreditsPacket)funds.Packet).ListCredits);
                Assert.AreEqual(lockbox.EntityId, logs.EntityId, "the history, on the lockbox");
                Assert.AreEqual(1, ((ClanLockboxLogsPacket)logs.Packet).Logs.Count);
            }
            finally
            {
                EntityManager.Instance.UnregisterDynamicObject(lockbox.EntityId);
                EntityManager.Instance.FreeEntity(lockbox.EntityId);
            }
        }

        [TestMethod]
        public void AMapChangeShowsTheLockboxItemsAgain()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);

            // The client keeps its slot lists across a map change but loses every item entity, and
            // the arrival (ResendForMap) sends every other list's items again; the lockbox's it did
            // not, so the lockbox showed empty until the next login or the next change to it.
            var item = ItemManager.StageItem(ItemManager.Instance.GetItemTemplateById(28), 1, "");
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            try
            {
                client.Player.ClanId = 77;
                client.Player.Inventory.ResetClanInventory();
                client.Player.Inventory.ClanInventory[3] = item.EntityId;
                WorldTestContext.Drain(client);

                inventory.ResendForMap(client);

                var calls = Calls(client);
                var created = calls.FindIndex(c => c.Packet is CreatePhysicalEntityPacket p && p.EntityId == item.EntityId);
                var reloaded = calls.FindIndex(c => c.Packet is InventoryReloadPacket p && p.InventoryType == InventoryType.ClanInventory);

                Assert.IsTrue(created >= 0, "the item's entity again");
                Assert.IsTrue(calls.Any(c => c.EntityId == item.EntityId && c.Packet is ItemInfoPacket), "and its data");
                Assert.IsTrue(reloaded > created, "then the list, with the items in place");

                var reload = (InventoryReloadPacket)calls[reloaded].Packet;
                Assert.AreEqual((int)ClanLockboxTab.TotalSlots, reload.Items.Count);
                Assert.AreEqual(item.EntityId, reload.Items[3]);
                Assert.AreEqual(1, calls.Count(c => c.Packet is InventoryReloadPacket), "once");

                // A member of no clan is sent no lockbox.
                client.Player.ClanId = 0;
                WorldTestContext.Drain(client);
                inventory.ResendForMap(client);
                Assert.IsFalse(Calls(client).Any(c => c.Packet is InventoryReloadPacket p && p.InventoryType == InventoryType.ClanInventory));

                // Nor is one whose lockbox list was never built: the login builds it.
                client.Player.ClanId = 77;
                client.Player.Inventory.ClanInventory.Clear();
                WorldTestContext.Drain(client);
                inventory.ResendForMap(client);
                Assert.IsFalse(Calls(client).Any(c => c.Packet is InventoryReloadPacket p && p.InventoryType == InventoryType.ClanInventory));
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(item.EntityId);
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.FreeEntity(item.EntityId);
            }
        }

        [TestMethod]
        public void AMemberInNoClanIsSentNothing()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);

            client.Player.ClanId = 0;
            WorldTestContext.Drain(client);

            inventory.SendClanLockboxState(client, 12345);

            Assert.AreEqual(0, Calls(client).Count);
        }

        [TestMethod]
        public void ATabBoughtIsAnnouncedOnEveryLockboxToTheClan()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var clanInstance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = clanInstance.GetValue(null);
            var first = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };
            var second = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };
            ClanEntry clan;

            using (var unit = context.CreateChar())
            {
                clan = unit.Clans.CreateClan("Red Company", true);
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, client.Player.Id, 3, ""));
                unit.Clans.UpdatePrestige(clan.Id, 100000);
            }

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                clanInstance.SetValue(null, clans);
                client.Player.ClanId = clan.Id;

                lock (Server.Clients)
                    Server.Clients.Add(client);

                EntityManager.Instance.RegisterDynamicObject(first);
                EntityManager.Instance.RegisterDynamicObject(second);
                WorldTestContext.Drain(client);

                inventory.PurchaseClanLockboxTab(client, new Rasa.Packets.Clan.Client.PurchaseClanLockboxTabPacket { TabId = 2 });

                var calls = Calls(client);
                var tabs = calls.Where(c => c.Packet is UpdateClanLockboxTabCountPacket).ToList();

                CollectionAssert.AreEquivalent(new[] { first.EntityId, second.EntityId }, tabs.Select(c => c.EntityId).ToList(), "once per lockbox, each on its own entity");
                Assert.IsTrue(tabs.All(c => ((UpdateClanLockboxTabCountPacket)c.Packet).Count == 2));

                // Tab 2 is paid for in prestige; the open window shows what is left.
                var price = ClanLockboxTab.Price(2);
                var funds = calls.Where(c => c.Packet is UpdateClanLockboxCreditsPacket).ToList();

                CollectionAssert.AreEquivalent(new[] { first.EntityId, second.EntityId }, funds.Select(c => c.EntityId).ToList(), "the balance, once per lockbox");
                Assert.IsTrue(funds.All(c => ((UpdateClanLockboxCreditsPacket)c.Packet).ListCredits[1] == 100000 - price));
            }
            finally
            {
                clanInstance.SetValue(null, previous);

                lock (Server.Clients)
                    Server.Clients.Remove(client);

                foreach (var lockbox in new[] { first, second })
                {
                    EntityManager.Instance.UnregisterDynamicObject(lockbox.EntityId);
                    EntityManager.Instance.FreeEntity(lockbox.EntityId);
                }
            }
        }

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();
    }
}
