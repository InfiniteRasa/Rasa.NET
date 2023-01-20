using System.Collections.Generic;

namespace Rasa.Repositories.Char.ClanInventory
{
    using Structures.Char;
    public interface IClanInventoryRepository
    {
        void AddInvItem(uint clanId, uint slotId, uint itemId);
        void DeleteInvItem(uint clanId, uint slotId);
        List<ClanInventoryEntry> GetItems(uint clanId);
        void MoveInvItem(uint clanId, uint slotId, uint itemId);
    }
}
