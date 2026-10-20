using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;

    // The two pieces of equipment that perform an ability - the MotorAssist boots' Rage and the
    // PAU AFS Mech Sprint - were used up like a medpack when right-clicked. They are kept.
    [TestClass]
    [DoNotParallelize]
    public class EquipmentAbilityTests
    {
        private const uint MotorAssistBootsTemplate = 130392;   // Rage (307) pump 5; shoes
        private const uint MechSprintTemplate = 130498;         // Sprint (401) level 6; mech ability 1
        private const uint LogosFistTemplate = 111120;          // "Logos Fist" Emote: 464 level 1

        [TestMethod]
        [DataRow(MotorAssistBootsTemplate, 307U, 5U, ActionId.AaSoldierRage)]
        [DataRow(MechSprintTemplate, 401U, 6U, ActionId.AaRecruitSprint)]
        public void ThePieceOfEquipmentPerformsItsAbilityAndIsKept(uint template, uint actionId, uint level, ActionId action)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, template);
            var manager = ToyTests.CreateManager(harness, actionId, level, template);
            harness.Client.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)actionId, level, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(queued => queued.ActionId == action);
            ToyTests.Land(harness, manager, pending);

            Assert.IsTrue(harness.Drain().OfType<AbilityRecoveryPacket>().Any(packet => packet.ActionId == action), "performed");

            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id), "kept");
            Assert.AreEqual(1U, item.StackSize);
            Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId), "still in the pack");
        }

        [TestMethod]
        public void AnItemThatIsNotEquipmentIsStillUsedUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = ToyTests.Grant(harness, LogosFistTemplate);
            var manager = ToyTests.CreateManager(harness, 464, 1, LogosFistTemplate);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(464, 1, item.EntityId));
            ToyTests.Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(queued => queued.ActionId == ActionId.AccountrewardEmoteItem));

            using var unit = harness.Context.CreateChar();
            Assert.IsNull(unit.Items.GetItem(item.Id));
        }

        /// <summary>The item, its class loaded with its equipment slot as the server loads it (EntityClassManager).</summary>
        private static Item Grant(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            var item = ToyTests.Grant(harness, template);
            var slot = harness.WorldContext.Set<EquipableClassEntry>().AsNoTracking().Single(row => row.Id == (uint)item.ItemTemplate.Class);

            EntityClassManager.Instance.LoadedEntityClasses[item.ItemTemplate.Class].EquipableClassInfo = new EquipableClassInfo((EquipmentData)slot.SlotId);

            return item;
        }
    }
}
