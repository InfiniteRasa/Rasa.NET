using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ApiLog
{
    using Context.Char;
    using Structures.Char;

    /// <summary>The REST API's log (api_log). Every call saves at once.</summary>
    public interface IApiLogRepository
    {
        /// <summary>Adds a row. Returns its id.</summary>
        uint Add(ApiLogEntry entry);

        /// <summary>The last rows, newest first.</summary>
        List<ApiLogEntry> GetRecent(int count);
    }

    public class ApiLogRepository : IApiLogRepository
    {
        private readonly CharContext _charContext;

        public ApiLogRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint Add(ApiLogEntry entry)
        {
            var row = new ApiLogEntry
            {
                CreatedAt = entry.CreatedAt,
                Address = entry.Address ?? "",
                Method = entry.Method ?? "",
                Path = entry.Path ?? "",
                Query = entry.Query ?? "",
                Status = entry.Status,
                Body = entry.Body ?? "",
                BodyLength = entry.BodyLength,
                Response = entry.Response ?? ""
            };

            _charContext.ApiLogEntries.Add(row);
            _charContext.SaveChanges();

            return row.Id;
        }

        public List<ApiLogEntry> GetRecent(int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.ApiLogEntries)
                .OrderByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();
    }
}
