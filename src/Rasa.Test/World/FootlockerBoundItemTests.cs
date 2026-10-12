using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // The footlocker is the account's: every character of the account sees what is in it
    // (InventoryManager.RequestMoveItemToHomeInventory, RequestTakeItemFromHomeInventory). An item
    // bound to one of them comes out only to that one ("That item is bound on a different
    // character."). An item can be bound two ways (Item.IsBound): to a character, when it was
    // equipped or bound on request, or by its template - mission items, GM and event gear,
    // account rewards - which says Bound on Character from the start but names no character.
    [TestClass]
    [DoNotParallelize]
    public class FootlockerBoundItemTests
    {
        private const uint Bound = 9301, Free = 9302, ItemClass = 3147;
        private const uint Account = 1, First = 1, Second = 2;

        [TestMethod]
        public void AnItemBoundByItsTemplateComesOutOnlyToTheCharacterThatPutItIn()
        {
            using var f = new Fixture();
            var item = f.InPack(f.First, Bound, slot: 3);

            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);
            Assert.AreEqual(item.EntityId, f.First.Player.Inventory.HomeInventory[7]);

            // The account's other character, logged in to it.
            var second = f.LogIn(Second);
            var held = second.Player.Inventory.HomeInventory[7];
            Assert.AreNotEqual(0UL, held, "seen in the footlocker");
            WorldTestContext.Drain(second);

            f.TakeOut(second, footlockerSlot: 7, packSlot: 3);

            Assert.AreEqual(held, second.Player.Inventory.HomeInventory[7], "not taken");
            Assert.AreEqual(0UL, second.Player.Inventory.PersonalInventory[3]);
            Assert.AreEqual(PlayerMessage.PmItemBoundOnDiffCharacter, Sent(second).OfType<DisplayClientMessagePacket>().Single().MsgId);
            Assert.AreEqual((uint)InventoryType.HomeInventory, f.Row(item).InventoryType);
        }

        [TestMethod]
        public void TheCharacterThatPutItInTakesItOutAgain()
        {
            using var f = new Fixture();
            var item = f.InPack(f.First, Bound, slot: 3);

            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);
            f.TakeOut(f.First, footlockerSlot: 7, packSlot: 4);

            Assert.AreEqual(item.EntityId, f.First.Player.Inventory.PersonalInventory[4]);
            Assert.AreEqual(First, f.Row(item).CharacterId);
            Assert.AreEqual((uint)InventoryType.Personal, f.Row(item).InventoryType);
        }

        [TestMethod]
        public void PuttingItInBindsItToTheCharacterForGood()
        {
            using var f = new Fixture();
            var item = f.InPack(f.First, Bound, slot: 3);
            WorldTestContext.Drain(f.First);

            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);

            Assert.AreEqual(First, item.BoundCharacterId);
            Assert.AreEqual(First, f.BoundTo(item), "written");

            // Its tooltip is the client's to redraw.
            Assert.IsTrue(Sent(f.First).OfType<ItemInfoPacket>().Any());
        }

        [TestMethod]
        public void ABindingThatCannotBeWrittenMovesNothing()
        {
            using var f = new Fixture();
            var item = f.InPack(f.First, Bound, slot: 3);

            f.Context.BeforeSave = _ => throw new DbUpdateException("Injected binding failure.");
            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);
            f.Context.BeforeSave = null;

            Assert.AreEqual(item.EntityId, f.First.Player.Inventory.PersonalInventory[3], "still in the pack");
            Assert.AreEqual(0UL, f.First.Player.Inventory.HomeInventory[7]);
            Assert.AreEqual(0u, item.BoundCharacterId, "and not bound in memory either");
            Assert.AreEqual((uint)InventoryType.Personal, f.Row(item).InventoryType);
        }

        [TestMethod]
        public void AnItemSwappedIntoTheFootlockerIsBoundToo()
        {
            using var f = new Fixture();
            var stored = f.InPack(f.First, Free, slot: 5);
            var bound = f.InPack(f.First, Bound, slot: 3);

            f.Deposit(f.First, packSlot: 5, footlockerSlot: 7);

            // Taken out onto the pack slot of the bound one: the two trade places.
            f.TakeOut(f.First, footlockerSlot: 7, packSlot: 3);

            Assert.AreEqual(stored.EntityId, f.First.Player.Inventory.PersonalInventory[3]);
            Assert.AreEqual(bound.EntityId, f.First.Player.Inventory.HomeInventory[7]);
            Assert.AreEqual(First, f.BoundTo(bound));

            var second = f.LogIn(Second);
            f.TakeOut(second, footlockerSlot: 7, packSlot: 3);
            Assert.AreEqual(0UL, second.Player.Inventory.PersonalInventory[3]);
        }

        [TestMethod]
        public void OneAlreadyThereUnboundIsBoundToWhoeverTakesItOut()
        {
            // Put in before anything bound it; whose it was is not known.
            using var f = new Fixture();
            var item = f.InFootlocker(Bound, slot: 7);
            var second = f.LogIn(Second);

            f.TakeOut(second, footlockerSlot: 7, packSlot: 3);

            Assert.AreNotEqual(0UL, second.Player.Inventory.PersonalInventory[3], "taken");
            Assert.AreEqual(Second, f.BoundTo(item));

            // And from then on it is theirs: put back, the first cannot have it.
            f.Deposit(second, packSlot: 3, footlockerSlot: 9);
            f.LogIn(f.First);
            f.TakeOut(f.First, footlockerSlot: 9, packSlot: 3);
            Assert.AreEqual(0UL, f.First.Player.Inventory.PersonalInventory[3]);
        }

        [TestMethod]
        public void OneAlreadyThereUnboundIsBoundToWhoeverSwapsItOut()
        {
            using var f = new Fixture();
            var item = f.InFootlocker(Bound, slot: 7);
            var second = f.LogIn(Second);
            var dropped = f.InPack(second, Free, slot: 3);

            // Something of theirs dropped on its slot: the two trade places.
            f.Deposit(second, packSlot: 3, footlockerSlot: 7);

            Assert.AreEqual(dropped.EntityId, second.Player.Inventory.HomeInventory[7]);
            Assert.AreNotEqual(0UL, second.Player.Inventory.PersonalInventory[3], "taken out by the swap");
            Assert.AreEqual(Second, f.BoundTo(item));
        }

        [TestMethod]
        public void AnItemThatIsNotBoundStaysFreeToAnyCharacter()
        {
            using var f = new Fixture();
            var item = f.InPack(f.First, Free, slot: 3);

            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);

            Assert.AreEqual(0u, f.BoundTo(item));

            var second = f.LogIn(Second);
            f.TakeOut(second, footlockerSlot: 7, packSlot: 3);

            Assert.AreNotEqual(0UL, second.Player.Inventory.PersonalInventory[3]);
            Assert.AreEqual(0u, f.BoundTo(item));
        }

        [TestMethod]
        public void AnItemBoundToACharacterAlreadyKeepsItsCharacter()
        {
            using var f = new Fixture();
            // Bound by its template, and bound to the account's other character already, however
            // it came to be in this one's pack.
            var item = f.InPack(f.First, Bound, slot: 3);

            item.BoundCharacterId = Second;
            using (var unit = f.Context.CreateChar())
                unit.Items.UpdateBoundCharacter(item);

            f.Deposit(f.First, packSlot: 3, footlockerSlot: 7);

            Assert.AreEqual(Second, f.BoundTo(item), "not taken over by the one who put it in");
        }

        private static List<Rasa.Packets.PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private sealed class Fixture : System.IDisposable
        {
            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal Client First => Context.Client;

            internal Fixture()
            {
                Inventory = new InventoryManager(Context, Context.Manager);
                Ready(First);

                foreach (var (id, bound) in new[] { (Bound, true), (Free, false) })
                {
                    Context.AddRewardTemplate(id, ItemClass);
                    var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[id];
                    template.InventoryCategory = InventoryCategory.Equipment;
                    template.BoundToCharacter = bound;
                }
            }

            private static void Ready(Client client)
            {
                client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, LockboxTab.TotalSlots).ToList();
                client.Player.LockboxTabs = 1;
            }

            internal Item InPack(Client client, uint templateId, int slot)
            {
                var item = Make(templateId);

                item.OwnerId = client.Player.Id;
                item.OwnerSlotId = (uint)slot;
                client.Player.Inventory.PersonalInventory[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            internal Item InFootlocker(uint templateId, int slot)
            {
                var item = Make(templateId);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                First.Player.Inventory.HomeInventory[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Account, 0, (uint)InventoryType.HomeInventory, (uint)slot, item.Id);

                return item;
            }

            /// <summary>
            /// The character, logged in: its pack and the account's footlocker loaded from the
            /// rows. A character not seen before is the account's second.
            /// </summary>
            internal Client LogIn(uint characterId) => LogIn(characterId == First.Player.Id ? First : Context.CreateAdditionalClient(characterId, Account, slot: 1));

            internal Client LogIn(Client client)
            {
                Ready(client);
                Inventory.InitCharacterInventory(client);
                WorldTestContext.Drain(client);
                return client;
            }

            internal void Deposit(Client client, int packSlot, int footlockerSlot) =>
                Inventory.RequestMoveItemToHomeInventory(client, new RequestMoveItemToHomeInventoryPacket { SrcSlot = (uint)packSlot, DestSlot = (uint)footlockerSlot, Quantity = 1 });

            internal void TakeOut(Client client, int footlockerSlot, int packSlot) =>
                Inventory.RequestTakeItemFromHomeInventory(client, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = (uint)footlockerSlot, DestSlot = (uint)packSlot, Quantity = 1 });

            internal CharacterInventoryEntry Row(Item item)
            {
                using var unit = Context.CreateChar();
                return unit.CharacterInventories.FindByItemId(item.Id);
            }

            /// <summary>The character the item's row is bound to: what the next login reads.</summary>
            internal uint BoundTo(Item item)
            {
                using var database = Context.Open();
                return database.ItemEntries.AsNoTracking().Single(entry => entry.ItemId == item.Id).BoundCharacterId;
            }

            private Item Make(uint templateId)
            {
                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];
                var item = ItemManager.StageItem(template, 1, "");

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose()
            {
                foreach (var id in new[] { Bound, Free })
                    EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[id].BoundToCharacter = false;

                Context.Dispose();
            }
        }
    }
}
