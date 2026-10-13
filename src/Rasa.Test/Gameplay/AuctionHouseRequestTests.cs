using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // What the auction house window sends, as the client builds it.
    //
    // The auctioneer is the id the server opened the window with, a Python long. An item is named
    // two ways: by the id the client was sent, a long (Receive All walks its own inbox list;
    // a drag carries the entity's id), or by int(widget.GetID()), the id read back out of a row
    // widget's name - a Python int whenever it fits one, which every item's does (Create Auction,
    // Cancel Auction, Receive, a right-click on an inbox row). Receive, Receive All and a
    // right-click send None for the pack slot: the client leaves the slot to the server for an
    // item from the inbox (client/inventory.py AddItemToPersonalInventory).
    [TestClass]
    [DoNotParallelize]
    public class AuctionHouseRequestTests
    {
        private const ulong Auctioneer = 133079561962676UL;
        private const uint ItemClass = 990301;
        private const uint FirstTemplate = 990310;

        private uint _nextTemplate = FirstTemplate;

        [TestMethod]
        [DataRow(false, DisplayName = "item id from the row widget: an int")]
        [DataRow(true, DisplayName = "item id as a long")]
        public void CreateAuctionIsRead(bool itemAsLong)
        {
            var message = Message(GameOpcode.RequestCreateAuction, pw =>
            {
                pw.WriteTuple(4);
                pw.WriteULong(Auctioneer);
                Id(pw, 123456, itemAsLong);
                pw.WriteUInt(2500);
                pw.WriteUInt(3);
            });

            Assert.IsTrue(message.ReadPacket(), "a throw or a false here closes the connection");

            var packet = (RequestCreateAuctionPacket)message.Packet;

            Assert.AreEqual(Auctioneer, packet.EntityId);
            Assert.AreEqual(123456UL, packet.ItemEntityId);
            Assert.AreEqual(2500u, packet.Price);
            Assert.AreEqual(3u, packet.Duration);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "item id from the row widget: an int")]
        [DataRow(true, DisplayName = "item id as a long")]
        public void CancelAuctionIsRead(bool itemAsLong)
        {
            var message = Message(GameOpcode.RequestCancelAuction, pw =>
            {
                pw.WriteTuple(2);
                pw.WriteULong(Auctioneer);
                Id(pw, 123456, itemAsLong);
            });

            Assert.IsTrue(message.ReadPacket());

            var packet = (RequestCancelAuctionPacket)message.Packet;

            Assert.AreEqual(Auctioneer, packet.EntityId);
            Assert.AreEqual(123456UL, packet.ItemEntityId);
        }

        [TestMethod]
        [DataRow(false, -1, DisplayName = "Receive and a right-click: int id, None")]
        [DataRow(true, -1, DisplayName = "Receive All, a drag onto another tab: long id, None")]
        [DataRow(true, 3, DisplayName = "a drag onto a slot: long id, slot")]
        [DataRow(false, 3, DisplayName = "int id, slot")]
        public void TakeFromInboxIsRead(bool itemAsLong, int slot)
        {
            var message = Message(GameOpcode.RequestTakeItemFromInboxInventory, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteULong(Auctioneer);
                Id(pw, 123456, itemAsLong);

                if (slot < 0)
                    pw.WriteNoneStruct();
                else
                    pw.WriteUInt((uint)slot);
            });

            Assert.IsTrue(message.ReadPacket());

            var packet = (RequestTakeItemFromInboxInventoryPacket)message.Packet;

            Assert.AreEqual(Auctioneer, packet.EntityId);
            Assert.AreEqual(123456UL, packet.ItemEntityId);
            Assert.AreEqual(slot < 0 ? (uint?)null : (uint)slot, packet.DestSlot);
        }

        [TestMethod]
        public void AnItemIsListedAndTakenDownAgainWithWhatTheWindowSends()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var seller = context.Client;
            var item = InPack(context, slot: 7);
            var auctions = Auctions(context);

            // Listing takes the item out of the pack through the inventory manager's instance.
            var singleton = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var before = singleton.GetValue(null);

            singleton.SetValue(null, new InventoryManager(context));

            try
            {
                Credits(context, 1000);
                context.Drain();

                // Create Auction: the row's id as an int, a price, the third duration.
                var create = Read<RequestCreateAuctionPacket>(GameOpcode.RequestCreateAuction, pw =>
                {
                    pw.WriteTuple(4);
                    pw.WriteULong(Auctioneer);
                    pw.WriteUInt(checked((uint)item.EntityId));
                    pw.WriteUInt(400);
                    pw.WriteUInt(2);
                });

                auctions.RequestCreateAuction(seller, create);

                var listed = context.Drain().ToList();

                Assert.AreEqual(item.EntityId, listed.OfType<AuctionCreationSuccessPacket>().Single().ItemId);
                CollectionAssert.AreEqual(new[] { item.EntityId }, seller.Player.Inventory.AuctionItems.ToArray());
                Assert.AreEqual(0UL, seller.Player.Inventory.PersonalInventory[7]);

                var deposit = AuctionHouseManager.CalculateDeposit(item, 400, 2);

                Assert.AreEqual(1000 - (int)deposit, seller.Player.Credits[CurencyType.Credits]);

                using (var unit = context.CreateChar())
                {
                    Assert.AreEqual(400u, unit.Auctions.GetAuctionByItemId(item.Id).Price);
                    Assert.AreEqual((uint)InventoryType.AuctionInventory, unit.CharacterInventories.FindByItemId(item.Id).InventoryType);
                }

                // Cancel Auction: the listing's row, again an int.
                var cancel = Read<RequestCancelAuctionPacket>(GameOpcode.RequestCancelAuction, pw =>
                {
                    pw.WriteTuple(2);
                    pw.WriteULong(Auctioneer);
                    pw.WriteUInt(checked((uint)item.EntityId));
                });

                auctions.RequestCancelAuction(seller, cancel);

                var cancelled = context.Drain().ToList();

                Assert.AreEqual(item.EntityId, cancelled.OfType<CancelAuctionSuccessPacket>().Single().ItemId);
                Assert.AreEqual(0, seller.Player.Inventory.AuctionItems.Count);
                Assert.AreEqual(1, seller.Player.Inventory.PersonalInventory.Count(id => id == item.EntityId));

                using (var unit = context.CreateChar())
                {
                    Assert.IsNull(unit.Auctions.GetAuctionByItemId(item.Id));
                    Assert.AreEqual((uint)InventoryType.Personal, unit.CharacterInventories.FindByItemId(item.Id).InventoryType);
                }
            }
            finally
            {
                singleton.SetValue(null, before);
            }
        }

        [TestMethod]
        public void AListingAndItsCancellationAreOnTheEconomyLog()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var seller = context.Client;
            var item = InPack(context, slot: 7);
            var auctions = Auctions(context);
            var singleton = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var before = singleton.GetValue(null);
            var store = new EconomyAuditTests.MemoryStore();
            var previousStore = EconomyAudit.Instance.Store;

            singleton.SetValue(null, new InventoryManager(context));
            EconomyAudit.Instance.Load(store);

            try
            {
                Credits(context, 1000);
                context.Drain();

                auctions.RequestCreateAuction(seller, new RequestCreateAuctionPacket
                {
                    EntityId = Auctioneer, ItemEntityId = item.EntityId, Price = 400, Duration = 2
                });
                auctions.RequestCancelAuction(seller, new RequestCancelAuctionPacket
                {
                    EntityId = Auctioneer, ItemEntityId = item.EntityId
                });

                var deposit = AuctionHouseManager.CalculateDeposit(item, 400, 2);
                var id = seller.Player.Id;

                Assert.HasCount(2, store.Events);
                Assert.IsTrue(store.Events[0].All(row => row.Kind == (byte)Rasa.Structures.Char.EconomyLogKind.AuctionListed && row.ReferenceId == item.Id));
                CollectionAssert.AreEquivalent(new[]
                {
                    $"{id} item {item.Id} -{item.StackSize} from 0",
                    $"{id} credits -{deposit} ={1000 - deposit} from 0"
                }, store.Events[0].Select(EconomyAuditTests.Describe).ToList(), "to the auction house, and the deposit");
                Assert.AreEqual((byte)Rasa.Structures.Char.EconomyLogKind.AuctionCancelled, store.Events[1].Single().Kind);
                Assert.AreEqual($"{id} item {item.Id} {item.StackSize} from 0", EconomyAuditTests.Describe(store.Events[1].Single()), "back, the deposit kept");
            }
            finally
            {
                singleton.SetValue(null, before);
                EconomyAudit.Instance.Load(previousStore);
            }
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Receive and a right-click: int id")]
        [DataRow(true, DisplayName = "Receive All: long id")]
        public void AnInboxItemTakenWithNoSlotGoesToTheFirstFreeSlotOfItsCategory(bool itemAsLong)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var block = Block();

            // The first two slots of its tab are taken, and so is all of the tab before it.
            InPack(context, (uint)block);
            InPack(context, (uint)block + 1);
            var waiting = InInbox(context, 0);

            context.Drain();

            inventory.RequestTakeItemFromInboxInventory(client, Take(waiting, itemAsLong, null));

            Assert.AreEqual(waiting.EntityId, client.Player.Inventory.PersonalInventory[block + 2]);
            Assert.AreEqual(0, client.Player.Inventory.InboxItems.Count);
            Assert.AreEqual((uint)(block + 2), waiting.OwnerSlotId);

            var told = context.Drain().ToList();

            Assert.AreEqual(waiting.EntityId, told.OfType<RemoveInboxItemPacket>().Single().EntityId);

            var added = told.OfType<InventoryAddItemPacket>().Single();

            Assert.AreEqual(waiting.EntityId, added.EntityId);
            Assert.AreEqual((uint)(block + 2), added.SlotId);

            using var unit = context.CreateChar();
            var row = unit.CharacterInventories.FindByItemId(waiting.Id);

            Assert.AreEqual((uint)InventoryType.Personal, row.InventoryType);
            Assert.AreEqual((uint)(block + 2), row.SlotId);
            Assert.AreEqual(client.Player.Id, row.CharacterId);
        }

        [TestMethod]
        public void EveryItemOfReceiveAllIsTaken()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var block = Block();
            var waiting = new[] { InInbox(context, 0), InInbox(context, 1), InInbox(context, 2) };

            // One request an item, as the button sends them.
            foreach (var item in waiting)
                inventory.RequestTakeItemFromInboxInventory(client, Take(item, true, null));

            CollectionAssert.AreEqual(waiting.Select(item => item.EntityId).ToArray(),
                client.Player.Inventory.PersonalInventory.Skip(block).Take(3).ToArray());
            Assert.AreEqual(0, client.Player.Inventory.InboxItems.Count);
        }

        [TestMethod]
        public void WithItsTabFullAnInboxItemStaysWhereItIsAndThePlayerIsTold()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var block = Block();

            for (var slot = 0; slot < 50; slot++)
                client.Player.Inventory.PersonalInventory[block + slot] = ulong.MaxValue - (ulong)slot;

            var waiting = InInbox(context, 0);

            context.Drain();

            try
            {
                inventory.RequestTakeItemFromInboxInventory(client, Take(waiting, false, null));

                CollectionAssert.AreEqual(new[] { waiting.EntityId }, client.Player.Inventory.InboxItems.ToArray());
                Assert.IsFalse(client.Player.Inventory.PersonalInventory.Contains(waiting.EntityId));

                var told = context.Drain().ToList();

                Assert.AreEqual(0, told.OfType<RemoveInboxItemPacket>().Count());
                Assert.AreEqual(PlayerMessage.PmInventoryFull, told.OfType<DisplayClientMessagePacket>().Single().MsgId);

                using var unit = context.CreateChar();

                Assert.AreEqual((uint)InventoryType.InboxInventory, unit.CharacterInventories.FindByItemId(waiting.Id).InventoryType);
            }
            finally
            {
                for (var slot = 0; slot < 50; slot++)
                    client.Player.Inventory.PersonalInventory[block + slot] = 0;
            }
        }

        [TestMethod]
        public void AnInboxItemDraggedOntoAFreeSlotOfItsTabGoesThere()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var block = Block();
            var waiting = InInbox(context, 0);

            inventory.RequestTakeItemFromInboxInventory(client, Take(waiting, true, (uint)block + 9));

            Assert.AreEqual(waiting.EntityId, client.Player.Inventory.PersonalInventory[block + 9]);
            Assert.AreEqual(0, client.Player.Inventory.InboxItems.Count);
        }

        [TestMethod]
        [DataRow(0, DisplayName = "a slot with something in it")]
        [DataRow(1, DisplayName = "a slot of another tab")]
        [DataRow(2, DisplayName = "a slot past the pack")]
        public void ASlotThatCannotTakeItIsNoSlot(int which)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var block = Block();
            var kept = InPack(context, (uint)block);
            var waiting = InInbox(context, 0);
            var asked = which switch
            {
                0 => (uint)block,
                1 => (uint)(block == 0 ? 60 : 0),
                _ => 250u
            };

            inventory.RequestTakeItemFromInboxInventory(client, Take(waiting, true, asked));

            Assert.AreEqual(kept.EntityId, client.Player.Inventory.PersonalInventory[block], "nothing is swapped out");
            Assert.AreEqual(waiting.EntityId, client.Player.Inventory.PersonalInventory[block + 1]);
            Assert.AreEqual(0, client.Player.Inventory.InboxItems.Count);

            if (asked < 250)
                Assert.AreEqual(which == 0 ? kept.EntityId : 0UL, client.Player.Inventory.PersonalInventory[(int)asked]);
        }

        [TestMethod]
        public void AnItemThatIsNotInTheInboxIsNotTaken()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var packed = InPack(context, (uint)Block() + 4);

            context.Drain();

            // Their own item, already in the pack: not one the inbox holds.
            inventory.RequestTakeItemFromInboxInventory(client, Take(packed, false, null));

            Assert.AreEqual(1, client.Player.Inventory.PersonalInventory.Count(id => id == packed.EntityId));
            Assert.AreEqual(packed.EntityId, client.Player.Inventory.PersonalInventory[Block() + 4]);
            Assert.AreEqual(0, context.Drain().OfType<RemoveInboxItemPacket>().Count());
        }

        /// <summary>Where the fixture's items go in the pack: the start of their category's fifty slots.</summary>
        private static int Block() => ((int)InventoryCategory.Equipment - 1) * 50;

        private static void Id(PythonWriter pw, uint id, bool asLong)
        {
            if (asLong)
                pw.WriteULong(id);
            else
                pw.WriteUInt(id);
        }

        private static RequestTakeItemFromInboxInventoryPacket Take(Item item, bool itemAsLong, uint? slot) =>
            Read<RequestTakeItemFromInboxInventoryPacket>(GameOpcode.RequestTakeItemFromInboxInventory, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteULong(Auctioneer);
                Id(pw, checked((uint)item.EntityId), itemAsLong);

                if (slot.HasValue)
                    pw.WriteUInt(slot.Value);
                else
                    pw.WriteNoneStruct();
            });

        private ItemTemplate Template(MissionTestContext context)
        {
            var templateId = _nextTemplate++;

            context.AddRewardTemplate(templateId, ItemClass);

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass];

            classInfo.ItemClassInfo.StackSize = 1;
            classInfo.ItemClassInfo.MaxHitPoints = 100;

            var template = classInfo.ItemTemplates[templateId];

            template.InventoryCategory = InventoryCategory.Equipment;
            template.HasSellableFlag = true;
            template.SellPrice = 100;

            return template;
        }

        private Item Staged(MissionTestContext context, InventoryType type, uint slot)
        {
            var item = ItemManager.StageItem(Template(context), 1, "");

            item.OwnerId = context.Client.Player.Id;
            item.OwnerSlotId = slot;

            using (var unit = context.CreateChar())
            {
                item.Id = unit.Items.CreateItem(item);
                unit.CharacterInventories.AddInvItem(context.Client.AccountEntry.Id, context.Client.Player.Id, (uint)type, slot, item.Id);
            }

            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            return item;
        }

        /// <summary>An item in the pack: row, entity and list.</summary>
        private Item InPack(MissionTestContext context, uint slot)
        {
            var item = Staged(context, InventoryType.Personal, slot);

            context.Client.Player.Inventory.PersonalInventory[(int)slot] = item.EntityId;

            return item;
        }

        /// <summary>An item waiting in the inbox: row, entity and list.</summary>
        private Item InInbox(MissionTestContext context, uint slot)
        {
            var item = Staged(context, InventoryType.InboxInventory, slot);

            context.Client.Player.Inventory.InboxItems.Add(item.EntityId);

            return item;
        }

        private static void Credits(MissionTestContext context, int credits)
        {
            context.Client.Player.Credits[CurencyType.Credits] = credits;

            using var unit = context.CreateChar();

            unit.Characters.UpdateCharacterCredits(context.Client.Player.Id, credits);
        }

        private static AuctionHouseManager Auctions(MissionTestContext context) =>
            (AuctionHouseManager)typeof(AuctionHouseManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { context });

        private static T Read<T>(GameOpcode method, Action<PythonWriter> arguments) where T : PythonPacket
        {
            var message = Message(method, arguments);

            Assert.IsTrue(message.ReadPacket(), $"{method} as the client sends it");

            return (T)message.Packet;
        }

        /// <summary>A method call as it arrives from the client: the arguments between the payload's markers.</summary>
        private static CallServerMethodMessage Message(GameOpcode method, Action<PythonWriter> arguments)
        {
            byte[] payload;

            using (var buffer = new MemoryStream())
            {
                using (var binary = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
                {
                    binary.Write((byte)0x4F);

                    using (var writer = new PythonWriter(binary))
                        arguments(writer);

                    binary.Write((byte)0x66);
                }

                payload = buffer.ToArray();
            }

            using var stream = new MemoryStream();

            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteUInt((uint)method);
                writer.WriteArray(payload);
            }

            stream.Position = 0;

            using var input = new BinaryReader(stream);
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);
            var message = new CallServerMethodMessage { Subtype = CallServerMethodSubtype.UserMethodById };

            message.Read(reader);

            return message;
        }
    }
}
