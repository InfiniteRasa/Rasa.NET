using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The Rez Trauma Kit (CONSUMABLE_RES_TRAUMA_KIT 427) was refused as not implemented and kept:
    // a player could not use one at all. Used now, it takes one stack of Rez Trauma off and is
    // used up; with no Rez Trauma it is refused, as the client refuses it.
    [TestClass]
    [DoNotParallelize]
    public class RezTraumaKitTests
    {
        private const int KitAction = 427;
        private const uint ClassOneKit = 47593;     // Consumable_Res_Sickness_Kit_01_05, 427 level 1
        private const uint ClassTenKit = 47602;     // Consumable_Res_Sickness_Kit_10_50, 427 level 10

        [TestMethod]
        [DataRow(ClassOneKit, 1u)]
        [DataRow(ClassTenKit, 10u)]
        public void AKitTakesOneStackOffAndIsUsedUp(uint template, uint level)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            PlayerDeath.RestorePenalties(harness.BootcampMap, player, 3, 300_000, 20_000);
            var kit = ToyTests.Grant(harness, template);
            var manager = ToyTests.CreateManager(harness, KitAction, level, template);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(KitAction, level, kit.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableResTraumaKit);
            ToyTests.Land(harness, manager, pending);

            var trauma = PlayerDeath.CurrentRezTrauma(player);
            Assert.IsNotNull(trauma);
            Assert.AreEqual(2, trauma.Stacks, "one stack off, whatever the kit's class");
            Assert.AreEqual(-40, GameEffectManager.AttributePercentOf(player, Attributes.Body));
            Assert.IsTrue(trauma.ExpiresTick - Environment.TickCount64 is > 290_000 and <= 300_000, "the time it had left");
            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == PlayerDeath.RezSicknessNoHealTypeId), "the no-healing is left");

            var packets = harness.Drain();
            Assert.IsFalse(packets.OfType<UserActionFailedPacket>().Any());
            Assert.IsTrue(packets.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == ActionId.ConsumableResTraumaKit), "the action finishes on the client");

            Assert.IsFalse(player.Inventory.PersonalInventory.Contains(kit.EntityId), "used up");
            using var unit = harness.Context.CreateChar();
            Assert.IsNull(unit.Items.GetItem(kit.Id));
        }

        [TestMethod]
        public void TheLastStackTakesRezTraumaOff()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            PlayerDeath.RestorePenalties(harness.BootcampMap, player, 1, 120_000, 0);
            var kit = ToyTests.Grant(harness, ClassOneKit);
            var manager = ToyTests.CreateManager(harness, KitAction, 1, ClassOneKit);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(KitAction, 1, kit.EntityId));
            ToyTests.Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableResTraumaKit));

            Assert.IsNull(PlayerDeath.CurrentRezTrauma(player));
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == PlayerDeath.RezSicknessTypeId));
            Assert.AreEqual(0, GameEffectManager.AttributePercentOf(player, Attributes.Body));
        }

        [TestMethod]
        public void WithNoRezTraumaItIsRefusedAndKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var kit = ToyTests.Grant(harness, ClassOneKit);
            var manager = ToyTests.CreateManager(harness, KitAction, 1, ClassOneKit);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(KitAction, 1, kit.EntityId));

            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNoRezSickness, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.ConsumableResTraumaKit));
            Assert.AreEqual(1U, kit.StackSize);
        }

        [TestMethod]
        public void ARezTraumaThatRanOutInTheWindupLeavesTheKitUnspent()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            PlayerDeath.RestorePenalties(harness.BootcampMap, player, 1, 120_000, 0);
            var kit = ToyTests.Grant(harness, ClassOneKit);
            var manager = ToyTests.CreateManager(harness, KitAction, 1, ClassOneKit);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(KitAction, 1, kit.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableResTraumaKit);
            GameEffectManager.Instance.DettachEffect(harness.BootcampMap, player, PlayerDeath.CurrentRezTrauma(player));
            harness.Drain();

            ToyTests.Land(harness, manager, pending);

            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNoRezSickness, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(1U, kit.StackSize);
            Assert.IsTrue(player.Inventory.PersonalInventory.Contains(kit.EntityId));
            Assert.IsFalse(player.ActionReuseUntil.ContainsKey(ActionId.ConsumableResTraumaKit), "no cooldown for a kit not used");
        }
    }
}
