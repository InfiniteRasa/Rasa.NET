using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterInventory
{
    using Structures.Char;
    public interface ICharacterInventoryRepository
    {
        void AddInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
        void DeleteInvItem(uint accountId, uint characteId, uint inventoryType, uint slotIndex);
        List<CharacterInventoryEntry> GetItems(uint accountId);
        void MoveInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
    }
}
