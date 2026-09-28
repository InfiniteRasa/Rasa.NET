using System.Collections.Generic;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemRepository
    {
        uint CreateItem(IItemChange item);
        void DeleteItem(uint itemId);
        ItemEntry GetItem(uint itemId);
        IReadOnlyList<ItemEntry> GetItems(IReadOnlyCollection<uint> itemIds);
        void UpdateAmmo(IItemChange item);
        void UpdateCurrentHitPoints(IItemChange item);
        void UpdateItemStackSize(IItemChange item);
    }
}
