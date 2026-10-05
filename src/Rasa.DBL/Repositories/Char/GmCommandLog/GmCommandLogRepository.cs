using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.GmCommandLog
{
    using Context.Char;
    using Structures.Char;

    public class GmCommandLogRepository : IGmCommandLogRepository
    {
        private readonly CharContext _charContext;

        public GmCommandLogRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint Add(GmCommandLogEntry entry)
        {
            var row = new GmCommandLogEntry
            {
                CreatedAt = entry.CreatedAt,
                Source = entry.Source,
                Result = entry.Result,
                AccountId = entry.AccountId,
                AccountLevel = entry.AccountLevel,
                RequiredLevel = entry.RequiredLevel,
                CharacterId = entry.CharacterId,
                Name = entry.Name ?? "",
                FamilyName = entry.FamilyName ?? "",
                Address = entry.Address ?? "",
                MapContextId = entry.MapContextId,
                InstanceId = entry.InstanceId,
                CoordX = entry.CoordX,
                CoordY = entry.CoordY,
                CoordZ = entry.CoordZ,
                TargetCharacterId = entry.TargetCharacterId,
                Target = entry.Target ?? "",
                Command = entry.Command ?? "",
                Text = entry.Text ?? ""
            };

            _charContext.GmCommandLogEntries.Add(row);
            _charContext.SaveChanges();

            return row.Id;
        }

        public void SetResult(uint id, GmCommandResult result)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.GmCommandLogEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return;

            row.Result = (byte)result;
            _charContext.SaveChanges();
        }

        public List<GmCommandLogEntry> GetRecent(int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.GmCommandLogEntries)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();

        public List<GmCommandLogEntry> GetByAccount(uint accountId, int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.GmCommandLogEntries)
                .Where(e => e.AccountId == accountId)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();
    }
}
