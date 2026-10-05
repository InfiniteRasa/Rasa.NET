using System;

namespace Rasa.Managers
{
    using Game;
    using Repositories.UnitOfWork;
    using Structures.Char;

    /// <summary>
    /// The chat log: every line of chat a player sends, kept in the character database's
    /// chat_log table (<see cref="IStore"/>). A row says who sent it - account, its level, the
    /// character - when, where the character stood, what kind of chat it was and to whom, the
    /// line itself, how many others it was sent to and what came of it.
    ///
    /// What is on it (CommunicatorManager): what is said to those nearby, shouted and emoted;
    /// whispers and replies, with whom they were to; squad, clan and clan leaders' chat; and the
    /// numbered channels - General, LFG, a map's General, Trade and Defense, a team's. A line
    /// that was not passed on is on it too, with why: the sender is silenced, a whisper's name is
    /// nobody's or its owner ignores the sender, the sender is not of the squad, clan, rank or
    /// channel, or the line is longer than chat takes (its beginning is kept).
    ///
    /// What is not: a line that begins with a dot is a command and not chat, and the game
    /// master audit log has it (GmAudit); an empty line is nothing said; what a game master
    /// announces is a command too. Voice is not chat the server reads.
    ///
    /// The row is written when the line has been passed on, in the same step: one insert a
    /// line. Nothing waits on the log or fails with it - a store that cannot be written costs
    /// the row, says so in the server log, and the line is said all the same.
    /// </summary>
    public class ChatAudit
    {
        private static ChatAudit _instance;
        private static readonly object InstanceLock = new object();

        public static ChatAudit Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new ChatAudit();

                return _instance;
            }
        }

        /// <summary>Where the log is kept. The call may throw; it is not retried.</summary>
        public interface IStore
        {
            /// <summary>Adds a row. Returns its id.</summary>
            uint Add(ChatLogEntry entry);
        }

        /// <summary>Where the rows go; null keeps none. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>The wall clock rows are dated by, UTC. Replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>From now on rows are kept in <paramref name="store"/>.</summary>
        public void Load(IStore store)
        {
            Store = store;
        }

        /// <summary>
        /// Puts a line of chat on the log: <paramref name="heardBy"/> is how many players other
        /// than the sender it was sent to, <paramref name="groupId"/> the squad, clan or channel
        /// it was said to, <paramref name="target"/> to whom in words, and
        /// <paramref name="whisperedTo"/> a whisper's recipient. Returns the row's id; 0 when
        /// no row was written - no store, no account, nothing said, or the store failed.
        /// </summary>
        public uint Record(Client client, ChatLogKind kind, string text, ChatLogResult result,
            uint heardBy = 0, uint groupId = 0, string target = null, Client whisperedTo = null)
        {
            var store = Store;

            if (store == null || string.IsNullOrEmpty(text))
                return 0;

            try
            {
                var account = client?.AccountEntry;

                if (account == null)
                    return 0;

                var player = client.Player;
                var entry = new ChatLogEntry
                {
                    CreatedAt = UtcNow(),
                    Kind = (byte)kind,
                    Result = (byte)result,
                    AccountId = account.Id,
                    AccountLevel = account.Level,
                    CharacterId = player?.Id ?? 0,
                    Name = Cut(player?.Name, ChatLogEntry.MaxNameLength),
                    FamilyName = Cut(player?.FamilyName ?? account.FamilyName, ChatLogEntry.MaxNameLength),
                    GroupId = groupId,
                    Target = Cut(target, ChatLogEntry.MaxTargetLength),
                    HeardBy = heardBy,
                    Text = Cut(text, ChatLogEntry.MaxTextLength)
                };

                if (player != null)
                {
                    entry.MapContextId = player.MapContextId;
                    entry.InstanceId = player.MapChannel?.InstanceId ?? 0;
                    entry.CoordX = player.Position.X;
                    entry.CoordY = player.Position.Y;
                    entry.CoordZ = player.Position.Z;
                }

                if (whisperedTo != null)
                {
                    entry.TargetAccountId = whisperedTo.AccountEntry?.Id ?? 0;
                    entry.TargetCharacterId = whisperedTo.Player?.Id ?? 0;

                    if (target == null)
                        entry.Target = Cut(whisperedTo.Player?.FamilyName, ChatLogEntry.MaxTargetLength);
                }

                return store.Add(entry);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error,
                    $"Chat log: a line of account {client?.AccountEntry?.Id} could not be recorded: {e.GetBaseException().Message}");

                return 0;
            }
        }

        private static string Cut(string text, int length) =>
            string.IsNullOrEmpty(text) ? "" : text.Length <= length ? text : text.Substring(0, length);

        /// <summary>The live server's store: the character database.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public uint Add(ChatLogEntry entry)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.ChatLogs.Add(entry);
            }
        }
    }
}
