using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A Hominis Machina's Self Revive (POLY_SELF_RES) is for the dead: asked for standing it is
    /// refused, and if someone else gets the Machina up during its windup it is refused as it
    /// lands, with nothing paid - its cost, its cooldown and its one use were all taken before, and
    /// it failed with "cannot perform that action now".
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SelfReviveTests
    {
        private const uint SelfRes = (uint)ActionId.PolySelfRes;

        [TestMethod]
        public void TheDeadMachinaGetsUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var manager = ToyTests.CreateManager(harness, SelfRes, 1, 0);
            var player = harness.Client.Player;

            Morph(player);
            Kill(harness);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)SelfRes, 1, 0, targetId: player.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(a => a.ActionId == ActionId.PolySelfRes);

            harness.Drain();
            ToyTests.Land(harness, manager, pending);

            Assert.AreNotEqual(CharacterState.Dead, player.State);
            Assert.IsFalse(AbilityManager.CanSelfRevive(player), "used once");
        }

        [TestMethod]
        public void ASelfReviveAskedForStandingIsRefused()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var manager = ToyTests.CreateManager(harness, SelfRes, 1, 0);
            var player = harness.Client.Player;

            Morph(player);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)SelfRes, 1, 0, targetId: player.EntityId));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(a => a.ActionId == ActionId.PolySelfRes));
            Assert.AreEqual(PlayerMessage.PmActionFailedTargetNotDead,
                harness.Drain().OfType<UserActionFailedPacket>().Single(p => p.ActionId == ActionId.PolySelfRes).MsgId);
            Assert.IsTrue(AbilityManager.CanSelfRevive(player));
            Assert.IsFalse(player.ActionReuseUntil.ContainsKey(ActionId.PolySelfRes));
        }

        [TestMethod]
        public void ASelfReviveWhoseMachinaIsRevivedInTheWindupTakesNothing()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var manager = ToyTests.CreateManager(harness, SelfRes, 1, 0);
            var player = harness.Client.Player;

            Morph(player);
            Kill(harness);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request((int)SelfRes, 1, 0, targetId: player.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(a => a.ActionId == ActionId.PolySelfRes);

            // Someone else gets them up while it winds up.
            typeof(PlayerDeath).GetMethod("Revive", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { harness.BootcampMap, harness.Client, player.Position, 100, new Actor(), false });
            Assert.AreNotEqual(CharacterState.Dead, player.State);

            var power = player.Attributes.TryGetValue(Attributes.Power, out var before) ? before.Current : 0;
            harness.Drain();
            ToyTests.Land(harness, manager, pending);

            Assert.AreEqual(PlayerMessage.PmActionFailedTargetNotDead,
                harness.Drain().OfType<UserActionFailedPacket>().Single(p => p.ActionId == ActionId.PolySelfRes).MsgId);
            Assert.IsTrue(AbilityManager.CanSelfRevive(player), "still theirs to use");
            Assert.IsFalse(player.ActionReuseUntil.ContainsKey(ActionId.PolySelfRes), "no cooldown");
            Assert.AreEqual(power, player.Attributes.TryGetValue(Attributes.Power, out var after) ? after.Current : 0, "no cost");
        }

        /// <summary>A Hominis Machina's morph, with Self Revive in its drawer.</summary>
        private static void Morph(Manifestation player)
        {
            player.MorphWeapon = new MorphWeaponItem();
            player.MorphAbilities = new List<(ActionId, uint)> { (ActionId.PolySelfRes, 1u) };
            Assert.IsTrue(AbilityManager.CanSelfRevive(player));
        }

        private static void Kill(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.Client.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsTrue(PlayerDeath.AtZero(harness.BootcampMap, harness.Client.Player, null));
        }
    }
}
