using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CraftingJob
{
    using Context.Char;
    using Structures.Char;

    public class CraftingJobRepository : ICraftingJobRepository
    {
        private readonly CharContext _charContext;

        public CraftingJobRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint Add(CraftingJobEntry entry)
        {
            if (entry == null)
                return 0;

            var row = new CraftingJobEntry
            {
                CharacterId = entry.CharacterId,
                KraftwerksId = entry.KraftwerksId,
                ResultClassId = entry.ResultClassId,
                ResultTemplateId = entry.ResultTemplateId,
                Count = entry.Count,
                CraftingPage = entry.CraftingPage,
                QualityId = entry.QualityId,
                FinishAt = entry.FinishAt
            };

            try
            {
                _charContext.CraftingJobEntries.Add(row);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error adding a crafting job for character {entry.CharacterId} at kraftwerks {entry.KraftwerksId}:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }

            entry.Id = row.Id;

            return row.Id;
        }

        public List<CraftingJobEntry> GetAll() =>
            _charContext.CreateNoTrackingQuery(_charContext.CraftingJobEntries)
                .OrderBy(e => e.Id)
                .ToList();

        public bool UpdateCount(uint id, uint count)
        {
            var entry = _charContext.CreateTrackingQuery(_charContext.CraftingJobEntries).FirstOrDefault(e => e.Id == id);

            if (entry == null)
                return false;

            entry.Count = count;
            _charContext.SaveChanges();
            return true;
        }

        public bool Delete(uint id)
        {
            var entry = _charContext.CreateTrackingQuery(_charContext.CraftingJobEntries).FirstOrDefault(e => e.Id == id);

            if (entry == null)
                return false;

            _charContext.Remove(entry);
            _charContext.SaveChanges();
            return true;
        }

        public int DeleteByKraftwerks(uint kraftwerksId)
        {
            var entries = _charContext.CreateTrackingQuery(_charContext.CraftingJobEntries)
                .Where(e => e.KraftwerksId == kraftwerksId)
                .ToList();

            if (entries.Count == 0)
                return 0;

            _charContext.CraftingJobEntries.RemoveRange(entries);
            _charContext.SaveChanges();
            return entries.Count;
        }

        public int DeleteByCharacter(uint characterId)
        {
            var entries = _charContext.CreateTrackingQuery(_charContext.CraftingJobEntries)
                .Where(e => e.CharacterId == characterId)
                .ToList();

            _charContext.CraftingJobEntries.RemoveRange(entries);

            return entries.Count;
        }
    }
}
