namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Which item templates are mission items, and what that makes them (Flag_mission_items).
    ///
    /// The client has no flag for it: an item is a mission item when the server says its
    /// quality is MISSION (generated.client.quality 1), which puts "Mission Item" on its tooltip,
    /// gives its name the mission-drop colour and its loot the mission sparkle. The templates are
    /// the ones filed under the Mission inventory tab (inventory_category 4, from the class names
    /// - Mis*, *_Mission_*, the zone items such as Cuthah_Keycard_Red_Warrish) and the mission
    /// tools and keycards the names put in other tabs (ExtraClasses). Not the Treeback mission
    /// rewards (MisTreeback*Reward), which are armour to keep, nor Mis_Flashpoint_Shield_Creature_*,
    /// a creature's shield.
    ///
    /// A mission item is Bound on Character, Not Tradable, Not Sellable and kept out of the
    /// footlocker and clan lockbox; the tooltip shows each, and trade, vendors and both lockboxes
    /// refuse it with the client's own messages.
    /// </summary>
    public static class MissionItems
    {
        /// <summary>generated.client.quality: MISSION 1, NORMAL 2.</summary>
        public const int MissionQuality = 1;
        public const int NormalQuality = 2;

        /// <summary>InventoryCategory.Mission.</summary>
        public const int MissionCategory = 4;

        /// <summary>Mission items the class name files elsewhere: keycards under Crafting, tools under Equipment.</summary>
        public static readonly string[] ExtraClasses =
        {
            "MisACTItemKeycard2", "MisACTItemKeycard3", "MisACTItemKeycard4",
            "MisMarshesLogosAccessCard1", "MisMarshesLogosAccessCard2", "MisMarshesLogosAccessCard3",
            "Mis_Palisades_ToolHealingDisc", "Mis_Plateau_ItemMindControlDeviceExtractor", "Mis_Plateau_Tissue_Extractor",
            "Mis_Marshes_ItemStriderController",
        };
    }
}
