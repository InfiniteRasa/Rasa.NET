using System.Collections.Generic;

namespace Rasa.Repositories.Char.ChatLog
{
    using Structures.Char;

    /// <summary>The chat log: chat_log. Every call saves at once.</summary>
    public interface IChatLogRepository
    {
        /// <summary>Adds a row. Returns its id.</summary>
        uint Add(ChatLogEntry entry);

        /// <summary>The latest rows, newest first.</summary>
        List<ChatLogEntry> GetRecent(int count);

        /// <summary>The latest rows an account said, or was whispered, newest first.</summary>
        List<ChatLogEntry> GetByAccount(uint accountId, int count);
    }
}
