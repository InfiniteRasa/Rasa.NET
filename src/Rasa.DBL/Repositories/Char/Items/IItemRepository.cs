using System.Collections.Generic;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemRepository
    {
        uint CreateItem(IItemChange item);
        void DeleteItem(uint itemId);
        void DeleteItems(IEnumerable<uint> itemIds);
        ItemEntry GetItem(uint itemId);
        IReadOnlyList<ItemEntry> GetItems(IReadOnlyCollection<uint> itemIds);
        void UpdateAmmo(IItemChange item);
        void UpdateBoundCharacter(IItemChange item);
        void UpdateColor(IItemChange item);
        void UpdateCurrentHitPoints(IItemChange item);
        void UpdateItemStackSize(IItemChange item);
    }
}
