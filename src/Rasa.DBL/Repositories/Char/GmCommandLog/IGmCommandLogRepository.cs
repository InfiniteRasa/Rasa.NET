using System.Collections.Generic;

namespace Rasa.Repositories.Char.GmCommandLog
{
    using Structures.Char;

    /// <summary>The game master audit log: gm_command_log. Every call saves at once.</summary>
    public interface IGmCommandLogRepository
    {
        /// <summary>Adds a row. Returns its id.</summary>
        uint Add(GmCommandLogEntry entry);

        /// <summary>Changes the result of a row; a row that is not there is left alone.</summary>
        void SetResult(uint id, GmCommandResult result);

        /// <summary>The latest rows, newest first.</summary>
        List<GmCommandLogEntry> GetRecent(int count);

        /// <summary>The latest rows of one account, newest first.</summary>
        List<GmCommandLogEntry> GetByAccount(uint accountId, int count);
    }
}
