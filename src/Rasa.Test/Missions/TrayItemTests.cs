using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class TrayItemTests
    {
        private const uint PineOckTemplate = 111117;       // Companion Pine-Ock: 460 level 1
        private const uint SnowballTemplate = 131481;      // Snowball: 528 level 1

        private static readonly FieldInfo AbilityManagerInstance =
            typeof(AbilityManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        [TestMethod]
        public void AnItemsActionArrivesAsPythonIntsAndIsRead()
        {
            var packet = Read(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(2);
                writer.WriteInt(460);       // 0x1E: an item's action, from its template, is an int
                writer.WriteInt(1);
                writer.WriteULong(123456);
            });

            Assert.AreEqual(2, packet.SlotId);
            Assert.AreEqual(460L, packet.AbilityId);
            Assert.AreEqual(1L, packet.AbilityLevel);
            Assert.AreEqual(123456UL, packet.ItemId);
        }

        [TestMethod]
        public void AClearedSlotIsAllNone()
        {
            var packet = Read(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(5);
                writer.WriteNoneStruct();
                writer.WriteNoneStruct();
                writer.WriteNoneStruct();
            });

            Assert.AreEqual(5, packet.SlotId);
            Assert.AreEqual(0L, packet.AbilityId);
            Assert.AreEqual(0L, packet.AbilityLevel);
            Assert.AreEqual(0UL, packet.ItemId);
        }

        [TestMethod]
        public void APetDraggedOntoTheTrayKeepsItsItemAndGoesBackWithIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));
            harness.Drain();

            ManifestationManager.Instance.RequestSetAbilitySlot(harness.Client,
                new RequestSetAbilitySlotPacket { SlotId = 2, AbilityId = 460, AbilityLevel = 1, ItemId = pet.EntityId });

            var slot = harness.Client.Player.Abilities[2];
            Assert.AreEqual(460, slot.AbilityId);
            Assert.AreEqual(1U, slot.AbilityLevel);
            Assert.AreEqual(pet.Id, slot.ItemId);

            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(pet.Id, unit.CharacterAbilityDrawers.GetCharacterAbilities(harness.Client.Player.Id)
                    .Single(row => row.AbilitySlot == 2).ItemId);

            var drawer = harness.Drain().OfType<AbilityDrawerPacket>().Single();
            Assert.AreEqual(pet.EntityId, drawer.ItemEntities[2]);

            // And from the database, as a login loads it.
            var loaded = harness.Maps.GetPlayerAbilities(harness.Client.Player.Id)[2];
            Assert.AreEqual(pet.Id, loaded.ItemId);
            Assert.AreEqual(pet.EntityId, AbilityDrawerItems.EntityOf(harness.Client.Player, loaded));
        }

        [TestMethod]
        public void AnItemCannotBringAnActionItDoesNotPerform()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));

            ManifestationManager.Instance.RequestSetAbilitySlot(harness.Client,
                new RequestSetAbilitySlotPacket { SlotId = 2, AbilityId = 460, AbilityLevel = 10, ItemId = pet.EntityId });

            Assert.IsFalse(harness.Client.Player.Abilities.ContainsKey(2));
        }

        [TestMethod]
        public void AGoneItemIsStoodInForByAnotherThatPerformsTheSameAction()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            using var abilities = UseAbilityManager(ToyTests.CreateManager(harness, 460, 1, PineOckTemplate));

            var gone = new AbilityDrawerData(2, 460, 1, pet.Id + 100000);
            Assert.AreEqual(pet.EntityId, AbilityDrawerItems.EntityOf(harness.Client.Player, gone));

            var otherAction = new AbilityDrawerData(3, 482, 1, pet.Id + 100000);
            Assert.IsNull(AbilityDrawerItems.EntityOf(harness.Client.Player, otherAction));

            var skill = new AbilityDrawerData(4, 460, 1);
            Assert.IsNull(AbilityDrawerItems.EntityOf(harness.Client.Player, skill));
        }

        // The client's tray slot keeps the item id the drawer was last sent with, and the drawer
        // is only sent at login and when the tray is edited. A pet deleted and bought again is a
        // new item, so the slot went on naming one that was gone and was refused until a relog.
        [TestMethod]
        public void ASlotNamingAnItemThatIsGoneIsPerformedWithTheOneInThePack()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var first = ToyTests.Grant(harness, PineOckTemplate);
            ToyTests.LoadClass(harness, 7747);
            var manager = ToyTests.CreateManager(harness, 460, 1, PineOckTemplate);
            using var abilities = UseAbilityManager(manager);
            var player = harness.Client.Player;

            ManifestationManager.Instance.RequestSetAbilitySlot(harness.Client,
                new RequestSetAbilitySlotPacket { SlotId = 2, AbilityId = 460, AbilityLevel = 1, ItemId = first.EntityId });
            var named = harness.Drain().OfType<AbilityDrawerPacket>().Single().ItemEntities[2];
            Assert.AreEqual(first.EntityId, named);

            // Deleted, and bought again at a vendor: nothing tells the client's tray.
            InventoryManager.Instance.ReduceStackCount(harness.Client, InventoryType.Personal, first, 1);
            var bought = Buy(harness, PineOckTemplate);
            Assert.AreNotEqual(named, bought.EntityId);
            Assert.IsFalse(harness.Drain().OfType<AbilityDrawerPacket>().Any());

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(460, 1, named));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardPet);
            Assert.AreEqual(bought.EntityId, pending.ItemId);
            ToyTests.Land(harness, manager, pending);
            Assert.IsNotNull(AbilityManager.PetCreatureOf(player));
            Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any());

            // Home again, then the slot as a login with no pet in the pack sends it: no item.
            manager.RequestPerformAbility(harness.Client, ToyTests.Request(460, 1, named));
            Assert.IsNull(AbilityManager.PetCreatureOf(player));
            player.ActionReuseUntil.Remove(ActionId.AccountrewardPet);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(460, 1, 0));
            pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardPet);
            Assert.AreEqual(bought.EntityId, pending.ItemId);
            ToyTests.Land(harness, manager, pending);
            Assert.IsNotNull(AbilityManager.PetCreatureOf(player));
            Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any());
        }

        [TestMethod]
        public void WithNoItemInThePackThatPerformsItTheSlotIsStillRefused()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var pet = ToyTests.Grant(harness, PineOckTemplate);
            var manager = ToyTests.CreateManager(harness, 460, 1, PineOckTemplate);
            var named = pet.EntityId;
            InventoryManager.Instance.ReduceStackCount(harness.Client, InventoryType.Personal, pet, 1);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(460, 1, named));
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(460, 1, 0));
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardPet));
        }

        // What is used up is the item it was performed with, not the one named.
        [TestMethod]
        public void AConsumableStoodInForIsTheOneUsedUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var snowballs = ToyTests.Grant(harness, SnowballTemplate, 3);
            var manager = ToyTests.CreateManager(harness, 528, 1, SnowballTemplate);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(528, 1, snowballs.EntityId + 100000));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)528);
            Assert.AreEqual(snowballs.EntityId, pending.ItemId);
            ToyTests.Land(harness, manager, pending);

            Assert.AreEqual(2U, snowballs.StackSize);
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(2U, unit.Items.GetItem(snowballs.Id).StackSize);
        }

        /// <summary>One of the template, bought from a vendor's counter (NpcManager.RequestVendorPurchase).</summary>
        private static Item Buy(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(0) { ItemPrice = 1, VendorItems = { template } };
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var npcs = new NpcManager(harness.Context, harness.Manager);

            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
            {
                VendorEntityId = npc.EntityId,
                ItemEntityId = EntityManager.Instance.VendorItems[npc.EntityId].Single(),
                Quantity = 1
            });

            return harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == template);
        }

        private static RequestSetAbilitySlotPacket Read(System.Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
                write(writer);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestSetAbilitySlotPacket();
            packet.Read(reader);
            return packet;
        }

        [TestMethod]
        public void AnAbilityRefusedOnLandingForItsItemIsNotPaidFor()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var snowballs = ToyTests.Grant(harness, SnowballTemplate, 3);
            var manager = ToyTests.CreateManager(harness, 528, 1, SnowballTemplate);
            using var abilities = UseAbilityManager(manager);
            var player = harness.Client.Player;

            // Give the throw a power cost, as other consumables have.
            var actions = (System.Collections.Generic.Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            actions[(ActionId)528].Levels[1].Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 40 });

            var power = player.Attributes[Attributes.Power];
            power.Current = 100;
            power.CurrentMax = 100;

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(528, 1, snowballs.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)528);
            Assert.AreEqual(100, power.Current, "paid on landing, not on asking");

            // The landing's checks pass - the snowballs are still in the pack - and the write that
            // uses one up fails: the consumption is the last thing that can refuse the throw. (A
            // pet would not do: it keeps its item, so nothing is consumed that could refuse.)
            harness.Drain();
            var refused = false;
            harness.Context.BeforeCommand = sql =>
            {
                if (!refused && sql.Contains("UPDATE \"items\"", StringComparison.Ordinal))
                {
                    refused = true;
                    throw new Microsoft.Data.Sqlite.SqliteException("disk I/O error", 10);
                }
            };

            try
            {
                ToyTests.Land(harness, manager, pending);
            }
            finally
            {
                harness.Context.BeforeCommand = null;
            }

            Assert.IsTrue(refused, "the stack's write was reached");

            Assert.AreEqual(PlayerMessage.PmMissingReqItem, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(100, power.Current, "refused, so not paid for: the cost used to be taken before the consumption could refuse");
            Assert.AreEqual(3U, snowballs.StackSize, "nothing used up");
            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(3U, unit.Items.GetItem(snowballs.Id).StackSize);
        }

        private static Restore UseAbilityManager(AbilityManager manager)
        {
            var previous = AbilityManagerInstance.GetValue(null);
            AbilityManagerInstance.SetValue(null, manager);
            return new Restore(() => AbilityManagerInstance.SetValue(null, previous));
        }

        private sealed class Restore : System.IDisposable
        {
            private readonly System.Action _undo;
            public Restore(System.Action undo) => _undo = undo;
            public void Dispose() => _undo();
        }
    }
}
