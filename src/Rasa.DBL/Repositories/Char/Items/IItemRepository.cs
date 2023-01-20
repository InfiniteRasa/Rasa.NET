using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemRepository
    {
        uint CreateItem(IItemChange item);
        void DeleteItem(uint itemId);
        ItemEntry GetItem(uint itemId);
        void UpdateAmmo(IItemChange item);
        void UpdateCurrentHitPoints(IItemChange item);
        void UpdateItemStackSize(IItemChange item);
    }
}
