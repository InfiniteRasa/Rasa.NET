using System.Collections.Generic;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemRepository
    {
        uint CreateItem(IItemChange item);

        /// <summary>Several new item rows in one save; their ids, in the order given.</summary>
        IReadOnlyList<uint> CreateItems(IReadOnlyList<IItemChange> items);

        /// <summary>
        /// These item rows and any inventory rows holding them, in one save: DeleteInvItemByItemId
        /// and DeleteItem for each, with the same refusal of an item a mission owns.
        /// </summary>
        void DeleteItemsWithInventory(IReadOnlyCollection<uint> itemIds);
        void DeleteItem(uint itemId);
        void DeleteItems(IEnumerable<uint> itemIds);
        ItemEntry GetItem(uint itemId);
        IReadOnlyList<ItemEntry> GetItems(IReadOnlyCollection<uint> itemIds);
        void UpdateAmmo(IItemChange item);
        void UpdateBoundCharacter(IItemChange item);
        void UpdateColor(IItemChange item);
        void UpdateCrafter(IItemChange item);
        void UpdateCurrentHitPoints(IItemChange item);
        void UpdateItemStackSize(IItemChange item);
        void UpdateModules(IItemChange item);
    }
}
