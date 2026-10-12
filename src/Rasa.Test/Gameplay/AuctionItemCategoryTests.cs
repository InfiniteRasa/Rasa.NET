using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;

    // The auction house's browse categories (AuctionItemCategories, BR-185): which items are in
    // which, worked out from the item's data rather than words in its class name.
    [TestClass]
    [DoNotParallelize]
    public class AuctionItemCategoryTests
    {
        private const uint FirstClass = 990100;
        private const uint FirstTemplate = 9901000;

        private readonly List<EntityClasses> _added = new List<EntityClasses>();
        private uint _nextClass = FirstClass;
        private uint _nextTemplate = FirstTemplate;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var classId in _added)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(classId);

            ItemModules.Load(Enumerable.Empty<ModuleClassEntry>(), Enumerable.Empty<ModuleEffectEntry>());
            ItemModules.LoadCrafting(Enumerable.Empty<ModuleItemEntry>(), Enumerable.Empty<ModifiableClassEntry>());
        }

        [TestMethod]
        public void TheCodesAreTheClientsOneForEachCategory()
        {
            CollectionAssert.AreEquivalent(AuctionCategory.Names.Keys.ToArray(), AuctionCategory.Codes.Keys.ToArray());
            Assert.HasCount(83, AuctionCategory.Codes);
            Assert.AreEqual(0x02000000u, AuctionCategory.Codes[16], "Armor");
            Assert.AreEqual(0x02030000u, AuctionCategory.Codes[33], "Hazmat");
            Assert.AreEqual(0x02030100u, AuctionCategory.Codes[34], "Hazmat, Head");
            Assert.AreEqual(0x010F0000u, AuctionCategory.Codes[106], "Tools, under Weapon");

            Assert.AreEqual(0xFF000000u, AuctionItemCategories.MaskOf(0x02000000));
            Assert.AreEqual(0xFFFF0000u, AuctionItemCategories.MaskOf(0x02030000));
            Assert.AreEqual(0xFFFFFF00u, AuctionItemCategories.MaskOf(0x02030100));
        }

        [TestMethod]
        public void AWeaponIsTheKindItsClassSaysAndAToolIsATool()
        {
            var pistol = Weapon("Weapon_Avatar_Pistol_Physical_CMN_01_to_04", ToolType.Pistol);
            var rifle = Weapon("Weapon_Avatar_Rifle_Physical_CMN_01_to_05", ToolType.Rifle);
            var torqueshell = Weapon("Weapon_Avatar_Torqueshell_Rifle_Physical_CMN_33_to_37");
            var grenades = Weapon("Weapon_Avatar_GrenadeLauncher_Physical_CMN_15_to_19", ToolType.GrenadeLauncher);
            var sword = Weapon("Weapon_Human_Redshirt_Sword_Electric");
            var disc = Weapon("Tool_Avatar_Healing_Disc_Direct_CMN_05_to_09", ToolType.HealingDisc, skill: 14);
            var extractor = Weapon("Tool_Avatar_Tissue_Extractor_40", ToolType.TissueExtractor);
            var odd = Weapon("Weapon_Creature_Mox");

            Assert.AreEqual("Weapon_Pistol", AuctionItemCategories.CategoryOf(pistol));
            Assert.AreEqual("Weapon_Rifle", AuctionItemCategories.CategoryOf(rifle));
            Assert.AreEqual("Weapon_SniperRifle", AuctionItemCategories.CategoryOf(torqueshell), "a Torqueshell_Rifle is no Rifle");
            Assert.AreEqual("Weapon_RPG", AuctionItemCategories.CategoryOf(grenades), "the client calls RPG Grenade Launcher");
            Assert.AreEqual("Weapon_Blade", AuctionItemCategories.CategoryOf(sword));
            Assert.AreEqual("Weapon_Tool", AuctionItemCategories.CategoryOf(disc));
            Assert.AreEqual("Weapon_Tool", AuctionItemCategories.CategoryOf(extractor), "no Tools skill, but a tool");
            Assert.AreEqual("Weapon", AuctionItemCategories.CategoryOf(odd));

            foreach (var weapon in new[] { pistol, rifle, torqueshell, grenades, sword, disc, odd })
                Assert.IsTrue(AuctionItemCategories.InCategory(weapon, 1), "all of them are Weapon");

            Assert.IsTrue(AuctionItemCategories.InCategory(torqueshell, 14));
            Assert.IsFalse(AuctionItemCategories.InCategory(torqueshell, 3));
            Assert.IsFalse(AuctionItemCategories.InCategory(rifle, 14));
            Assert.IsTrue(AuctionItemCategories.InCategory(disc, 106));
            Assert.IsFalse(AuctionItemCategories.InCategory(disc, 2));
        }

        [TestMethod]
        public void ArmorIsTheKindItsSkillIsForInTheSlotItIsWornIn()
        {
            var helmet = Worn("Armor_T2_Hazmat_V01_CMN_Helmet_24_to_28", EquipmentData.Helmet, skill: 30);
            var vest = Worn("Armor_T3_Mech_V01_CMN_Vest_45_to_49", EquipmentData.Torso, skill: 57);
            var gloves = Worn("Armor_T3_Bio_V01_CMN_Gloves_50_to_50", EquipmentData.Gloves, skill: 66);
            var boots = Worn("Armor_T1_MotorAssist_V01_CMN_Boots_03_to_07", EquipmentData.Shoes, skill: 19);
            var shirt = Worn("Clothing_Torso_1", EquipmentData.Torso);
            var goggles = Worn("AvatarSwap_Detail_Face_Hi_v01", EquipmentData.Eyewear);
            var hair = Worn("AvatarSwap_Hair_02", EquipmentData.Hair);
            var shield = Worn("Shield_Creature_Minion_01", EquipmentData.Wing);

            Assert.AreEqual("Armor_Hazmat_Head", AuctionItemCategories.CategoryOf(helmet));
            Assert.AreEqual("Armor_MechSuit_Chest", AuctionItemCategories.CategoryOf(vest));
            Assert.AreEqual("Armor_BioSuit_Hands", AuctionItemCategories.CategoryOf(gloves));
            Assert.AreEqual("Armor_MotorAssist_Feet", AuctionItemCategories.CategoryOf(boots));
            Assert.AreEqual("Armor_Accessory", AuctionItemCategories.CategoryOf(shirt), "no armor skill");
            Assert.AreEqual("Armor_Accessory_UpperFace", AuctionItemCategories.CategoryOf(goggles));
            Assert.IsNull(AuctionItemCategories.CategoryOf(hair));
            Assert.IsNull(AuctionItemCategories.CategoryOf(shield));

            Assert.IsTrue(AuctionItemCategories.InCategory(helmet, 34), "Hazmat, Head");
            Assert.IsTrue(AuctionItemCategories.InCategory(helmet, 33), "Hazmat");
            Assert.IsTrue(AuctionItemCategories.InCategory(helmet, 16), "Armor");
            Assert.IsFalse(AuctionItemCategories.InCategory(helmet, 26), "Reflective, Head");
            Assert.IsFalse(AuctionItemCategories.InCategory(helmet, 37), "Hazmat, Torso");
            Assert.IsTrue(AuctionItemCategories.InCategory(vest, 61));
            Assert.IsTrue(AuctionItemCategories.InCategory(shirt, 113));
            Assert.IsTrue(AuctionItemCategories.InCategory(goggles, 114));
            Assert.IsFalse(AuctionItemCategories.InCategory(hair, 16), "in no category, not even the top one");
        }

        [TestMethod]
        public void ConsumablesAndCraftingGoByWhatTheItemIs()
        {
            var cartridges = Thing("Ammo_Cartridge_1_Standard_Grade", InventoryCategory.Consumable);
            var micromech = Thing("Ammo_Micromech_4_Weapons_Grade", InventoryCategory.Consumable);
            Weapon("Weapon_Avatar_Rifle_Fixture", ToolType.Rifle, ammo: cartridges.Class);

            var metal = Thing("Metal1", InventoryCategory.Crafting);
            var component = Thing("Component_ArmorDye_Bonding_Agent", InventoryCategory.Crafting);
            var junk = Thing("Loot_Junk_Boargar_Tusk", InventoryCategory.Misc);
            var grenade = Thing("Consumable_Grenade_Physical_01_05", InventoryCategory.Consumable);
            var medpack = Thing("Consumable_Medpack_01A_05", InventoryCategory.Consumable);
            var kit = Thing("Consumable_Res_Sickness_Kit_01_05", InventoryCategory.Consumable);
            var dye = Thing("Modification_ArmorDye_Crafted_Primary_Fuschias_Dark", InventoryCategory.Consumable, AugmentationType.Customization);
            var booster = Thing("Consumable_XP_Booster", InventoryCategory.Consumable);
            var token = Thing("CP_Token_Wilde_CP02_Assault", InventoryCategory.Misc);
            var ammoRecipe = Thing("Recipe_Ammo_Cartridges_1", InventoryCategory.Crafting, AugmentationType.Recipe);
            var paintRecipe = Thing("Recipe_ArmorDye_Crafted_Primary_Fuschias_Bright", InventoryCategory.Crafting, AugmentationType.Recipe);
            var resourceRecipe = Thing("Recipe_Resource_Upgrade_Scrap_2", InventoryCategory.Crafting, AugmentationType.Recipe);
            var medpackRecipe = Thing("Recipe_Consumable_Medpack_01A", InventoryCategory.Crafting, AugmentationType.Recipe);
            var mission = Thing("Mis_Wilderness_Item_EssenceOfTinctu", InventoryCategory.Mission);

            Assert.AreEqual("Consumables_Ammunition", AuctionItemCategories.CategoryOf(cartridges), "a weapon fires it");
            Assert.AreEqual("Consumables_Resources", AuctionItemCategories.CategoryOf(micromech), "no weapon fires it");
            Assert.AreEqual("Consumables_Resources", AuctionItemCategories.CategoryOf(metal));
            Assert.AreEqual("Consumables_Resources", AuctionItemCategories.CategoryOf(component));
            Assert.AreEqual("Crafting_Salvage", AuctionItemCategories.CategoryOf(junk));
            Assert.AreEqual("Consumables_Explosives", AuctionItemCategories.CategoryOf(grenade));
            Assert.AreEqual("Consumables_Medical", AuctionItemCategories.CategoryOf(medpack));
            Assert.AreEqual("Consumables_Medical", AuctionItemCategories.CategoryOf(kit));
            Assert.AreEqual("Consumables_Dyes", AuctionItemCategories.CategoryOf(dye));
            Assert.AreEqual("Consumables_Miscellaneous", AuctionItemCategories.CategoryOf(booster));
            Assert.AreEqual("Consumables_Miscellaneous", AuctionItemCategories.CategoryOf(token), "the Misc inventory category");
            Assert.AreEqual("Crafting_Fabrication_Ammunition", AuctionItemCategories.CategoryOf(ammoRecipe));
            Assert.AreEqual("Crafting_Fabrication_Paint", AuctionItemCategories.CategoryOf(paintRecipe));
            Assert.AreEqual("Crafting_Fabrication_Resource", AuctionItemCategories.CategoryOf(resourceRecipe));
            Assert.AreEqual("Crafting_Fabrication_Consumable", AuctionItemCategories.CategoryOf(medpackRecipe));
            Assert.IsNull(AuctionItemCategories.CategoryOf(mission));

            Assert.IsTrue(AuctionItemCategories.InCategory(grenade, 105), "Consumables");
            Assert.IsTrue(AuctionItemCategories.InCategory(ammoRecipe, 74), "Fabrication");
            Assert.IsTrue(AuctionItemCategories.InCategory(ammoRecipe, 73), "Crafting");
            Assert.IsFalse(AuctionItemCategories.InCategory(cartridges, 108));
        }

        [TestMethod]
        public void AModuleIsUnderTheKindOfItemItGoesInto()
        {
            var armor = Worn("Armor_T2_Hazmat_V01_CMN_Vest_24_to_28", EquipmentData.Torso, skill: 30);
            var rifle = Weapon("Weapon_Avatar_Rifle_Physical_CMN_11_to_15", ToolType.Rifle);
            var disc = Weapon("Tool_Avatar_Healing_Disc_Direct_CMN_05_to_09", ToolType.HealingDisc, skill: 14);

            var armorModule = Thing("ModuleItem_Generic", InventoryCategory.Crafting);
            var weaponModule = Thing("ModuleItem_Generic", InventoryCategory.Crafting);
            var toolModule = Thing("ModuleItem_Generic", InventoryCategory.Crafting);
            var legacy = Thing("Recipe_Modification", InventoryCategory.Crafting, AugmentationType.Recipe);

            ItemModules.Load(new[]
            {
                new ModuleClassEntry { Id = 1, ClassSetId = 212 },
                new ModuleClassEntry { Id = 2, ClassSetId = 1212 },
                new ModuleClassEntry { Id = 3, ClassSetId = 1273 },
                new ModuleClassEntry { Id = 4, ClassSetId = 1274 }
            }, Enumerable.Empty<ModuleEffectEntry>());
            ItemModules.LoadCrafting(new[]
            {
                new ModuleItemEntry { Id = armorModule.ItemTemplateId, ModuleId = 1, Strength = 1 },
                new ModuleItemEntry { Id = weaponModule.ItemTemplateId, ModuleId = 2, Strength = 1 },
                new ModuleItemEntry { Id = toolModule.ItemTemplateId, ModuleId = 3, Strength = 1 },
                new ModuleItemEntry { Id = legacy.ItemTemplateId, ModuleId = 4, Strength = 1 }
            }, new[]
            {
                new ModifiableClassEntry { Id = (uint)armor.Class, ClassSetId = 212 },
                new ModifiableClassEntry { Id = (uint)rifle.Class, ClassSetId = 1212 },
                new ModifiableClassEntry { Id = (uint)disc.Class, ClassSetId = 1273 }
            });

            Assert.AreEqual("Crafting_Modules_Armor", AuctionItemCategories.CategoryOf(armorModule));
            Assert.AreEqual("Crafting_Modules_Weapons", AuctionItemCategories.CategoryOf(weaponModule));
            Assert.AreEqual("Crafting_Modules_Tools", AuctionItemCategories.CategoryOf(toolModule));
            Assert.AreEqual("Crafting_Modules", AuctionItemCategories.CategoryOf(legacy), "a set that takes nothing");
            Assert.IsTrue(AuctionItemCategories.InCategory(weaponModule, 116));
            Assert.IsTrue(AuctionItemCategories.InCategory(legacy, 116));
            Assert.IsFalse(AuctionItemCategories.InCategory(legacy, 117));
        }

        [TestMethod]
        public void AnItemThatAsksForNoLevelIsInEveryRange()
        {
            Assert.IsTrue(AuctionItemCategories.InLevelRange(0, 1, 5), "the client sends an empty box as 1");
            Assert.IsTrue(AuctionItemCategories.InLevelRange(0, 30, 50));
            Assert.IsTrue(AuctionItemCategories.InLevelRange(10, 5, 15));
            Assert.IsTrue(AuctionItemCategories.InLevelRange(15, 5, 15));
            Assert.IsFalse(AuctionItemCategories.InLevelRange(20, 5, 15));
            Assert.IsFalse(AuctionItemCategories.InLevelRange(4, 5, 15));
        }

        private ItemTemplate Weapon(string className, ToolType toolType = ToolType.None, int skill = 0, EntityClasses ammo = 0)
        {
            var entityClass = Class(className, AugmentationType.Item, AugmentationType.Equipable, AugmentationType.Weapon);
            entityClass.EquipableClassInfo = new EquipableClassInfo(EquipmentData.Weapon);
            entityClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { AmmoClassId = (uint)ammo });

            var template = Template(entityClass, InventoryCategory.Equipment);
            template.WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry { ToolType = (uint)toolType });

            if (skill != 0)
                template.EquipableInfo = new EquipableInfo(skill, 1);

            return template;
        }

        private ItemTemplate Worn(string className, EquipmentData slot, int skill = 0)
        {
            var entityClass = Class(className, AugmentationType.Item, AugmentationType.Equipable);
            entityClass.EquipableClassInfo = new EquipableClassInfo(slot);

            var template = Template(entityClass, InventoryCategory.Equipment);

            if (skill != 0)
                template.EquipableInfo = new EquipableInfo(skill, 1);

            return template;
        }

        private ItemTemplate Thing(string className, InventoryCategory category, params AugmentationType[] more)
        {
            var entityClass = Class(className, new[] { AugmentationType.Item }.Concat(more).ToArray());
            return Template(entityClass, category);
        }

        private EntityClass Class(string className, params AugmentationType[] augmentations)
        {
            var classId = _nextClass++;
            var entityClass = new EntityClass(classId, className, 0, 0, augmentations.ToList(), true);

            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId] = entityClass;
            _added.Add((EntityClasses)classId);

            return entityClass;
        }

        private ItemTemplate Template(EntityClass entityClass, InventoryCategory category) =>
            new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = _nextTemplate++, ItemClass = entityClass.ClassId })
            {
                InventoryCategory = category
            };
    }
}
