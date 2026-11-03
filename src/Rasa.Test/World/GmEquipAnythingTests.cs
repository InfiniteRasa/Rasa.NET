using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// .equipanything puts a Specialist's gear on a character who is not one: the server lets
    /// it past the skill, and the GM's client is told they have the skill, which is what its own
    /// check (equipable.py CanActorEquip) reads before it will send the request at all.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GmEquipAnythingTests
    {
        // skilldata: T2_SPECIALIST_HAZMAT_ARMOR and T2_SPECIALIST_LEECH_GUN.
        private const SkillId HazmatArmor = (SkillId)30, LeechGun = (SkillId)31;
        private const uint ArmorClass = 7101, ArmorTemplate = 2901;
        private const uint GunClass = 7102, GunTemplate = 2902;
        private const int GunSkillLevel = 3;

        [TestMethod]
        public void AGmWhoIsNoSpecialistPutsOnASpecialistsArmourAndGun()
        {
            using var context = Context(GmLevel.GameMaster, out var inventory);
            var client = context.Client;
            var player = client.Player;
            var armor = Equipable(context, inventory, ArmorTemplate, ArmorClass, EquipmentData.Torso, HazmatArmor, 1, slot: 0);
            var gun = Equipable(context, inventory, GunTemplate, GunClass, EquipmentData.Weapon, LeechGun, GunSkillLevel, slot: 1);
            var own = Describe(player.Skills);

            // Off: neither goes on.
            Assert.IsFalse(inventory.ValidateItemEquip(client, armor));
            EquipArmor(inventory, client, 0, EquipmentData.Torso);
            EquipGun(inventory, client, 1, 1);
            Assert.AreEqual(0UL, player.Inventory.EquippedInventory[(int)EquipmentData.Torso]);
            Assert.AreEqual(0UL, player.Inventory.WeaponDrawer[1]);
            context.Drain();

            GmAbilities.ToggleEquipAnything(client);
            Assert.IsTrue(player.GmEquipAnything);

            // The client is told it has both skills, at the levels its own check asks for, and
            // still has its own.
            var told = Told(context.Drain());
            Assert.IsTrue(told[HazmatArmor] >= 1, "Hazmat Armor");
            Assert.IsTrue(told[LeechGun] >= GunSkillLevel, "Leech Gun at the gun's level");
            Assert.IsTrue(told[SkillId.Firearms] >= 2, "Firearms kept");
            Assert.IsTrue(ClientWouldEquip(told, armor) && ClientWouldEquip(told, gun), "equipable.py says yes");
            foreach (var (skillId, level) in GmAbilities.EquipmentSkills())
                Assert.IsTrue(told.TryGetValue(skillId, out var shown) && shown >= level, $"{skillId} at {level}");

            // Only told: the character's skills are their own.
            CollectionAssert.AreEquivalent(own, Describe(player.Skills));

            EquipArmor(inventory, client, 0, EquipmentData.Torso);
            EquipGun(inventory, client, 1, 1);
            Assert.AreEqual(armor.EntityId, player.Inventory.EquippedInventory[(int)EquipmentData.Torso], "the armour is on");
            Assert.AreEqual(gun.EntityId, player.Inventory.WeaponDrawer[1], "the gun is in the drawer");

            // Off again: the client is told its own skills and no more.
            context.Drain();
            GmAbilities.ToggleEquipAnything(client);
            Assert.IsFalse(player.GmEquipAnything);
            var after = Told(context.Drain());
            CollectionAssert.AreEquivalent(own, after.Select(skill => $"{(int)skill.Key}:{skill.Value}").ToList());
        }

        [TestMethod]
        public void SkillsSentWhileItIsOnStayWidened()
        {
            using var context = Context(GmLevel.GameMaster, out var inventory);
            var client = context.Client;
            Equipable(context, inventory, GunTemplate, GunClass, EquipmentData.Weapon, LeechGun, GunSkillLevel, slot: 1);

            client.Player.GmEquipAnything = true;

            // Every other send of the skills - a refused LevelSkills, a respec, a new class -
            // goes through SkillsFor, and would otherwise take the skill away again.
            Assert.AreEqual(GunSkillLevel, Told(GmAbilities.SkillsFor(client))[LeechGun]);
            Assert.IsFalse(client.Player.Skills.ContainsKey(LeechGun));
        }

        [TestMethod]
        public void APlayerWhoIsNoGmIsToldTheirOwnSkills()
        {
            using var context = Context(GmLevel.Player, out var inventory);
            var client = context.Client;
            var gun = Equipable(context, inventory, GunTemplate, GunClass, EquipmentData.Weapon, LeechGun, GunSkillLevel, slot: 1);

            client.Player.GmEquipAnything = true;

            var told = Told(GmAbilities.SkillsFor(client));
            Assert.IsFalse(told.ContainsKey(LeechGun));
            Assert.IsFalse(inventory.ValidateItemEquip(client, gun));
        }

        [TestMethod]
        public void TwoSkillsPacketsCarryTheirOwnSkills()
        {
            var first = new SkillsPacket(new Dictionary<SkillId, SkillsData> { [SkillId.Firearms] = new SkillsData(SkillId.Firearms, -1, 1) });
            var second = new SkillsPacket(new Dictionary<SkillId, SkillsData> { [LeechGun] = new SkillsData(LeechGun, -1, 5) });

            CollectionAssert.AreEqual(new[] { (SkillId.Firearms, 1) }, first.Skills.ToArray());
            CollectionAssert.AreEqual(new[] { (LeechGun, 5) }, second.Skills.ToArray());
        }

        private static MissionTestContext Context(GmLevel level, out InventoryManager inventory)
        {
            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
            var player = context.Client.Player;
            var account = context.Client.AccountEntry;
            account.Level = (byte)level;

            player.Skills = new Dictionary<SkillId, SkillsData>
            {
                [SkillId.Firearms] = new SkillsData(SkillId.Firearms, -1, 2),
                [SkillId.HandToHand] = new SkillsData(SkillId.HandToHand, -1, 1),
            };
            player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            player.Inventory.WeaponDrawer = Enumerable.Repeat(0UL, 5).ToList();
            player.ActiveWeapon = 0;

            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit })
                player.Attributes[attribute] = new ActorAttributes(attribute, 10, 10, 10, 0, 0);
            foreach (var attribute in new[] { Attributes.Health, Attributes.Chi, Attributes.Power })
                player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            player.Attributes[Attributes.Regen] = new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);

            typeof(ManifestationManager)
                .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(null, new ManifestationManager(context));

            inventory = new InventoryManager(context, context.Manager);
            context.Drain();
            return context;
        }

        private static Item Equipable(MissionTestContext context, InventoryManager inventory, uint templateId, uint classId,
            EquipmentData slotId, SkillId skill, int skillLevel, uint slot)
        {
            // CreateInventoryItem makes the template afresh; what it asks for is set after.
            var item = context.CreateInventoryItem(templateId, classId, 1);
            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
            classInfo.EquipableClassInfo = new EquipableClassInfo(slotId);
            Assert.AreSame(classInfo.ItemTemplates[templateId], item.ItemTemplate);
            item.ItemTemplate.EquipableInfo = new EquipableInfo((int)skill, skillLevel);

            inventory.AddItemBySlot(context.Client, InventoryType.Personal, item.EntityId, slot, true, true);
            return item;
        }

        private static void EquipArmor(InventoryManager inventory, Client client, uint from, EquipmentData to) =>
            inventory.RequestEquipArmor(client, new RequestEquipArmorPacket { SrcInventory = InventoryType.Personal, SrcSlot = from, DestSlot = (uint)to });

        private static void EquipGun(InventoryManager inventory, Client client, uint from, uint drawerSlot) =>
            inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = from, DestSlot = drawerSlot });

        /// <summary>The skill half of equipable.py's CanActorEquip, against the levels it was sent.</summary>
        private static bool ClientWouldEquip(Dictionary<SkillId, int> told, Item item)
        {
            var equipable = item.ItemTemplate.EquipableInfo;
            if (equipable == null || equipable.SkillId <= 0 || equipable.SkillLevel <= 0)
                return true;

            return told.TryGetValue((SkillId)equipable.SkillId, out var level) && level >= equipable.SkillLevel;
        }

        private static Dictionary<SkillId, int> Told(IEnumerable<object> sent) =>
            Told(sent.OfType<SkillsPacket>().Last());

        private static Dictionary<SkillId, int> Told(SkillsPacket packet) =>
            packet.Skills.ToDictionary(skill => skill.SkillId, skill => skill.SkillLevel);

        private static List<string> Describe(Dictionary<SkillId, SkillsData> skills) =>
            skills.Values.Select(skill => $"{(int)skill.SkillId}:{skill.SkillLevel}").ToList();
    }
}
