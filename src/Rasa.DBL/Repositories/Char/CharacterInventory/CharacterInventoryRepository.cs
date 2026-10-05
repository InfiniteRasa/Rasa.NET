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
            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, itemId);
            var entry = new CharacterInventoryEntry(accountId, characterId, inventoryType, slotId, itemId);

            try
            {
                _charContext.CharacterInventoryEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e) when (_charContext.Database.CurrentTransaction == null)
            {
                Logger.WriteLog(LogType.Error, "Error creating item:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public void DeleteInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var entry = query.Where(e => e.AccountId == accountId && e.CharacterId == characterId && e.InventoryType == inventoryType && e.SlotId == slotId).FirstOrDefault();

            // Remove(null) throws; a row that is already gone is not an error here.
            if (entry == null)
                return;

            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, entry.ItemId);
            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        /// <summary>
        /// Deletes the inventory row for one item, wherever it is. An item has exactly one row
        /// (MoveInvItem relies on that too), so this does not depend on the caller knowing the
        /// character id the row was written with, which has not always been the same thing.
        /// </summary>
        public void DeleteInvItemByItemId(uint itemId)
        {
            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, itemId);
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var entry = query.FirstOrDefault(e => e.ItemId == itemId);

            if (entry == null)
                return;

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public void DeleteForCharacter(uint accountId, uint characterId)
        {
            if (characterId == 0)
                throw new ArgumentOutOfRangeException(nameof(characterId),
                    "Shared account inventory is not owned by a character.");

            var inventory = _charContext.CreateTrackingQuery(_charContext.CharacterInventoryEntries)
                .Where(entry => entry.AccountId == accountId && entry.CharacterId == characterId)
                .ToArray();
            var itemIds = inventory.Select(entry => entry.ItemId).ToArray();
            var items = _charContext.CreateTrackingQuery(_charContext.ItemEntries)
                .Where(entry => itemIds.Contains(entry.ItemId));

            // Stage both sets for the same commit as the character deletion.
            _charContext.CharacterInventoryEntries.RemoveRange(inventory);
            _charContext.ItemEntries.RemoveRange(items);
        }

        public CharacterInventoryEntry FindByItemId(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(
                _charContext.CharacterInventoryEntries);
            return query.FirstOrDefault(entry => entry.ItemId == itemId);
        }

        public List<CharacterInventoryEntry> GetItems(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var characterInventoryEntries = query.Where(e => e.AccountId == accountId).ToList();

            return characterInventoryEntries;
        }

        public List<CharacterInventoryEntry> GetByType(ICollection<uint> characterIds, uint inventoryType)
        {
            if (characterIds == null || characterIds.Count == 0)
                return new List<CharacterInventoryEntry>();

            var ids = characterIds.ToList();

            return _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries)
                .Where(e => e.InventoryType == inventoryType && ids.Contains(e.CharacterId))
                .OrderBy(e => e.CharacterId)
                .ThenBy(e => e.SlotId)
                .ToList();
        }

        /// <summary>
        /// Whether the item's inventory row belongs to this account and either to this character
        /// or to the account's home lockbox (character id 0). MoveInvItem finds its row by item id
        /// alone and rewrites the owner, so a slot move for an item the caller does not hold in the
        /// database - one handed to another player, or one whose row is gone - would otherwise
        /// take that row over.
        /// </summary>
        public bool IsHeldBy(uint itemId, uint accountId, uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);

            return query.Any(e => e.ItemId == itemId && e.AccountId == accountId
                                  && (e.CharacterId == characterId || e.CharacterId == 0));
        }

        public void MoveInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, itemId);
            var invItem = _charContext.CreateTrackingQuery(_charContext.CharacterInventoryEntries).FirstOrDefault(e => e.ItemId == itemId);

            if (invItem == null)
            {
                Logger.WriteLog(LogType.Error, $"Item {itemId} has no inventory row; move skipped.");
                return;
            }

            invItem.AccountId = accountId;
            invItem.CharacterId = characterId;
            invItem.SlotId = slotId;
            invItem.InventoryType = inventoryType;
            _charContext.SaveChanges();
        }
    }
}
