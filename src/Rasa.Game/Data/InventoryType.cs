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

    public enum InventoryOffset
    {
        Player              = 0,
        CategoryConsumable  = 50,
        Equiped             = 250,
        WeaponDrawer        = 272
    }
}
