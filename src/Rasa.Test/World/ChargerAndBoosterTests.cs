using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The Armor and Power Chargers (CONSUMABLE_RESTORE_KIT_ARMOR 467, _POWER 486) and the
    // Adrenaline Booster (CONSUMABLE_ADRENALINE_BOOST 459) were refused as not implemented and
    // kept. A charger restores its bar every second for its duration, as its item text says; the
    // booster adds its share of the adrenaline bar at once.
    [TestClass]
    [DoNotParallelize]
    public class ChargerAndBoosterTests
    {
        private const uint ArmorAction = 467;
        private const uint PowerAction = 486;
        private const uint BoosterAction = 459;

        private const uint ArmorClassOne = 111133;
        private const uint ArmorClassSix = 118897;
        private const uint ArmorClassSeven = 118898;
        private const uint PowerClassTen = 118910;
        private const uint BoosterClassOne = 111136;
        private const uint BoosterClassNine = 118816;

        [TestMethod]
        public void AnArmorChargerRestoresArmorEverySecondUpToTheMaximumAndIsUsedUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 120, 120, 0, 0, 1);

            var kit = Use(harness, ArmorAction, 1, ArmorClassOne);

            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == 342);
            Assert.AreEqual(Attributes.Armor, effect.TickRestores);
            Assert.AreEqual(50, effect.TickHealMin, "500 over 10 seconds");
            Assert.AreEqual(1000, effect.TickIntervalMs);
            Assert.IsTrue(effect.ExpiresTick - effect.NextTickTick is > 9_000 and <= 10_000);

            Assert.AreEqual(50, Tick(harness, effect));
            Assert.AreEqual(50, player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(50, Tick(harness, effect));

            // Up to the maximum, and the tick says what was added: the client adds it itself.
            Assert.AreEqual(20, Tick(harness, effect));
            Assert.AreEqual(120, player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(0, Tick(harness, effect));

            Assert.IsFalse(player.Inventory.PersonalInventory.Contains(kit.EntityId), "used up");
            Assert.IsTrue(player.ActionReuseUntil.ContainsKey(ActionId.ConsumableRestoreKitArmor), "the 30 s cooldown");
        }

        [TestMethod]
        public void APowerChargerRestoresPower()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 500, 500, 100, 0, 1);
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 500, 500, 0, 0, 1);

            Use(harness, PowerAction, 10, PowerClassTen);

            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == 343);
            Assert.AreEqual(Attributes.Power, effect.TickRestores);
            Assert.IsTrue(effect.ExpiresTick - effect.NextTickTick is > 4_000 and <= 5_000, "5 seconds");
            Assert.AreEqual(60, Tick(harness, effect), "300 over 5 seconds");
            Assert.AreEqual(160, player.Attributes[Attributes.Power].Current);
            Assert.AreEqual(0, player.Attributes[Attributes.Armor].Current, "and nothing else");
        }

        [TestMethod]
        [DataRow(ArmorClassSix, 6u, 440)]
        [DataRow(ArmorClassSeven, 7u, 680)]
        public void ClassesSixAndSevenRestoreWhatTheirTextSays(uint template, uint level, int perSecond)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 50_000, 50_000, 0, 0, 1);

            Use(harness, ArmorAction, level, template);

            Assert.AreEqual(perSecond, player.ActiveEffects.Values.Single(e => e.TypeId == 342).TickHealMin);
        }

        [TestMethod]
        public void AChargerRestoresNothingToTheDead()
        {
            var player = new Manifestation { State = CharacterState.Dead };
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 0, 0, 1);

            Assert.AreEqual(0, ActorManager.Restore(player, Attributes.Armor, 50));
            Assert.AreEqual(0, player.Attributes[Attributes.Armor].Current);
        }

        [TestMethod]
        public void TheBoosterFillsTheBarAndIsUsedUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 200, 200, 20, 0, 1);

            var booster = Use(harness, BoosterAction, 1, BoosterClassOne, drain: false);

            Assert.AreEqual(200, player.Attributes[Attributes.Chi].Current);
            var packets = harness.Drain();
            Assert.AreEqual(200, packets.OfType<UpdateChiPacket>().Single().Chi.Current, "announced: the floating number");
            Assert.IsFalse(packets.OfType<UserActionFailedPacket>().Any());
            Assert.IsFalse(player.Inventory.PersonalInventory.Contains(booster.EntityId), "used up");
            Assert.IsTrue(player.ActionReuseUntil.ContainsKey(ActionId.ConsumableAdrenalineBoost), "the 5 minute cooldown");
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.ActionId == ActionId.ConsumableAdrenalineBoost), "instant: no effect left on");
        }

        [TestMethod]
        public void PastItsLevelTheBoosterAddsLess()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Level = 50;
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 200, 200, 0, 0, 1);

            Use(harness, BoosterAction, 9, BoosterClassNine);

            Assert.AreEqual(180, player.Attributes[Attributes.Chi].Current, "Class IX at level 50: slightly diminished");
        }

        [TestMethod]
        [DataRow(5, 9, 100)]        // Class I below level 10
        [DataRow(5, 10, 90)]        // "If you are Level 10 or higher ... diminished per level"
        [DataRow(5, 14, 50)]
        [DataRow(5, 19, 0)]
        [DataRow(45, 50, 90)]       // Class IX: "slightly diminished" at 50
        [DataRow(50, 50, 100)]      // Class X: "Instantly adds 100% Adrenaline"
        public void TheBoostByLevel(int nominal, int playerLevel, int percent)
        {
            var info = new ActionLevelInfo();
            info.Properties[AbilityProperty.NominalLevel] = nominal;
            info.Properties[AbilityProperty.LevelPercent] = 10;

            Assert.AreEqual(percent, AbilityManager.AdrenalineBoostPercent(playerLevel, info));
        }

        [TestMethod]
        public void OnAFullBarTheBoosterIsRefusedAndKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 200, 200, 200, 0, 1);
            var booster = ToyTests.Grant(harness, BoosterClassOne);
            var manager = ToyTests.CreateManager(harness, BoosterAction, 1, BoosterClassOne);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)BoosterAction, 1, booster.EntityId));

            Assert.AreEqual(PlayerMessage.PmCannotPerformActionFullAdrenaline, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(1U, booster.StackSize);
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.ConsumableAdrenalineBoost));
        }

        [TestMethod]
        public void ABarThatFilledDuringTheRecoveryLeavesTheBoosterUnspent()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 200, 200, 20, 0, 1);
            var booster = ToyTests.Grant(harness, BoosterClassOne);
            var manager = ToyTests.CreateManager(harness, BoosterAction, 1, BoosterClassOne);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)BoosterAction, 1, booster.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumableAdrenalineBoost);
            player.Attributes[Attributes.Chi].Current = 200;
            harness.Drain();

            ToyTests.Land(harness, manager, pending);

            Assert.AreEqual(PlayerMessage.PmCannotPerformActionFullAdrenaline, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(1U, booster.StackSize);
            Assert.IsFalse(player.ActionReuseUntil.ContainsKey(ActionId.ConsumableAdrenalineBoost), "no cooldown for a booster not used");
        }

        [TestMethod]
        public void ABoosterThatWouldAddNothingIsRefusedAndKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.Level = 30;
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 200, 200, 0, 0, 1);
            var booster = ToyTests.Grant(harness, BoosterClassOne);
            var manager = ToyTests.CreateManager(harness, BoosterAction, 1, BoosterClassOne);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)BoosterAction, 1, booster.EntityId));

            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(1U, booster.StackSize);
        }

        /// <summary>Uses the item, lands the action, and drains what it sent unless asked not to.</summary>
        private static Item Use(BootcampRuntimeTestHarness.Harness harness, uint action, uint level, uint template, bool drain = true)
        {
            var item = ToyTests.Grant(harness, template);
            var manager = ToyTests.CreateManager(harness, action, level, template);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)action, level, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(candidate => candidate.ActionId == (ActionId)action);
            ToyTests.Land(harness, manager, pending);

            if (drain)
            {
                var packets = harness.Drain();
                Assert.IsFalse(packets.OfType<UserActionFailedPacket>().Any());
                Assert.IsTrue(packets.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == (ActionId)action), "the action finishes on the client");
            }

            return item;
        }

        /// <summary>One tick of the charger, due now: what it says it added, with no armour or power update of its own.</summary>
        private static int Tick(BootcampRuntimeTestHarness.Harness harness, GameEffect effect)
        {
            effect.NextTickTick = 0;
            harness.Drain();

            GameEffectManager.Instance.DoWork(harness.BootcampMap, 1000);

            var packets = harness.Drain();
            var tick = packets.OfType<GameEffectTickPacket>().Single(p => p.EffectId == effect.EffectId);

            Assert.IsFalse(packets.OfType<UpdateArmorPacket>().Any() || packets.OfType<UpdatePowerPacket>().Any(), "the client adds the tick itself");

            return tick.Entries.Sum(entry => entry.Amount);
        }
    }
}
