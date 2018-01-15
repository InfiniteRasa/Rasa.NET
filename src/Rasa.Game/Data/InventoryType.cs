namespace Rasa.Data
{
    public enum InventoryType
    {
        Personal                = 1,
        HomeInventory           = 2, // (lockbox)
        HiddenInventory         = 3,
        TradeInventory          = 4,
        PlayerVendorInventory   = 5,
        GameContextInventory    = 6,
        OverflowInventory       = 7,
        EquipedInventory        = 8,
        WeaponDrawerInventory   = 9,
        BuyBackInventory        = 10,
        AuctionInventory        = 11,
        InboxInventory          = 12,
        OutboxInventory         = 13,
        WagerInventory          = 14,
        ClanInventory           = 15
    }

    public enum InventoryCategory
    {
        Equipment   = 1,
        Consumable  = 2,
        Crafting    = 3,
        Mission     = 4,
        Misc        = 5
    }

    public enum EquipmentSlots
    {
        Helmet                  = 1,
        Shoes                   = 2,
        Gloves                  = 3,
        Weapon                  = 13,
        Hair                    = 14,
        Torso                   = 15,
        Legs                    = 16,
        Face                    = 17,
        Wing                    = 18,
        EyeWear                 = 19,
        Beard                   = 20,
        Mask                    = 21,
        MechWeapon1             = 25,
        MechWeapon2             = 26,
        MechWeapon3             = 27,
        MechWeapon4             = 28,
        MechWeapon5             = 29,
        MechAbility1            = 30,
        MechAbility2            = 31,
        MechAbility3            = 32,
        MechAbility4            = 33,
        MechAbility5            = 34,
        MechArmorPlating        = 35,
        MechShieldGenerator     = 36,
        MechMobilitySystem      = 37,
        MechTargetingComputer   = 38,
        MechPowerPlant          = 39,
        HandToHand              = 10000001
    }

    public enum InventoryOffset
    {
        Player              = 0,
        CategoryConsumable  = 50,
        Equiped             = 250,
        WeaponDrawer        = 272
    }
}
