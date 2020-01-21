using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterInventoryTable
    {
        public static void AddInvItem(uint accountId, uint characterSlot, int inventoryType, uint slotId, uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterInventory.Add(new CharacterInventoryEntry
                {
                    AccountId = accountId,
                    CharacterSlot = characterSlot,
                    InventoryType = inventoryType,
                    SlotId = slotId,
                    ItemId = itemId
                });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void DeleteInvItem(uint accountId, uint characterSlot, int inventoryType, uint slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterInventory = GameDatabaseAccess.CharConnection.CharacterInventory.First(charInv =>
                    charInv.AccountId == accountId
                    && charInv.CharacterSlot == characterSlot
                    && charInv.InventoryType == inventoryType
                    && charInv.SlotId == slotId);
                GameDatabaseAccess.CharConnection.Remove(characterInventory);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static List<CharacterInventoryEntry> GetItems(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.CharacterInventory.Where(charInv => charInv.AccountId == accountId)
                    .ToList();
            }
        }

        public static void MoveInvItem(uint accountId, uint characterSlot, int inventoryType, uint slotId, uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterInventory = GameDatabaseAccess.CharConnection.CharacterInventory.First(charInv =>
                    charInv.AccountId == accountId
                    && charInv.CharacterSlot == characterSlot
                    && charInv.ItemId == itemId
                    );
                characterInventory.InventoryType = inventoryType;
                characterInventory.SlotId = slotId;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
