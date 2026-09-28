using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class AbilityTrayConsolidationTests
    {
        [TestMethod]
        public void PersistedAbilitySelectionLoadsAndArmCommitsBeforePublication()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            using (var unit = context.CreateChar())
                unit.Characters.UpdateCharacterAbilitySlot(context.Client.Player.Id, 7);

            using (var unit = context.CreateChar())
            {
                var reloaded = new Manifestation(unit.Characters.Get(context.Client.Player.Id), new());
                Assert.AreEqual(7, reloaded.CurrentAbilityDrawer);
            }

            context.AfterSave = database =>
            {
                Assert.AreEqual(0, context.Client.Player.CurrentAbilityDrawer);
                Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
            };

            manager.RequestArmAbility(context.Client, 24);

            Assert.AreEqual(24, context.Client.Player.CurrentAbilityDrawer);
            Assert.AreEqual(24, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                .Select(message => message.Packet).OfType<AbilityDrawerSlotPacket>().Single().AbilityDrawerSlot);
            using var verify = context.CreateChar();
            Assert.AreEqual((byte)24, verify.Characters.Get(context.Client.Player.Id).CurrentAbilitySlot);
        }

        [TestMethod]
        public void InvalidAbilitySelectionDoesNotMutatePersistOrPublish()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var saves = context.SaveAttempts;

            manager.RequestArmAbility(context.Client, 25);

            Assert.AreEqual(0, context.Client.Player.CurrentAbilityDrawer);
            Assert.AreEqual(saves, context.SaveAttempts);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
        }

        [TestMethod]
        public void DrawerSetValidatesLearnedRankAndRollsBackBeforeRuntimePublication()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.Skills[(SkillId)165] = new SkillsData((SkillId)165, 401, 2);
            var request = new RequestSetAbilitySlotPacket
            {
                SlotId = 3,
                AbilityId = 401,
                AbilityLevel = 2
            };
            context.AfterSave = _ => throw new DbUpdateException("Injected drawer failure.");

            manager.RequestSetAbilitySlot(context.Client, request);

            Assert.AreEqual(0, context.Client.Player.Abilities.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(context.Client).Count);
            using (var verify = context.CreateChar())
                Assert.AreEqual(0, verify.CharacterAbilityDrawers
                    .GetCharacterAbilities(context.Client.Player.Id).Count);

            context.AfterSave = null;
            manager.RequestSetAbilitySlot(context.Client, request);
            Assert.AreEqual(2u, context.Client.Player.Abilities[3].AbilityLevel);
            Assert.AreEqual(1, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                .Select(message => message.Packet).OfType<AbilityDrawerPacket>().Count());
        }

        [TestMethod]
        public void DrawerSwapMovesBothSlotsAtomicallyWithoutAliasing()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.Skills[(SkillId)49] = new SkillsData((SkillId)49, 194, 3);
            context.Client.Player.Skills[(SkillId)165] = new SkillsData((SkillId)165, 401, 3);
            manager.RequestSetAbilitySlot(context.Client, new RequestSetAbilitySlotPacket
                { SlotId = 0, AbilityId = 194, AbilityLevel = 1 });
            manager.RequestSetAbilitySlot(context.Client, new RequestSetAbilitySlotPacket
                { SlotId = 1, AbilityId = 401, AbilityLevel = 2 });
            WorldTestContext.Drain(context.Client);

            manager.RequestSwapAbilitySlots(context.Client,
                new RequestSwapAbilitySlotsPacket { FromSlot = 0, ToSlot = 1 });

            Assert.AreEqual(401, context.Client.Player.Abilities[0].AbilityId);
            Assert.AreEqual(194, context.Client.Player.Abilities[1].AbilityId);
            Assert.AreEqual(0, context.Client.Player.Abilities[0].AbilitySlotId);
            Assert.AreEqual(1, context.Client.Player.Abilities[1].AbilitySlotId);
            Assert.AreNotSame(context.Client.Player.Abilities[0], context.Client.Player.Abilities[1]);
        }

        [TestMethod]
        public void AbilityLoadoutPublicationIncludesEmptyDrawerAndPersistedSelection()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.Client.Player.CurrentAbilityDrawer = 24;

            manager.PublishAbilityLoadout(context.Client);

            var packets = WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                .Select(message => message.Packet).ToList();
            Assert.AreEqual(0, packets.OfType<AbilityDrawerPacket>().Single().Abilities.Count);
            Assert.AreEqual(24, packets.OfType<AbilityDrawerSlotPacket>().Single().AbilityDrawerSlot);
        }
    }
}
