using System;
using System.Linq;

namespace Rasa.Repositories.Char.Items
{
    using Context.Char;
    using Structures.Char;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

    public class ItemRepository : IItemRepository
    {
        private readonly CharContext _charContext;

        public ItemRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint CreateItem(IItemChange item)
        {
            var entry = new ItemEntry(item);

            try
            {
                _charContext.ItemEntries.Add(entry);
                _charContext.SaveChanges();
                return entry.ItemId;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating item:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }
        }

        public void DeleteItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == itemId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public ItemEntry GetItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var item = query.FirstOrDefault(e => e.ItemId == itemId);

            return item;

        }

        public void UpdateAmmo(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.AmmoCount = item.CurrentAmmo;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCurrentHitPoints(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.CurrentHitPoints = item.CurrentHitPoints;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateItemStackSize(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.StackSize = item.StackSize;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }
    }
}
