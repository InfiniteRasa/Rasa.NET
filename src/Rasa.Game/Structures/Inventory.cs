using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class Inventory
    {
        // 0-49 Equipment, 50-99 Consumables, 100-149 Crafting, 150-199 Mission, 200-249 Misc
        public List<ulong> PersonalInventory = new List<ulong>(); // change index
        public List<ulong> HomeInventory = new List<ulong>();   // LockBox
        public List<ulong> ClanInventory = new List<ulong>();   // ClanLockBox
        // 250 unknown, 251 helmet, 252 boots, 253 gloves, 254 unknown, 255 unknown
        // 256 unknown, 257 unknown, 258 unknown, 259 unknown, 260 unknown
        // 261 unknown, 262 unknown, 263 unknown, 264 unknown, 265 vest
        // 266 legs, 267 unknown, 268 unknown, 269 eyeweare, 270 unknown
        // 271 mask    (there is maybe more)
        public List<ulong> EquippedInventory = new List<ulong>();
        // 272 weapondrawer1, 273 weapondrawer2, 274 weapondrawer3, 275 weapondrawer4, 276 weapondrawer5
        // 277 weapondrawer1ammo, 278 weapondrawer2ammo, 279 weapondrawer3ammo, 280 weapondrawer4ammo, 281 weapondrawer5ammo
        public List<ulong> WeaponDrawer = new List<ulong>();

        /// <summary>
        /// Items sold to a vendor this session, oldest first, still registered with the
        /// EntityManager and still rows in the items table so they can be bought back as they
        /// were. This is the only list a buyback is honoured from. Capped at MaxBuybackItems:
        /// the oldest is destroyed to make room.
        /// </summary>
        public List<ulong> BuybackItems = new List<ulong>();

        public const int MaxBuybackItems = 10;

        /// <summary>
        /// Items this character has listed at an auction house, in the order they were listed.
        /// Each is still a row in items and in character_inventory - type AuctionInventory - so
        /// the list is rebuilt from the database at login, and the index into it is the slot id
        /// that row carries.
        /// </summary>
        public List<ulong> AuctionItems = new List<ulong>();

        /// <summary>The client refuses to list a thirty-first item; the server agrees.</summary>
        public const int MaxAuctionItems = 30;

        /// <summary>
        /// Items waiting to be collected at an auction house - bought, or returned when an
        /// auction ran out. Rows of type InboxInventory, so they survive a restart and reach a
        /// player who was offline when the item arrived.
        /// </summary>
        public List<ulong> InboxItems = new List<ulong>();

        /// <summary>shared.gameconstants.MAX_INBOX_ITEMS; the client refuses a thirty-first.</summary>
        public const int MaxInboxItems = 30;

        /// <summary>
        /// The item in the wager slot of the prestige window, 0 for none: a character_inventory
        /// row of type WagerInventory, slot 0 (InventoryManager.Wager). Whether it is locked there
        /// is the character's (Manifestation.WagerLocked).
        /// </summary>
        public ulong WagerItem;

        /// <summary>
        /// Empties the clan lockbox list to exactly <see cref="ClanLockboxTab.TotalSlots"/> empty
        /// slots, however many it held before - none at all for a character still on the loading
        /// screen of their login. Every clan lockbox handler indexes the list by slot, and the
        /// InventoryCreate and InventoryReload packets send it whole.
        /// </summary>
        public void ResetClanInventory()
        {
            ClanInventory.Clear();

            for (var i = 0; i < ClanLockboxTab.TotalSlots; i++)
                ClanInventory.Add(0);
        }
    }
}
