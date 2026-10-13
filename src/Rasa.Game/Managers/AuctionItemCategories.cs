using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Which of the auction house's browse categories an item is in (AuctionCategory).
    ///
    /// The client sends a category and nothing else to say what is wanted, and carries no list
    /// of which items are in which: the assignment was the 2009 server's. The categories used to
    /// be matched against words in the item class name, which only works where the class says
    /// what the category does - 59 of the 83 found nothing, the slots (classes say Helmet, Vest,
    /// Gloves, Boots), Mech and Bio armor, Torqueshell rifles, grenade launchers, tools,
    /// accessories and every consumable and crafting group among them. So an item's place is
    /// worked out here from its data, the most telling field first:
    ///
    ///  - a weapon or tool (the Weapon augmentation, or the weapon equipment slot): a tool when
    ///    its tool type is one of the six tools (healing disc, armor augmentation, cipher, tissue
    ///    extractor, salvage, field repair), it asks for the Tools skill, or its class is a Tool_
    ///    one; otherwise the kind its class name says (<see cref="WeaponKinds"/>), and Weapon alone
    ///    when it says none;
    ///  - armor and clothing (the other equipment slots): the armor its skill requirement is for
    ///    (<see cref="ArmorSkills"/> - every armor template asks for one), in the slot it is worn
    ///    in (<see cref="ArmorSlots"/>). Equipment that asks for no armor skill - clothing,
    ///    rewards, face pieces - is Accessory, with Upper Face and Lower Face for those slots.
    ///    Hair, faces and the creature shields are in no category;
    ///  - Mimeogel is Mimeomech, and junk loot (Loot_Junk_) is Salvage;
    ///  - the fabrication ingredients are Consumables, Resources: the raw materials (Metal1 and
    ///    the like), Component_, Ingredient and Resource_ classes, and the Ammo_ classes no weapon
    ///    fires - micromech, scrap, nucleotides;
    ///  - a module (module_item) is Modules, under Armor, Weapon or Tool by the kind of item its
    ///    class set takes; one whose set takes nothing is Modules alone;
    ///  - a recipe is Fabrication, by what it makes: ammunition, armor paint, resources, and
    ///    Consumables for the rest (medpacks, grenades, the detonator);
    ///  - ammunition is a class some weapon fires (its AmmoClassId);
    ///  - the rest of the consumables: Explosives (grenades, bombs), Medical (medpacks and the
    ///    restore and Rez Sickness kits), Armor Paint (armor dyes and clothing colours) and
    ///    Miscellaneous; items of the Misc inventory category are Miscellaneous too;
    ///  - anything left of the Crafting inventory category is Crafting alone. Mission items are
    ///    in no category.
    ///
    /// A category takes everything under it: Armor everything with Armor's top byte, Hazmat
    /// everything with Hazmat's two.
    /// </summary>
    public static class AuctionItemCategories
    {
        /// <summary>The weapon kinds by the word their class names use, the more particular first: a Torqueshell_Rifle is no Rifle.</summary>
        private static readonly (string Word, string Category)[] WeaponKinds =
        {
            ("Torqueshell", "Weapon_SniperRifle"),
            ("TorqueShellRifle", "Weapon_SniperRifle"),
            ("GrenadeLauncher", "Weapon_RPG"),
            ("RocketLauncher", "Weapon_RocketLauncher"),
            ("MachineGun", "Weapon_MachineGun"),
            ("LeechGun", "Weapon_LeechGun"),
            ("NetGun", "Weapon_NetGun"),
            ("PolarityGun", "Weapon_PolarityGun"),
            ("InjectionGun", "Weapon_InjectionGun"),
            ("PropellantGun", "Weapon_PropellantGun"),
            ("Staff", "Weapon_Staff"),
            ("Blade", "Weapon_Blade"),
            ("Sword", "Weapon_Blade"),
            ("Shotgun", "Weapon_Shotgun"),
            ("Pistol", "Weapon_Pistol"),
            ("Rifle", "Weapon_Rifle")
        };

        /// <summary>The armor skills (the client's skilldata) and the kind of armor each is for.</summary>
        private static readonly IReadOnlyDictionary<int, string> ArmorSkills = new Dictionary<int, string>
        {
            [19] = "Armor_MotorAssist",     // T1_RECRUIT_MOTOR_ASSIST_ARMOR
            [21] = "Armor_Reflective",      // T2_SOLDIER_REFLECTIVE_ARMOR
            [30] = "Armor_Hazmat",          // T2_SPECIALIST_HAZMAT_ARMOR
            [39] = "Armor_Graviton",        // T3_COMMANDO_GRAVITON_ARMOR
            [48] = "Armor_Stealth",         // T3_RANGER_STEALTH_ARMOR
            [57] = "Armor_MechSuit",        // T3_SAPPER_MECH_ARMOR
            [66] = "Armor_BioSuit"          // T3_BIOTECHNICIAN_BIO_ARMOR
        };

        /// <summary>The Tools skill (T2_SPECIALIST_TOOLS).</summary>
        private const int ToolsSkill = (int)SkillId.SpecialistTools;

        /// <summary>The slots, as the categories name them.</summary>
        private static readonly IReadOnlyDictionary<EquipmentData, string> ArmorSlots = new Dictionary<EquipmentData, string>
        {
            [EquipmentData.Helmet] = "Head",
            [EquipmentData.Eyewear] = "UpperFace",
            [EquipmentData.Beard] = "LowerFace",
            [EquipmentData.Mask] = "LowerFace",
            [EquipmentData.Torso] = "Chest",
            [EquipmentData.Gloves] = "Hands",
            [EquipmentData.Legs] = "Legs",
            [EquipmentData.Shoes] = "Feet"
        };

        /// <summary>The slots worn as armor or clothing: hair, faces and the creature shields' slot are not.</summary>
        private static readonly HashSet<EquipmentData> WornSlots = new HashSet<EquipmentData>
        {
            EquipmentData.Helmet, EquipmentData.Torso, EquipmentData.Gloves, EquipmentData.Legs, EquipmentData.Shoes,
            EquipmentData.Eyewear, EquipmentData.Beard, EquipmentData.Mask
        };

        /// <summary>The fabrication raw materials, which are one word and a grade.</summary>
        private static readonly HashSet<string> RawMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Metal1", "Liquid1", "Hide1", "Superconductor1", "Combustible1", "HardMineral1", "SoftFiber1", "Gas1",
            "Glass1", "Dye1", "Solvent1", "Synthetic1", "Adhesive1", "Microbes1"
        };

        private static readonly Dictionary<string, uint> CodeByName =
            AuctionCategory.Names.ToDictionary(entry => entry.Value, entry => AuctionCategory.Codes[entry.Key]);

        private static HashSet<EntityClasses> _ammunition;
        private static int _ammunitionFrom = -1;
        private static readonly object CacheLock = new object();

        /// <summary>Whether the item template is in the category: in it, or in one under it.</summary>
        public static bool InCategory(ItemTemplate template, uint categoryId)
        {
            if (!AuctionCategory.Codes.TryGetValue(categoryId, out var wanted))
                return false;

            var code = CodeOf(template);

            return code != 0 && (code & MaskOf(wanted)) == wanted;
        }

        /// <summary>
        /// Whether an item asking for this level is in the range searched. One that asks for none
        /// is in every range: the client sends an empty level box as 1, so a level-0 item was in
        /// no search at all.
        /// </summary>
        public static bool InLevelRange(int level, uint minLevel, uint maxLevel) =>
            level == 0 || (level >= minLevel && level <= maxLevel);

        /// <summary>The bytes of a code that its category fixes: one for a top category, two for a kind, three for a slot.</summary>
        public static uint MaskOf(uint code) =>
            (code & 0x00FF0000) == 0 ? 0xFF000000u : (code & 0x0000FF00) == 0 ? 0xFFFF0000u : 0xFFFFFF00u;

        /// <summary>The code of the deepest category the item is in; 0 for none.</summary>
        public static uint CodeOf(ItemTemplate template)
        {
            var name = CategoryOf(template);

            return name != null && CodeByName.TryGetValue(name, out var code) ? code : 0;
        }

        /// <summary>The name of the deepest category the item is in (AuctionCategory.Names); null for none.</summary>
        public static string CategoryOf(ItemTemplate template)
        {
            if (template == null || !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(template.Class, out var entityClass) || entityClass == null)
                return null;

            var className = entityClass.ClassName ?? string.Empty;
            var words = className.Split('_');
            var augmentations = entityClass.Augmentations ?? new List<AugmentationType>();
            var slot = entityClass.EquipableClassInfo?.EquipmentSlotId;
            var skill = template.EquipableInfo?.SkillId ?? 0;

            if (augmentations.Contains(AugmentationType.Weapon) || slot == EquipmentData.Weapon)
                return WeaponCategory(template, words, skill);

            if (slot != null)
                return WornCategory(slot.Value, skill);

            if ((uint)template.Class == ItemModules.MimeogelClassId)
                return "Crafting_Mimeogel";

            if (Is(words, 0, "Loot") && Is(words, 1, "Junk"))
                return "Crafting_Salvage";

            var ammunition = IsAmmunition(template.Class);

            if (IsResource(className, words, ammunition))
                return "Consumables_Resources";

            if (ItemModules.IsModuleItem(template.ItemTemplateId, out var classSetId))
                return ModuleCategory(classSetId);

            if (augmentations.Contains(AugmentationType.Recipe))
                return Has(words, "Ammo") ? "Crafting_Fabrication_Ammunition"
                    : Has(words, "ArmorDye") || Has(words, "Dye") ? "Crafting_Fabrication_Paint"
                    : Has(words, "Resource") ? "Crafting_Fabrication_Resource"
                    : "Crafting_Fabrication_Consumable";

            if (ammunition)
                return "Consumables_Ammunition";

            if (template.InventoryCategory == InventoryCategory.Consumable || Is(words, 0, "Consumable") || Is(words, 0, "Modification")
                || augmentations.Contains(AugmentationType.Customization))
                return Has(words, "Grenade") || Has(words, "Bomb") || Has(words, "Detonator") ? "Consumables_Explosives"
                    : Has(words, "Medpack") || Has(words, "Restore") || (Has(words, "Res") && Has(words, "Sickness")) ? "Consumables_Medical"
                    : Has(words, "ArmorDye") || Has(words, "ClothingColor") || Has(words, "Dye") ? "Consumables_Dyes"
                    : "Consumables_Miscellaneous";

            return template.InventoryCategory switch
            {
                InventoryCategory.Misc => "Consumables_Miscellaneous",
                InventoryCategory.Crafting => "Crafting",
                _ => null
            };
        }

        private static string WeaponCategory(ItemTemplate template, string[] words, int skill)
        {
            var toolType = template.WeaponInfo?.ToolType ?? ToolType.None;

            if ((toolType >= ToolType.HealingDisc && toolType <= ToolType.FieldRepair) || skill == ToolsSkill || Is(words, 0, "Tool"))
                return "Weapon_Tool";

            foreach (var (word, category) in WeaponKinds)
                if (Has(words, word))
                    return category;

            return "Weapon";
        }

        private static string WornCategory(EquipmentData slot, int skill)
        {
            if (!WornSlots.Contains(slot))
                return null;

            ArmorSlots.TryGetValue(slot, out var part);

            if (ArmorSkills.TryGetValue(skill, out var armor))
                return part != null && CodeByName.ContainsKey($"{armor}_{part}") ? $"{armor}_{part}" : armor;

            return part != null && CodeByName.ContainsKey($"Armor_Accessory_{part}") ? $"Armor_Accessory_{part}" : "Armor_Accessory";
        }

        private static bool IsResource(string className, string[] words, bool ammunition) =>
            RawMaterials.Contains(className)
            || Is(words, 0, "Component")
            || Is(words, 0, "Resource")
            || className.StartsWith("Ingredient", StringComparison.OrdinalIgnoreCase)
            || (Is(words, 0, "Ammo") && !ammunition);

        /// <summary>Modules, by the kind of item their class set takes: armor, weapons or tools.</summary>
        private static string ModuleCategory(uint classSetId)
        {
            var member = classSetId == 0 ? 0 : ItemModules.MemberOf(classSetId);

            if (member == 0 || !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)member, out var memberClass) || memberClass == null)
                return "Crafting_Modules";

            var memberWords = (memberClass.ClassName ?? string.Empty).Split('_');

            return Is(memberWords, 0, "Tool") ? "Crafting_Modules_Tools"
                : memberClass.Augmentations?.Contains(AugmentationType.Weapon) == true ? "Crafting_Modules_Weapons"
                : "Crafting_Modules_Armor";
        }

        /// <summary>Whether some weapon fires this class. Worked out again whenever the entity classes have changed in number.</summary>
        private static bool IsAmmunition(EntityClasses classId)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            lock (CacheLock)
            {
                if (_ammunition == null || _ammunitionFrom != classes.Count)
                {
                    _ammunition = classes.Values
                        .Where(entityClass => entityClass?.WeaponClassInfo != null && entityClass.WeaponClassInfo.AmmoClassId != 0)
                        .Select(entityClass => entityClass.WeaponClassInfo.AmmoClassId)
                        .ToHashSet();
                    _ammunitionFrom = classes.Count;
                }

                return _ammunition.Contains(classId);
            }
        }

        private static bool Is(string[] words, int index, string word) =>
            index < words.Length && words[index].Equals(word, StringComparison.OrdinalIgnoreCase);

        private static bool Has(string[] words, string word) =>
            words.Any(candidate => candidate.Equals(word, StringComparison.OrdinalIgnoreCase));
    }
}
