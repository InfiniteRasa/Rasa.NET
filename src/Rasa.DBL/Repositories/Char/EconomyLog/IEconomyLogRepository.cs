using System.Collections.Generic;

namespace Rasa.Repositories.Char.EconomyLog
{
    using Structures.Char;

    /// <summary>The economy log: economy_log. Every call saves at once.</summary>
    public interface IEconomyLogRepository
    {
        /// <summary>Adds the rows of one event together. Returns how many were added.</summary>
        int Add(IReadOnlyCollection<EconomyLogEntry> entries);

        /// <summary>A character's latest rows, newest first.</summary>
        List<EconomyLogEntry> GetByCharacter(uint characterId, int count);

        /// <summary>An item's rows, oldest first: whose it was, in turn.</summary>
        List<EconomyLogEntry> GetByItem(uint itemId);
    }
}
