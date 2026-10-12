using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// The categories the auction house's browse tab offers. Its search button stays disabled
    /// until one is picked, so every query carries one and the server has to know them all.
    ///
    /// From the client's own generated.client.auctionhouse.categorydata: (code, name, depth,
    /// uielement). The code packs the path a byte a level - the top category in its highest
    /// byte, then the kind, then the slot: Armor 0x02000000, Hazmat 0x02030000, Hazmat Head
    /// 0x02030100. The names are the 2009 server's, not the item classes': the classes say
    /// Helmet, Vest, Gloves and Boots, Mech and Bio, Torqueshell and GrenadeLauncher. Which items
    /// are in a category is worked out from the item's data (Managers.AuctionItemCategories).
    /// </summary>
    public static class AuctionCategory
    {
        public static readonly IReadOnlyDictionary<uint, string> Names = new Dictionary<uint, string>
        {
            [1] = "Weapon",
            [2] = "Weapon_Pistol",
            [3] = "Weapon_Rifle",
            [4] = "Weapon_Shotgun",
            [5] = "Weapon_MachineGun",
            [6] = "Weapon_LeechGun",
            [7] = "Weapon_RPG",
            [8] = "Weapon_RocketLauncher",
            [9] = "Weapon_NetGun",
            [10] = "Weapon_PolarityGun",
            [11] = "Weapon_InjectionGun",
            [12] = "Weapon_PropellantGun",
            [13] = "Weapon_Staff",
            [14] = "Weapon_SniperRifle",
            [15] = "Weapon_Blade",
            [16] = "Armor",
            [17] = "Armor_MotorAssist",
            [18] = "Armor_MotorAssist_Head",
            [21] = "Armor_MotorAssist_Chest",
            [22] = "Armor_MotorAssist_Hands",
            [23] = "Armor_MotorAssist_Legs",
            [24] = "Armor_MotorAssist_Feet",
            [25] = "Armor_Reflective",
            [26] = "Armor_Reflective_Head",
            [29] = "Armor_Reflective_Chest",
            [30] = "Armor_Reflective_Hands",
            [31] = "Armor_Reflective_Legs",
            [32] = "Armor_Reflective_Feet",
            [33] = "Armor_Hazmat",
            [34] = "Armor_Hazmat_Head",
            [37] = "Armor_Hazmat_Chest",
            [38] = "Armor_Hazmat_Hands",
            [39] = "Armor_Hazmat_Legs",
            [40] = "Armor_Hazmat_Feet",
            [41] = "Armor_Graviton",
            [42] = "Armor_Graviton_Head",
            [43] = "Armor_Graviton_UpperFace",
            [44] = "Armor_Graviton_LowerFace",
            [45] = "Armor_Graviton_Chest",
            [46] = "Armor_Graviton_Hands",
            [47] = "Armor_Graviton_Legs",
            [48] = "Armor_Graviton_Feet",
            [49] = "Armor_Stealth",
            [50] = "Armor_Stealth_Head",
            [53] = "Armor_Stealth_Chest",
            [54] = "Armor_Stealth_Hands",
            [55] = "Armor_Stealth_Legs",
            [56] = "Armor_Stealth_Feet",
            [57] = "Armor_MechSuit",
            [58] = "Armor_MechSuit_Head",
            [61] = "Armor_MechSuit_Chest",
            [62] = "Armor_MechSuit_Hands",
            [63] = "Armor_MechSuit_Legs",
            [64] = "Armor_MechSuit_Feet",
            [65] = "Armor_BioSuit",
            [66] = "Armor_BioSuit_Head",
            [69] = "Armor_BioSuit_Chest",
            [70] = "Armor_BioSuit_Hands",
            [71] = "Armor_BioSuit_Legs",
            [72] = "Armor_BioSuit_Feet",
            [73] = "Crafting",
            [74] = "Crafting_Fabrication",
            [75] = "Crafting_Fabrication_Ammunition",
            [76] = "Crafting_Fabrication_Paint",
            [77] = "Crafting_Fabrication_Consumable",
            [78] = "Crafting_Fabrication_Resource",
            [104] = "Crafting_Salvage",
            [105] = "Consumables",
            [106] = "Weapon_Tool",
            [107] = "Consumables_Ammunition",
            [108] = "Consumables_Resources",
            [109] = "Consumables_Explosives",
            [110] = "Consumables_Medical",
            [111] = "Consumables_Dyes",
            [112] = "Consumables_Miscellaneous",
            [113] = "Armor_Accessory",
            [114] = "Armor_Accessory_UpperFace",
            [115] = "Armor_Accessory_LowerFace",
            [116] = "Crafting_Modules",
            [117] = "Crafting_Modules_Armor",
            [118] = "Crafting_Modules_Tools",
            [119] = "Crafting_Modules_Weapons",
            [120] = "Crafting_Mimeogel",
        };

        /// <summary>The packed code of each category: a byte for each level of its path.</summary>
        public static readonly IReadOnlyDictionary<uint, uint> Codes = new Dictionary<uint, uint>
        {
            [1] = 0x01000000,   // Weapon
            [2] = 0x01010000,   // Weapon_Pistol
            [3] = 0x01020000,   // Weapon_Rifle
            [4] = 0x01030000,   // Weapon_Shotgun
            [5] = 0x01040000,   // Weapon_MachineGun
            [6] = 0x01050000,   // Weapon_LeechGun
            [7] = 0x01060000,   // Weapon_RPG
            [8] = 0x01070000,   // Weapon_RocketLauncher
            [9] = 0x01080000,   // Weapon_NetGun
            [10] = 0x01090000,  // Weapon_PolarityGun
            [11] = 0x010A0000,  // Weapon_InjectionGun
            [12] = 0x010B0000,  // Weapon_PropellantGun
            [13] = 0x010C0000,  // Weapon_Staff
            [14] = 0x010D0000,  // Weapon_SniperRifle
            [15] = 0x010E0000,  // Weapon_Blade
            [16] = 0x02000000,  // Armor
            [17] = 0x02010000,  // Armor_MotorAssist
            [18] = 0x02010100,  // Armor_MotorAssist_Head
            [21] = 0x02010400,  // Armor_MotorAssist_Chest
            [22] = 0x02010500,  // Armor_MotorAssist_Hands
            [23] = 0x02010600,  // Armor_MotorAssist_Legs
            [24] = 0x02010700,  // Armor_MotorAssist_Feet
            [25] = 0x02020000,  // Armor_Reflective
            [26] = 0x02020100,  // Armor_Reflective_Head
            [29] = 0x02020400,  // Armor_Reflective_Chest
            [30] = 0x02020500,  // Armor_Reflective_Hands
            [31] = 0x02020600,  // Armor_Reflective_Legs
            [32] = 0x02020700,  // Armor_Reflective_Feet
            [33] = 0x02030000,  // Armor_Hazmat
            [34] = 0x02030100,  // Armor_Hazmat_Head
            [37] = 0x02030400,  // Armor_Hazmat_Chest
            [38] = 0x02030500,  // Armor_Hazmat_Hands
            [39] = 0x02030600,  // Armor_Hazmat_Legs
            [40] = 0x02030700,  // Armor_Hazmat_Feet
            [41] = 0x02040000,  // Armor_Graviton
            [42] = 0x02040100,  // Armor_Graviton_Head
            [43] = 0x02040200,  // Armor_Graviton_UpperFace
            [44] = 0x02040300,  // Armor_Graviton_LowerFace
            [45] = 0x02040400,  // Armor_Graviton_Chest
            [46] = 0x02040500,  // Armor_Graviton_Hands
            [47] = 0x02040600,  // Armor_Graviton_Legs
            [48] = 0x02040700,  // Armor_Graviton_Feet
            [49] = 0x02050000,  // Armor_Stealth
            [50] = 0x02050100,  // Armor_Stealth_Head
            [53] = 0x02050400,  // Armor_Stealth_Chest
            [54] = 0x02050500,  // Armor_Stealth_Hands
            [55] = 0x02050600,  // Armor_Stealth_Legs
            [56] = 0x02050700,  // Armor_Stealth_Feet
            [57] = 0x02060000,  // Armor_MechSuit
            [58] = 0x02060100,  // Armor_MechSuit_Head
            [61] = 0x02060400,  // Armor_MechSuit_Chest
            [62] = 0x02060500,  // Armor_MechSuit_Hands
            [63] = 0x02060600,  // Armor_MechSuit_Legs
            [64] = 0x02060700,  // Armor_MechSuit_Feet
            [65] = 0x02070000,  // Armor_BioSuit
            [66] = 0x02070100,  // Armor_BioSuit_Head
            [69] = 0x02070400,  // Armor_BioSuit_Chest
            [70] = 0x02070500,  // Armor_BioSuit_Hands
            [71] = 0x02070600,  // Armor_BioSuit_Legs
            [72] = 0x02070700,  // Armor_BioSuit_Feet
            [73] = 0x03000000,  // Crafting
            [74] = 0x03010000,  // Crafting_Fabrication
            [75] = 0x03010100,  // Crafting_Fabrication_Ammunition
            [76] = 0x03010200,  // Crafting_Fabrication_Paint
            [77] = 0x03010300,  // Crafting_Fabrication_Consumable
            [78] = 0x03010400,  // Crafting_Fabrication_Resource
            [104] = 0x03040000, // Crafting_Salvage
            [105] = 0x04000000, // Consumables
            [106] = 0x010F0000, // Weapon_Tool
            [107] = 0x04010000, // Consumables_Ammunition
            [108] = 0x04020000, // Consumables_Resources
            [109] = 0x04030000, // Consumables_Explosives
            [110] = 0x04040000, // Consumables_Medical
            [111] = 0x04050000, // Consumables_Dyes
            [112] = 0x04060000, // Consumables_Miscellaneous
            [113] = 0x02080000, // Armor_Accessory
            [114] = 0x02080100, // Armor_Accessory_UpperFace
            [115] = 0x02080200, // Armor_Accessory_LowerFace
            [116] = 0x03050000, // Crafting_Modules
            [117] = 0x03050100, // Crafting_Modules_Armor
            [118] = 0x03050200, // Crafting_Modules_Tools
            [119] = 0x03050300, // Crafting_Modules_Weapons
            [120] = 0x03060000, // Crafting_Mimeogel
        };
    }
}
