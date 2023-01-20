using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ClanInventory
{
    using Context.Char;
    using Structures.Char;
    public class ClanInventoryRepository : IClanInventoryRepository
    {
        private readonly CharContext _charContext;

        public ClanInventoryRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddInvItem(uint clanId, uint slotId, uint itemId)
        {
            var entry = new ClanInventoryEntry(clanId, slotId, itemId);

            try
            {
                _charContext.ClanInventoryEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding item to clan inventory:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public void DeleteInvItem(uint clanId, uint slotId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanInventoryEntries);
            var entry = query.Where(e => e.ClanId == clanId && e.SlotId == slotId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public List<ClanInventoryEntry> GetItems(uint clanId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanInventoryEntries);
            var entries = query.Where(e => e.ClanId == clanId).ToList();

            return entries;
        }

        public void MoveInvItem(uint clanId, uint slotId, uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanInventoryEntries);
            var entry = query.FirstOrDefault(e => e.ClanId == clanId && e.ItemId == itemId);

            entry.SlotId = slotId;

            _charContext.ClanInventoryEntries.Update(entry);
            _charContext.SaveChanges();
        }
    }
}
