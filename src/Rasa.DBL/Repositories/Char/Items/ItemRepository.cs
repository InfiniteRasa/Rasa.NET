using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

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
                throw;
            }
        }

        public void DeleteItem(uint itemId)
        {
            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, itemId);
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == itemId).FirstOrDefault();

            if (entry == null)
                return;

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        /// <summary>
        /// Writes one column of one item's row, without reading the row first.
        ///
        /// These three are the hot writes - a round of ammo leaves the clip on every shot, and
        /// the weapon's own refire is all that paces them - and each of them used to fetch the
        /// row with a tracking query and then save it: two round trips for one column, on the
        /// world loop. A stub attached by its key with that one column marked modified sends the
        /// UPDATE by itself.
        ///
        /// The write stays immediate rather than being gathered up and flushed later: a map
        /// change destroys the player's item entities and reads them back from these rows, and an
        /// item can change hands between one shot and the next - traded, sold, banked, listed -
        /// so a clip count held in memory would come back with the rounds already fired still in
        /// it. What this takes away is the read, not the write.
        ///
        /// A row that is not there updates nothing, which EF reports as a concurrency failure:
        /// that is the missing row the tracking query used to find, logged the way it was.
        /// </summary>
        private void UpdateColumn(IItemChange item, string column, Action<ItemEntry> write)
        {
            // Already tracked here - Attach refuses a second instance of the same key - so it is
            // written where it is, with everything else it carries left alone.
            var tracked = _charContext.ItemEntries.Local.FirstOrDefault(e => e.ItemId == item.Id);

            if (tracked != null)
            {
                write(tracked);
                _charContext.SaveChanges();
                return;
            }

            var entry = new ItemEntry { ItemId = item.Id };

            write(entry);
            _charContext.Attach(entry);
            _charContext.Entry(entry).Property(column).IsModified = true;

            try
            {
                _charContext.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                Logger.WriteLog(LogType.Error, $"Item {item.Id} does not exist; update skipped.");
            }
            finally
            {
                // The stub is every other column at its default, so it does not stay behind to be
                // saved by something else, or to stand in the way of the real row being tracked.
                _charContext.Entry(entry).State = EntityState.Detached;
            }
        }

        public ItemEntry GetItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var item = query.FirstOrDefault(e => e.ItemId == itemId);

            return item;

        }

        public IReadOnlyList<ItemEntry> GetItems(IReadOnlyCollection<uint> itemIds)
        {
            var ids = itemIds.Distinct().ToArray();
            return ids.Length == 0 ? Array.Empty<ItemEntry>() :
                _charContext.CreateNoTrackingQuery(_charContext.ItemEntries)
                    .Where(entry => ids.Contains(entry.ItemId)).ToArray();
        }

        public void UpdateAmmo(IItemChange item)
        {
            UpdateColumn(item, nameof(ItemEntry.AmmoCount), entry => entry.AmmoCount = item.CurrentAmmo);
        }

        public void UpdateCurrentHitPoints(IItemChange item)
        {
            UpdateColumn(item, nameof(ItemEntry.CurrentHitPoints), entry => entry.CurrentHitPoints = item.CurrentHitPoints);
        }

        public void UpdateItemStackSize(IItemChange item)
        {
            CharacterMissionItem.MissionItemMutationGuard.RequireUnbound(_charContext, item.Id);
            UpdateColumn(item, nameof(ItemEntry.StackSize), entry => entry.StackSize = item.StackSize);
        }
    }
}
