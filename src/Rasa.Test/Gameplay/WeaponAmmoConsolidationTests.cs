using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class WeaponAmmoConsolidationTests
    {
        [TestMethod]
        public void ShotPersistsBeforePublishingAndFailureLeavesClipUnchanged()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
                Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
                throw new DbUpdateException("Injected shot failure.");
            };

            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client));

            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(7u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count);
            context.BeforeSave = null;

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(6u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(1, context.World.Map.QueuedMissiles.Count);
            Assert.AreEqual(6u, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<WeaponAmmoInfoPacket>()
                .Single().AmmoInfo);
        }

        [TestMethod]
        [DataRow(20u, 30u)]
        [DataRow(7u, 0u)]
        [DataRow(0u, 0u)]
        public void FullClipAndNoReserveDoNotScheduleOrChangeAmmo(uint clip, uint reserve)
        {
            using var context = new WeaponAmmoContext(clip);
            if (reserve > 0)
                context.AddAmmo(reserve);
            var manager = new ManifestationManager(context);

            manager.RequestWeaponReload(context.Client, true);
            manager.RequestWeaponReload(context.Client, true);

            Assert.AreEqual(0, context.World.Map.PerformRecovery.Count);
            Assert.AreEqual(0, context.SaveAttempts);
            Assert.AreEqual(clip, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
        }

        [TestMethod]
        public void ReloadCommitsClipAndReserveStacksBeforePublishing()
        {
            using var context = new WeaponAmmoContext();
            var first = context.AddAmmo(3);
            var second = context.AddAmmo(4, 51);
            var manager = new ManifestationManager(context);
            manager.RequestWeaponReload(context.Client, true);
            var action = context.World.Map.PerformRecovery.Single();
            WorldTestContext.Drain(context.Client);
            context.BeforeSave = database =>
            {
                Assert.IsNotNull(database.Database.CurrentTransaction);
                Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
                Assert.AreEqual(3u, first.StackSize);
                Assert.AreEqual(4u, second.StackSize);
                Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
            };

            manager.WeaponReload(action);

            Assert.AreEqual(14u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(14u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(0UL, context.Client.Player.Inventory.PersonalInventory[50]);
            Assert.AreEqual(0UL, context.Client.Player.Inventory.PersonalInventory[51]);
            var packets = WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();
            Assert.AreEqual(14u, packets.OfType<WeaponAmmoInfoPacket>().Single().AmmoInfo);
            Assert.AreEqual(14u, packets.OfType<PerformRecoveryPacket>().Single().Arg);
        }

        [TestMethod]
        [DataRow(false, true)]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(true, false)]
        public void ReloadSkipsProtectedFirstAmmoAndUsesOnlyUnboundReserve(bool clearRuntimeOwnership, bool hasOrdinaryAmmo)
        {
            using var context = new WeaponAmmoContext();
            var protectedAmmo = context.AddAmmo(20);
            var assignmentId = Guid.NewGuid().ToString("N");
            protectedAmmo.MissionOwnership = new MissionItemOwnership(context.Client.Player.Id, 901, assignmentId, 1, "survey-ammo");
            using (var unit = context.CreateChar())
                unit.CharacterMissionItems.Save(new CharacterMissionItemEntry
                {
                    CharacterId = context.Client.Player.Id, MissionId = 901, AssignmentId = assignmentId,
                    Generation = 1, ItemKey = "survey-ammo", ItemId = protectedAmmo.Id, Quantity = 20
                });
            if (clearRuntimeOwnership)
                protectedAmmo.MissionOwnership = null;
            var ordinaryAmmo = hasOrdinaryAmmo ? context.AddAmmo(20, 51) : null;
            var manager = new ManifestationManager(context);

            manager.RequestWeaponReload(context.Client, true);

            if (hasOrdinaryAmmo)
            {
                var action = context.World.Map.PerformRecovery.Single();
                WorldTestContext.Drain(context.Client);
                manager.WeaponReload(action);
                Assert.AreEqual(20U, context.Weapon.CurrentAmmo);
                Assert.AreEqual(20U, context.Read(context.Weapon).AmmoCount);
                Assert.AreEqual(7U, ordinaryAmmo.StackSize);
                Assert.AreEqual(7U, context.Read(ordinaryAmmo).StackSize);
                var packets = WorldTestContext.Drain(context.Client)
                    .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToArray();
                Assert.AreEqual(20U, packets.OfType<WeaponAmmoInfoPacket>().Single().AmmoInfo);
                Assert.AreEqual(20U, packets.OfType<PerformRecoveryPacket>().Single().Arg);
            }
            else
            {
                Assert.AreEqual(0, context.World.Map.PerformRecovery.Count, "Protected reserve does not authorize a reload.");
                Assert.AreEqual(7U, context.Weapon.CurrentAmmo);
                Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
            }
            Assert.AreEqual(20U, protectedAmmo.StackSize);
            Assert.AreEqual(20U, context.Read(protectedAmmo).StackSize);
            Assert.AreEqual(protectedAmmo.EntityId, context.Client.Player.Inventory.PersonalInventory[50]);
            using var verify = context.CreateChar();
            var owner = verify.CharacterMissionItems.GetOwner(protectedAmmo.Id);
            Assert.AreEqual(assignmentId, owner.AssignmentId);
            Assert.AreEqual(20U, owner.Quantity);
        }
    }
}
