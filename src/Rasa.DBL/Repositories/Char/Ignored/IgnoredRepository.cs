using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Repositories.Char.Ignored
{
    using Context.Char;
    using Structures.Char;

    public class IgnoredRepository : IIgnoredRepository
    {
        private readonly CharContext _charContext;

        public IgnoredRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddIgnored(uint accountId, uint ignoredAccountId)
        {
            var entry = new IgnoredEntry(accountId, ignoredAccountId);

            try
            {
                _charContext.IgnoredEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding Ignored:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public List<uint> GetIgnored(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.IgnoredEntries);
            var entries = query.Where(e => e.AccountId == accountId).Select(e => e.IgnoredAccountId).ToList();

            return entries;
        }

        public void RemoveIgnored(uint accountId, uint ignoredAccountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.IgnoredEntries);
            var entry = query.Where(e => e.AccountId == accountId && e.IgnoredAccountId == ignoredAccountId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }
    }
}
