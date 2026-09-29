using System.Linq;
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

    [TestClass]
    [DoNotParallelize]
    public class ChaingunTests
    {
        [TestMethod]
        [DataRow(1u, 107)]     // Chaingun
        [DataRow(3u, 107)]     // Pulse Chaingun
        [DataRow(4u, 107)]     // Laser Chaingun
        [DataRow(5u, 107)]     // Electric Chaingun
        [DataRow(7u, 445)]     // Series 3 Chaingun
        [DataRow(8u, 443)]     // Series 2 Chaingun
        [DataRow(9u, 455)]     // Series 2 Laser Pistol
        [DataRow(10u, 456)]    // Series 3 Laser Pistols
        [DataRow(11u, 462)]    // Auto Cannon
        [DataRow(16u, 462)]    // Auto Cannon
        public void EachMachineGunHasItsConstantFireEffect(uint actionArgId, int effectTypeId)
        {
            Assert.IsTrue(ConstantFire.IsConstantFire(ActionId.WeaponMachinegun));
            Assert.AreEqual(effectTypeId, ConstantFire.EffectTypeOf(ActionId.WeaponMachinegun, actionArgId));
        }

        [TestMethod]
        public void HoldingTheTriggerRunsTheEffectAndLettingGoTakesItOff()
        {
            using var context = new WeaponAmmoContext();
            foreach (var attribute in new[] { Attributes.Armor, Attributes.Power, Attributes.Regen })
                context.Client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            var previous = weaponClass.WeaponClassInfo;
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = (uint)ActionId.WeaponMachinegun, AttackActionArgId = 4,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = 55, MaxDamage = 55, DamageType = (byte)DamageType.Laser, WeaponAnimConditionCode = 1
            });

            try
            {
                var manager = new ManifestationManager(context);

                Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

                Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count, "no single shot");
                Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
                var sent = Sent(context);
                var attached = sent.OfType<GameEffectAttachedPacket>().Single();
                Assert.AreEqual(ConstantFire.MachinegunTypeId, attached.EffectTypeId);
                Assert.AreEqual((uint)DamageType.Laser, attached.EffectLevel, "the Laser Chaingun's FX level");
                Assert.HasCount(1, sent.OfType<ConstantFireTickPacket>().ToArray());
                Assert.IsTrue(ConstantFire.IsFiring(context.Client));

                // Held: the next interval is another tick of the same effect.
                context.Client.Player.NextShotAt = 0;
                Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
                sent = Sent(context);
                Assert.IsEmpty(sent.OfType<GameEffectAttachedPacket>().ToArray());
                Assert.HasCount(1, sent.OfType<ConstantFireTickPacket>().ToArray());

                // Let go: the effect comes off - which is what stops the charge on the client.
                manager.StopAutoFire(context.Client);
                sent = Sent(context);
                Assert.AreEqual(attached.EffectId, sent.OfType<GameEffectDetachedPacket>().Single().EffectId);
                Assert.IsFalse(ConstantFire.IsFiring(context.Client));
            }
            finally
            {
                weaponClass.WeaponClassInfo = previous;
            }
        }

        [TestMethod]
        public void ALoneWeaponAttackRequestDoesNotStartTheFire()
        {
            using var context = new WeaponAmmoContext();
            foreach (var attribute in new[] { Attributes.Armor, Attributes.Power, Attributes.Regen })
                context.Client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            var previous = weaponClass.WeaponClassInfo;
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = (uint)ActionId.WeaponMachinegun, AttackActionArgId = 1,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = 55, MaxDamage = 55, DamageType = (byte)DamageType.Physical, WeaponAnimConditionCode = 1
            });

            try
            {
                Sent(context);
                MissileManager.Instance.RequestWeaponAttack(context.Client,
                    new Rasa.Packets.MapChannel.Client.RequestWeaponAttackPacket { ActionId = ActionId.WeaponMachinegun, ActionArgId = 1 });

                Assert.IsFalse(ConstantFire.IsFiring(context.Client), "only the held trigger runs it: nothing would take it off");
                Assert.IsEmpty(Sent(context).OfType<GameEffectAttachedPacket>().ToArray());
                Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count);
            }
            finally
            {
                weaponClass.WeaponClassInfo = previous;
            }
        }

        private static Rasa.Packets.PythonPacket[] Sent(WeaponAmmoContext context) =>
            WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToArray();
    }
}
