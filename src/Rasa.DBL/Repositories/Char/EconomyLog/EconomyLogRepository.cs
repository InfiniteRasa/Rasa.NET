using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.EconomyLog
{
    using Context.Char;
    using Structures.Char;

    public class EconomyLogRepository : IEconomyLogRepository
    {
        private readonly CharContext _charContext;

        public EconomyLogRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public int Add(IReadOnlyCollection<EconomyLogEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return 0;

            foreach (var entry in entries)
                _charContext.EconomyLogEntries.Add(new EconomyLogEntry
                {
                    CreatedAt = entry.CreatedAt,
                    TransferId = entry.TransferId ?? "",
                    Kind = entry.Kind,
                    ReferenceId = entry.ReferenceId,
                    CharacterId = entry.CharacterId,
                    AccountId = entry.AccountId,
                    OtherCharacterId = entry.OtherCharacterId,
                    ItemId = entry.ItemId,
                    ItemTemplateId = entry.ItemTemplateId,
                    Quantity = entry.Quantity,
                    Currency = entry.Currency,
                    Amount = entry.Amount,
                    Balance = entry.Balance,
                    MapContextId = entry.MapContextId
                });

            _charContext.SaveChanges();

            return entries.Count;
        }

        public List<EconomyLogEntry> GetByCharacter(uint characterId, int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.EconomyLogEntries)
                .Where(e => e.CharacterId == characterId)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();

        public List<EconomyLogEntry> GetByItem(uint itemId) =>
            _charContext.CreateNoTrackingQuery(_charContext.EconomyLogEntries)
                .Where(e => e.ItemId == itemId)
                .OrderBy(e => e.Id)
                .ToList();
    }
}
