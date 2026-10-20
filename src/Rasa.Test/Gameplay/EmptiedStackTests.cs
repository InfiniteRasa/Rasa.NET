using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Test.Missions;

    /// <summary>
    /// A stack used up or destroyed whole is gone from the database: its inventory row and its
    /// items row. The items row used to be left behind, referenced by nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EmptiedStackTests
    {
        private const uint SnowballTemplate = 131481;

        [TestMethod]
        public void AStackDestroyedWholeTakesItsItemRowWithIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var stack = ToyTests.Grant(harness, SnowballTemplate, 5);
            var id = stack.Id;

            Assert.IsTrue(Exists(harness, id));
            harness.Drain();

            // Part of it: the row stays, three left.
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = stack.EntityId, Quantity = 2 });

            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(3u, unit.Items.GetItem(id).StackSize);
                Assert.IsNotNull(unit.CharacterInventories.FindByItemId(id));
            }

            // The rest: both rows go, and the pack and the client are told.
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new PersonalInventory_DestroyItemPacket { EntityId = stack.EntityId, Quantity = 3 });

            Assert.IsFalse(Exists(harness, id), "no items row left behind");
            Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Contains(stack.EntityId));
            Assert.IsNull(EntityManager.Instance.GetItem(stack.EntityId));
            Assert.IsTrue(harness.Drain().OfType<InventoryRemoveItemPacket>().Any(packet => packet.EntityId == stack.EntityId));
        }

        [TestMethod]
        public void AStackUsedUpTakesItsItemRowWithIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var stack = ToyTests.Grant(harness, SnowballTemplate, 2);
            var id = stack.Id;
            var itemClass = stack.ItemTemplate.Class;

            // Spent by class, as a crafting job or an ability's item cost spends it.
            Assert.AreEqual(0u, InventoryManager.Instance.RemoveItemsByClass(harness.Client, itemClass, 2));

            Assert.IsFalse(Exists(harness, id));
            Assert.AreEqual(0u, InventoryManager.Instance.CountItemsByClass(harness.Client, itemClass));
        }

        [TestMethod]
        public void MoreThanTheStackHoldsIsTheWholeStack()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var stack = ToyTests.Grant(harness, SnowballTemplate, 2);
            var id = stack.Id;

            InventoryManager.Instance.ReduceStackCount(harness.Client, InventoryType.Personal, stack, 50);

            Assert.IsFalse(Exists(harness, id));
            Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Contains(stack.EntityId));
        }

        private static bool Exists(BootcampRuntimeTestHarness.Harness harness, uint itemId)
        {
            using var unit = harness.Context.CreateChar();

            return unit.Items.GetItem(itemId) != null || unit.CharacterInventories.FindByItemId(itemId) != null;
        }
    }
}
