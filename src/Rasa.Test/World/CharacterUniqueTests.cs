using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Gameplay;
    using Rasa.Test.Missions;

    // Character Unique (InventoryManager.Unique): one item of such a template a character, asked
    // on every way an item comes to them. WagerTests has the clan feud forfeit.
    [TestClass]
    [DoNotParallelize]
    public class CharacterUniqueTests
    {
        private const uint Unique = 9311, Plain = 9312, ItemClass = 3147;
        private const uint Account = 1, Second = 2;

        [TestMethod]
        public void WhatIsHeldCountsAListingAndLeavesOutWhatIsGoing()
        {
            using var f = new Fixture();
            var player = f.First.Player;
            var unique = f.Template(Unique);
            var plain = f.Template(Plain);

            Assert.IsTrue(f.Inventory.MayReceive(player, unique));
            Assert.IsFalse(f.Inventory.MayReceive(player, new[] { unique, unique }), "two of one at once");
            Assert.IsTrue(f.Inventory.MayReceive(player, new[] { plain, plain }));

            var held = f.InPack(f.First, Unique, slot: 3);

            Assert.IsFalse(f.Inventory.MayReceive(player, unique));
            Assert.IsTrue(f.Inventory.MayReceive(player, unique, new[] { held.EntityId }), "the one going the other way");
            Assert.IsTrue(f.Inventory.MayReceive(player, plain));

            var kept = f.Inventory.Receivable(player, new[] { plain, unique, plain }, template => template, out var refused);
            CollectionAssert.AreEqual(new[] { plain, plain }, kept);
            Assert.IsTrue(refused);

            // Listed at an auction house it is still theirs: it comes back if it does not sell.
            player.Inventory.PersonalInventory[3] = 0;
            Assert.IsTrue(f.Inventory.MayReceive(player, unique));
            player.Inventory.AuctionItems.Add(held.EntityId);
            Assert.IsFalse(f.Inventory.MayReceive(player, unique), "listed");
        }

        [TestMethod]
        public void TheDatabaseAnswersForACharacterWhoIsNotLoggedIn()
        {
            using var f = new Fixture();
            f.Context.SeedCharacter(Account, 1, Second);

            bool Holds(uint characterId)
            {
                using var unit = f.Context.CreateChar();
                return InventoryManager.HoldsTemplateStored(unit, Account, characterId, Unique);
            }

            Assert.IsFalse(Holds(Second));

            // The first character's pack is theirs alone.
            f.InPack(f.First, Unique, slot: 3);
            Assert.IsTrue(Holds(f.First.Player.Id));
            Assert.IsFalse(Holds(Second));

            // The footlocker is the account's: unbound, it is every character's; bound, only its one's.
            var shared = f.InFootlocker(Unique, slot: 5);
            Assert.IsTrue(Holds(Second));

            shared.BoundCharacterId = f.First.Player.Id;
            using (var unit = f.Context.CreateChar())
                unit.Items.UpdateBoundCharacter(shared);

            Assert.IsFalse(Holds(Second));
        }

        [TestMethod]
        public void OneInTheFootlockerDoesNotComeOutToACharacterWhoHasOne()
        {
            using var f = new Fixture();
            var shared = f.InFootlocker(Unique, slot: 7);
            f.InPack(f.First, Unique, slot: 3);
            WorldTestContext.Drain(f.First);

            f.TakeOut(f.First, footlockerSlot: 7, packSlot: 4);

            Assert.AreEqual(shared.EntityId, f.First.Player.Inventory.HomeInventory[7], "left");
            Assert.AreEqual(0UL, f.First.Player.Inventory.PersonalInventory[4]);
            Assert.AreEqual(PlayerMessage.PmItemCharacterUnique, Sent(f.First).OfType<DisplayClientMessagePacket>().Single().MsgId);

            // Swapped for the one held, it does: one goes in as the other comes out.
            f.TakeOut(f.First, footlockerSlot: 7, packSlot: 3);
            Assert.AreEqual(shared.EntityId, f.First.Player.Inventory.PersonalInventory[3]);
            Assert.AreNotEqual(0UL, f.First.Player.Inventory.HomeInventory[7]);
        }

        [TestMethod]
        public void AFixedRewardOfOneHeldIsLeftOutAndTheRestIsGiven()
        {
            using var f = new Fixture();
            var context = f.Context;
            var fixedTemplate = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[28];
            fixedTemplate.HasCharacterUniqueFlag = true;

            var held = f.InPack(context.Client, 28, slot: 50);
            var before = context.ReadRewardTotals();
            context.Drain();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, 0, null));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + context.Reward.Experience, after.Experience, "the rest of it");
            Assert.AreEqual(before.ItemCount + 2, after.ItemCount, "the chosen two, not the three held already");
            Assert.AreEqual(1, context.Client.Player.Inventory.PersonalInventory
                .Count(id => EntityManager.Instance.GetItem(id)?.ItemTemplate?.ItemTemplateId == 28));
            Assert.AreEqual(1u, held.StackSize, "not merged into");
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[429].State);
            Assert.IsTrue(context.Drain().OfType<DisplayClientMessagePacket>().Any(p => p.MsgId == PlayerMessage.PmItemCharacterUnique));
        }

        [TestMethod]
        public void AChosenRewardOfOneHeldRefusesTheTurnInSoAnotherCanBeChosen()
        {
            using var f = new Fixture();
            var context = f.Context;
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[29].HasCharacterUniqueFlag = true;
            f.InPack(context.Client, 29, slot: 50);
            var before = context.ReadRewardTotals();
            context.Drain();

            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, 0, null));

            Assert.AreEqual(before.Experience, context.ReadRewardTotals().Experience);
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[429].State);
            Assert.IsTrue(context.Drain().OfType<DisplayClientMessagePacket>().Any(p => p.MsgId == PlayerMessage.PmItemCharacterUnique));
        }

        [TestMethod]
        public void ASquadMemberWhoHoldsOneDoesNotRollForIt()
        {
            using var f = new Fixture();
            var first = f.First;
            var second = f.Context.CreateAdditionalClient(Second, accountId: 7);
            var party = new Party(1, first.AccountEntry.Id, new List<PartyMember>()) { LootMethod = PartyLootMethod.DiceRoll };

            LootDispenser Corpse() => new LootDispenser
            {
                LootItems =
                {
                    new LootItem { Item = f.Loose(Unique), EntityId = 1 },
                    new LootItem { Item = f.Loose(Unique), EntityId = 2 }
                }
            };

            // The first holds one: the second rolls alone for the first, and has won one for the other.
            f.InPack(first, Unique, slot: 3);
            var loot = Corpse();
            var dice = new Queue<int>(new[] { 100, 1, 100, 1 });
            LootRolls.Distribute(loot, party, new List<Client> { first, second }, dice.Dequeue);

            Assert.AreEqual(second.Player.EntityId, loot.LootItems[0].ReservedFor);
            Assert.AreEqual(0UL, loot.LootItems[1].ReservedFor, "nobody left to take it: not rolled");

            // Neither holds one: one each, whoever the dice favour.
            first.Player.Inventory.PersonalInventory[3] = 0;
            loot = Corpse();
            LootRolls.Distribute(loot, party, new List<Client> { first, second }, () => 50);

            CollectionAssert.AreEquivalent(new[] { first.Player.EntityId, second.Player.EntityId },
                loot.LootItems.Select(item => item.ReservedFor).ToArray());
        }

        [TestMethod]
        public void OneHeldStaysOnTheCorpseTakenAloneOrWithLootAllAndWalkingPastSaysNothing()
        {
            using var context = new LootConsolidationTests.LootFixture();
            context.Item.ItemTemplate.HasCharacterUniqueFlag = true;
            context.Storage.AddAmmo(1, 60);
            context.Drain();

            int Told() => context.Drain().OfType<DisplayClientMessagePacket>().Count(p => p.MsgId == PlayerMessage.PmItemCharacterUnique);

            context.Manager.RequestLootItemFromCorpse(context.Client,
                new RequestLootItemFromCorpsePacket { EntityId = context.Loot.EntityId, ItemId = context.Item.EntityId });
            Assert.IsFalse(context.Loot.LootItems.Single().Taken);
            Assert.AreEqual(1, Told());

            context.Manager.RequestLootAllFromCorpse(context.Client, new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });
            Assert.IsFalse(context.Loot.LootItems.Single().Taken);
            Assert.AreEqual(1, Told());

            context.Manager.RequestLootAllFromCorpse(context.Client, new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId, AutoLootOnly = true });
            Assert.IsFalse(context.Loot.LootItems.Single().Taken);
            Assert.AreEqual(0, Told(), "sent every frame in reach");
        }

        [TestMethod]
        public void LootAllTakesOneOfTwoAndLeavesTheOther()
        {
            using var context = new LootConsolidationTests.LootFixture();
            context.Item.ItemTemplate.HasCharacterUniqueFlag = true;
            var second = context.Storage.AddUnownedLoot(1);
            second.ItemTemplate.HasCharacterUniqueFlag = true;
            context.Loot.LootItems.Add(new LootItem(second, context.Client.Player.EntityId, 0));
            context.Drain();

            context.Manager.RequestLootAllFromCorpse(context.Client, new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(1, context.Loot.LootItems.Count(item => item.Taken));
            Assert.IsTrue(context.Drain().OfType<DisplayClientMessagePacket>().Any(p => p.MsgId == PlayerMessage.PmItemCharacterUnique));
        }

        [TestMethod]
        public void AnAuctionIsNotBoughtOutBySomeoneWhoHoldsOne()
        {
            using var context = new WeaponAmmoContext(characterId: 42);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single(entry => entry.Id == 42).Credit = 100;
                database.SaveChanges();
            }

            var item = context.AddUnownedLoot(1);
            item.ItemTemplate.HasCharacterUniqueFlag = true;
            context.AddAuction(item, sellerId: 99, price: 50);
            context.AddAmmo(1, 60);
            WorldTestContext.Drain(context.Client);

            var manager = (AuctionHouseManager)typeof(AuctionHouseManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null)
                .Invoke(new object[] { context });

            manager.RequestAuctionBuyout(context.Client, new RequestAuctionBuyoutPacket { ItemId = checked((uint)item.EntityId), Price = 50 });

            Assert.AreEqual(PlayerMessage.PmItemCharacterUnique, Sent(context.Client).OfType<AuctionBuyoutFailedPacket>().Single().PlayerMessageId);
            Assert.AreEqual(0, context.Client.Player.Inventory.InboxItems.Count);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
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
                First.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, LockboxTab.TotalSlots).ToList();
                First.Player.LockboxTabs = 1;

                foreach (var (id, unique) in new[] { (Unique, true), (Plain, false) })
                {
                    Context.AddRewardTemplate(id, ItemClass);
                    var template = Template(id);
                    template.InventoryCategory = InventoryCategory.Equipment;
                    template.HasCharacterUniqueFlag = unique;
                }
            }

            internal ItemTemplate Template(uint id) => EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[id];

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

            /// <summary>An item of the template, registered, with a row but in no inventory: on a corpse, say.</summary>
            internal Item Loose(uint templateId) => Make(templateId);

            internal void TakeOut(Client client, int footlockerSlot, int packSlot) =>
                Inventory.RequestTakeItemFromHomeInventory(client, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = (uint)footlockerSlot, DestSlot = (uint)packSlot, Quantity = 1 });

            private Item Make(uint templateId)
            {
                var item = ItemManager.StageItem(Template(templateId), 1, "");

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose()
            {
                foreach (var id in new uint[] { Unique, Plain, 28, 29 })
                    if (EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)ItemClass, out var classInfo) &&
                        classInfo.ItemTemplates.TryGetValue(id, out var template))
                        template.HasCharacterUniqueFlag = false;

                Context.Dispose();
            }
        }
    }
}
