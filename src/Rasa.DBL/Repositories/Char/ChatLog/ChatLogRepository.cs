using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ChatLog
{
    using Context.Char;
    using Structures.Char;

    public class ChatLogRepository : IChatLogRepository
    {
        private readonly CharContext _charContext;

        public ChatLogRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint Add(ChatLogEntry entry)
        {
            var row = new ChatLogEntry
            {
                CreatedAt = entry.CreatedAt,
                Kind = entry.Kind,
                Result = entry.Result,
                AccountId = entry.AccountId,
                AccountLevel = entry.AccountLevel,
                CharacterId = entry.CharacterId,
                Name = entry.Name ?? "",
                FamilyName = entry.FamilyName ?? "",
                MapContextId = entry.MapContextId,
                InstanceId = entry.InstanceId,
                CoordX = entry.CoordX,
                CoordY = entry.CoordY,
                CoordZ = entry.CoordZ,
                GroupId = entry.GroupId,
                TargetAccountId = entry.TargetAccountId,
                TargetCharacterId = entry.TargetCharacterId,
                Target = entry.Target ?? "",
                HeardBy = entry.HeardBy,
                Text = entry.Text ?? ""
            };

            _charContext.ChatLogEntries.Add(row);
            _charContext.SaveChanges();

            return row.Id;
        }

        public List<ChatLogEntry> GetRecent(int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.ChatLogEntries)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();

        public List<ChatLogEntry> GetByAccount(uint accountId, int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.ChatLogEntries)
                .Where(e => e.AccountId == accountId || e.TargetAccountId == accountId)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();
    }
}
