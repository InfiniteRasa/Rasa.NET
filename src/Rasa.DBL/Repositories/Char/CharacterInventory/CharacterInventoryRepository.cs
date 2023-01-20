using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterInventory
{
    using Context.Char;
    using Structures.Char;

    public class CharacterInventoryRepository : ICharacterInventoryRepository
    {
        private readonly CharContext _charContext;

        public CharacterInventoryRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            var entry = new CharacterInventoryEntry(accountId, characterId, inventoryType, slotId, itemId);

            try
            {
                _charContext.CharacterInventoryEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating item:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public void DeleteInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var entry = query.Where(e => e.AccountId == accountId && e.CharacterId == characterId && e.InventoryType == inventoryType && e.SlotId == slotId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public List<CharacterInventoryEntry> GetItems(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var characterInventoryEntries = query.Where(e => e.AccountId == accountId).ToList();

            return characterInventoryEntries;
        }

        public void MoveInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var invItem = query.FirstOrDefault(e => e.ItemId == itemId);

            invItem.AccountId = accountId;
            invItem.CharacterId = characterId;
            invItem.SlotId = slotId;
            invItem.InventoryType = inventoryType;

            _charContext.CharacterInventoryEntries.Update(invItem);
            _charContext.SaveChanges();
        }
    }
}
