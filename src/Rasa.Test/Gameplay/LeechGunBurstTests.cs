using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    /// <summary>
    /// A leech gun's fire opens with a burst: "One click will fire a 2 ammunition burst which
    /// deals double the damage listed on the weapon" (ConstantFire.BurstShotsOf). Holding the
    /// trigger goes on at a round and the listed damage a pulse.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LeechGunBurstTests
    {
        [TestMethod]
        public void TheFirstPulseIsTwoRoundsForTwiceTheDamageAndTheRestAreOne()
        {
            // A crit on either pulse would hide the ratio; the trigger is pulled again until neither is one.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                using var context = Leech(clip: 7, out var creature, out var manager, out var restore);

                try
                {
                    Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
                    var burst = Sent(context);
                    Assert.AreEqual(5u, context.Weapon.CurrentAmmo, "two rounds");
                    AssertClipToldAfterTheTick(burst, 5);

                    context.Client.Player.NextShotAt = 0;
                    Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
                    var held = Sent(context);
                    Assert.AreEqual(4u, context.Weapon.CurrentAmmo, "one round");
                    AssertClipToldAfterTheTick(held, 4);

                    var first = Shot(burst, creature);
                    var second = Shot(held, creature);

                    if (first.IsCritical || second.IsCritical)
                        continue;

                    Assert.IsTrue(second.Amount > 0);
                    Assert.AreEqual(2.0 * second.Amount, first.Amount, 2.0, "twice the damage, as one hit");

                    // Let go and pull again: another burst.
                    manager.StopAutoFire(context.Client);
                    context.Client.Player.NextShotAt = 0;
                    Sent(context);
                    Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
                    Assert.AreEqual(2u, context.Weapon.CurrentAmmo, "a new pull is a new burst");
                    return;
                }
                finally
                {
                    manager.StopAutoFire(context.Client);
                    restore();
                }
            }

            Assert.Fail("twenty pulls and a crit in every one");
        }

        [TestMethod]
        public void WithOneRoundLeftTheBurstIsOneShot()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                using var context = Leech(clip: 1, out var creature, out var manager, out var restore);

                try
                {
                    var before = creature.Attributes[Attributes.Health].Current;
                    Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
                    var shot = Shot(Sent(context), creature);
                    Assert.AreEqual(0u, context.Weapon.CurrentAmmo);

                    if (shot.IsCritical)
                        continue;

                    // The listed 55, under the bead it was fired with: never past the one shot.
                    Assert.IsTrue(shot.Amount > 0 && shot.Amount <= 55, $"{shot.Amount}");
                    Assert.AreEqual(before - shot.Amount, creature.Attributes[Attributes.Health].Current);
                    return;
                }
                finally
                {
                    manager.StopAutoFire(context.Client);
                    restore();
                }
            }

            Assert.Fail("twenty pulls and a crit in every one");
        }

        [TestMethod]
        public void OnlyALeechGunBursts()
        {
            Assert.AreEqual(2, ConstantFire.BurstShotsOf(ActionId.WeaponDensitygun, 50, 1));
            Assert.AreEqual(1, ConstantFire.BurstShotsOf(ActionId.WeaponDensitygun, 1, 1));
            Assert.AreEqual(1, ConstantFire.BurstShotsOf(ActionId.WeaponDensitygun, 3, 2));
            Assert.AreEqual(2, ConstantFire.BurstShotsOf(ActionId.WeaponDensitygun, 0, 0), "no rounds to count");

            foreach (var other in new[] { ActionId.WeaponMachinegun, ActionId.WeaponPolaritygun, ActionId.WeaponFlamethrower })
                Assert.AreEqual(1, ConstantFire.BurstShotsOf(other, 50, 1), $"{other}");
        }

        /// <summary>
        /// The client takes a shot's rounds off its own count on the tick, so the clip is told
        /// after it, and what it is told is what is left.
        /// </summary>
        private static void AssertClipToldAfterTheTick(Rasa.Packets.PythonPacket[] sent, uint left)
        {
            var tick = Array.FindIndex(sent, packet => packet is ConstantFireTickPacket);
            var clip = Array.FindLastIndex(sent, packet => packet is WeaponAmmoInfoPacket);

            Assert.IsTrue(tick >= 0, "a tick");
            Assert.IsTrue(clip > tick, "the clip after the tick");
            Assert.AreEqual(1, sent.OfType<WeaponAmmoInfoPacket>().Count());
            Assert.AreEqual(left, ((WeaponAmmoInfoPacket)sent[clip]).AmmoInfo);
        }

        private static WeaponAmmoContext Leech(uint clip, out Creature creature, out ManifestationManager manager, out Action restore)
        {
            var context = new WeaponAmmoContext(clip);
            var player = context.Client.Player;

            foreach (var attribute in new[] { Attributes.Armor, Attributes.Power, Attributes.Regen })
                player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);

            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            var previous = weaponClass.WeaponClassInfo;
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = (uint)ActionId.WeaponDensitygun, AttackActionArgId = 1,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 50, MinDamage = 55, MaxDamage = 55, DamageType = (byte)DamageType.Physical, WeaponAnimConditionCode = 1
            });
            restore = () => weaponClass.WeaponClassInfo = previous;

            // Straight ahead of the shooter, well inside the gun's range.
            creature = ConstantFireTargetTests.Spawn(context.World, player.Position + new Vector3(0, 0, 5), TargetCategory.Hostile);
            player.Target = creature.EntityId;

            manager = new ManifestationManager(context);
            WorldTestContext.Drain(context.Client);
            return context;
        }

        private static TickEntry Shot(Rasa.Packets.PythonPacket[] sent, Creature creature) =>
            sent.OfType<ConstantFireTickPacket>().Single().Pulses.Single().Single(shot => shot.EntityId == creature.EntityId);

        private static Rasa.Packets.PythonPacket[] Sent(WeaponAmmoContext context) =>
            WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToArray();
    }
}
