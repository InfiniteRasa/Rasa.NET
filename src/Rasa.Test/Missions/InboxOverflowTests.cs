using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    // What a full backpack cannot take goes to the inbox (InventoryManager.Inbox): mission reward
    // items, a crafting result or a GM's gift (SendToInbox), and items a GM mails (MailItems).
    [TestClass]
    [DoNotParallelize]
    public class InboxOverflowTests
    {
        private const uint Template = 28;      // class 3147, stacks of 50000, category 2 (MissionTestContext)
        private const uint Stack = 50000;

        private static uint[] InboxSlots(MissionTestContext context)
        {
            using var unit = context.CreateChar();
            return unit.CharacterInventories.GetItems(1)
                .Where(row => row.CharacterId == 1 && row.InventoryType == (uint)InventoryType.InboxInventory)
                .Select(row => row.SlotId).OrderBy(slot => slot).ToArray();
        }

        private static int PersonalRows(MissionTestContext context)
        {
            using var unit = context.CreateChar();
            return unit.CharacterInventories.GetItems(1).Count(row => row.CharacterId == 1 && row.InventoryType == (uint)InventoryType.Personal);
        }

        [TestMethod]
        public void RewardItemsThatDoNotFitTheBackpackGoToTheInbox()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using var grant = new InventoryManager.InventoryGrant();

            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                    new[] { new InventoryManager.InventoryItemGrant(Template, Stack * 52) }, unit));
            grant.Publish(context.Client);

            Assert.AreEqual(50, context.Client.Player.Inventory.PersonalInventory.Skip(50).Take(50).Count(id => id != 0), "the category is full");
            Assert.AreEqual(2, context.Client.Player.Inventory.InboxItems.Count);
            CollectionAssert.AreEqual(new uint[] { 0, 1 }, InboxSlots(context));
            Assert.AreEqual(50, PersonalRows(context));

            var packets = context.Drain();
            Assert.AreEqual(2, packets.OfType<AddInboxItemPacket>().Count());
            Assert.IsTrue(packets.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("pick-up box")));

            foreach (var entityId in context.Client.Player.Inventory.InboxItems)
                Assert.AreEqual(Stack, EntityManager.Instance.GetItem(entityId).StackSize);
        }

        [TestMethod]
        public void ARewardTooBigForTheBackpackAndTheInboxIsStillRefusedWhole()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using var grant = new InventoryManager.InventoryGrant();

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                    new[] { new InventoryManager.InventoryItemGrant(Template, Stack * (50 + Inventory.MaxInboxItems + 1)) }, unit));
            });

            Assert.AreEqual(0, PersonalRows(context));
            Assert.AreEqual(0, InboxSlots(context).Length);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);
        }

        [TestMethod]
        public void AnUnplacedItemGoesToTheFirstFreeInboxSlotUntilTheInboxIsFull()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var inventory = new InventoryManager(context);

            for (var i = 0; i < Inventory.MaxInboxItems; i++)
                Assert.IsTrue(inventory.SendToInbox(context.Client, context.CreateInventoryItem(Template, 3147, 5)), $"item {i}");

            Assert.AreEqual(Inventory.MaxInboxItems, InboxSlots(context).Length);
            Assert.AreEqual(Inventory.MaxInboxItems, context.Client.Player.Inventory.InboxItems.Count);
            Assert.AreEqual(Inventory.MaxInboxItems, context.Drain().OfType<AddInboxItemPacket>().Count());

            var extra = context.CreateInventoryItem(Template, 3147, 5);
            Assert.IsFalse(inventory.SendToInbox(context.Client, extra), "full");
            Assert.IsFalse(context.Client.Player.Inventory.InboxItems.Contains(extra.EntityId));
        }

        [TestMethod]
        public void AGmCanMailItemsToAPlayerWhoIsNotOnline()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var find = Moderation.FindStoredAccount;

            try
            {
                Moderation.FindStoredAccount = name =>
                {
                    using var unit = context.CreateChar();
                    return unit.GameAccounts.FindByFamilyName(name);
                };

                var result = new InventoryManager(context).MailItems("Family 1", Template, Stack * 2 + 20000, "Warden");

                Assert.IsTrue(result.Done, result.Text);
                CollectionAssert.AreEqual(new uint[] { 0, 1, 2 }, InboxSlots(context));
                Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count, "not online: rows only, loaded when they arrive");

                using var check = context.CreateChar();
                var counts = check.CharacterInventories.GetItems(1)
                    .Where(row => row.InventoryType == (uint)InventoryType.InboxInventory)
                    .Select(row => check.Items.GetItem(row.ItemId).StackSize).OrderBy(n => n).ToArray();
                CollectionAssert.AreEqual(new uint[] { 20000, Stack, Stack }, counts);

                Assert.IsFalse(new InventoryManager(context).MailItems("Nobody", Template, 1, null).Done);
                Assert.IsFalse(new InventoryManager(context).MailItems("Family 1", 999999, 1, null).Done, "no such template");
                Assert.IsFalse(new InventoryManager(context).MailItems("Family 1", Template, Stack * 28, null).Done, "27 slots left, needs 28");
                Assert.AreEqual(3, InboxSlots(context).Length, "nothing made by a refusal");
            }
            finally
            {
                Moderation.FindStoredAccount = find;
            }
        }

        [TestMethod]
        public void AMailedCharacterUniqueItemIsOneStackAndNotASecond()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var find = Moderation.FindStoredAccount;
            var template = ItemManager.Instance.GetItemTemplateById(Template);
            var wasUnique = template.HasCharacterUniqueFlag;

            try
            {
                Moderation.FindStoredAccount = name =>
                {
                    using var unit = context.CreateChar();
                    return unit.GameAccounts.FindByFamilyName(name);
                };
                template.HasCharacterUniqueFlag = true;

                var inventory = new InventoryManager(context);

                Assert.IsFalse(inventory.MailItems("Family 1", Template, Stack + 1, null).Done, "two stacks of a unique template");
                Assert.AreEqual(0, InboxSlots(context).Length, "nothing made");

                Assert.IsTrue(inventory.MailItems("Family 1", Template, Stack, null).Done);
                Assert.AreEqual(1, InboxSlots(context).Length);

                var again = inventory.MailItems("Family 1", Template, 1, null);
                Assert.IsFalse(again.Done, "the one in the inbox is held");
                StringAssert.Contains(again.Text, "Character Unique");
                Assert.AreEqual(1, InboxSlots(context).Length);
            }
            finally
            {
                template.HasCharacterUniqueFlag = wasUnique;
                Moderation.FindStoredAccount = find;
            }
        }

        [TestMethod]
        public void AGmCanMailItemsToAPlayerInTheWorld()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            context.Client.AccountEntry.FamilyName = "Family 1";

            lock (Server.Clients)
                Server.Clients.Add(context.Client);

            try
            {
                var result = new InventoryManager(context).MailItems("family 1", Template, 0, "Warden");

                Assert.IsTrue(result.Done, result.Text);
                Assert.AreEqual(1, context.Client.Player.Inventory.InboxItems.Count);
                Assert.AreEqual(Stack, EntityManager.Instance.GetItem(context.Client.Player.Inventory.InboxItems[0]).StackSize, "no quantity: a full stack");

                var packets = context.Drain();
                Assert.AreEqual(1, packets.OfType<AddInboxItemPacket>().Count());
                Assert.IsTrue(packets.OfType<SystemMessagePacket>().Any(p => p.TextMessage.StartsWith("A game master sent you an item")));
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(context.Client);
            }
        }
    }
}
