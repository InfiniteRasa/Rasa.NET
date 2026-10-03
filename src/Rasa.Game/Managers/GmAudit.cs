using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The game master audit log: every command a game master enters, kept in the character
    /// database's gm_command_log table (<see cref="IStore"/>) and said in the server log. A row
    /// says who entered it - account, level, character, the address they are connected from -
    /// when, where their character stood and what they had selected, the command as it was
    /// entered, the level it needs and what came of it.
    ///
    /// What is on it:
    ///  - the dot commands typed in chat (ChatCommandsManager.ProcessCommand);
    ///  - the slash commands the client passes on, and the picks of its GM windows
    ///    (ChatCommandsManager.PrivilegedCommand);
    ///  - the GM requests the client has messages of their own for: /gotomob, /changefirstname,
    ///    /changelastname, the GM mission window's ForceCompleteObjective, RequestLOSReport;
    ///  - what is typed at the game server's console (Server), under account 0.
    ///
    /// Whose commands: an account of any level above Player has everything it enters recorded -
    /// what was run, what it was refused for want of level, and what was no command at all. An
    /// ordinary player has only the refusals recorded - a real command tried without the level
    /// for it - and no more than <see cref="UnprivilegedPerMinute"/> of them a minute an
    /// account: the log is not theirs to fill. A line of theirs that begins with a dot and is
    /// no command is chat that went nowhere, and is not recorded.
    ///
    /// The row is written before the command is run, so a command that brings the server down
    /// is on the log; one that throws has its row marked failed. Nothing waits on the log or
    /// fails with it: a store that cannot be written costs the row, says so in the server log,
    /// and the command is run all the same.
    /// </summary>
    public class GmAudit
    {
        private static GmAudit _instance;
        private static readonly object InstanceLock = new object();

        public static GmAudit Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new GmAudit();

                return _instance;
            }
        }

        /// <summary>The refusals of an ordinary player's account recorded in a minute; the rest are dropped.</summary>
        public const int UnprivilegedPerMinute = 10;

        private const long MinuteMs = 60_000;

        /// <summary>Where the log is kept. Either call may throw; neither is retried.</summary>
        public interface IStore
        {
            /// <summary>Adds a row. Returns its id.</summary>
            uint Add(GmCommandLogEntry entry);

            /// <summary>Changes a row's result.</summary>
            void SetResult(uint id, GmCommandResult result);
        }

        private readonly object _sync = new object();

        /// <summary>The refusals recorded for each ordinary account in its current minute: when the minute began, and how many.</summary>
        private readonly Dictionary<uint, (long Since, int Count)> _refusals = new Dictionary<uint, (long, int)>();

        /// <summary>Where the rows go; null keeps none. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>The wall clock rows are dated by, UTC. Replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>The clock the refusals of ordinary players are counted by, in milliseconds. Replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>From now on rows are kept in <paramref name="store"/>.</summary>
        public void Load(IStore store)
        {
            Store = store;
        }

        /// <summary>
        /// Puts a command a client entered on the log, before it is run or as it is refused:
        /// <paramref name="command"/> is its name as the server knows it,
        /// <paramref name="text"/> the whole line, <paramref name="required"/> the level it
        /// needs. Returns the row's id for <see cref="Failed"/>; 0 when no row was written.
        /// </summary>
        public uint Record(Client client, GmCommandSource source, string command, string text, GmLevel required, GmCommandResult result)
        {
            var account = client?.AccountEntry;

            if (account == null)
                return 0;

            if (account.Level == (byte)GmLevel.Player)
            {
                // Only what an ordinary player was refused, and only so much of it.
                if (result != GmCommandResult.Denied || !Admit(account.Id))
                    return 0;
            }

            var player = client.Player;
            var entry = new GmCommandLogEntry
            {
                CreatedAt = UtcNow(),
                Source = (byte)source,
                Result = (byte)result,
                AccountId = account.Id,
                AccountLevel = account.Level,
                RequiredLevel = (byte)required,
                CharacterId = player?.Id ?? 0,
                Name = Cut(player?.Name, 64),
                FamilyName = Cut(player?.FamilyName, 64),
                Address = AddressOf(client),
                Command = Cut(command, GmCommandLogEntry.MaxCommandLength),
                Text = Cut(text, GmCommandLogEntry.MaxTextLength)
            };

            if (player != null)
            {
                entry.MapContextId = player.MapContextId;
                entry.InstanceId = player.MapChannel?.InstanceId ?? 0;
                entry.CoordX = player.Position.X;
                entry.CoordY = player.Position.Y;
                entry.CoordZ = player.Position.Z;

                try
                {
                    DescribeTarget(player.Target, entry);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"GM audit: the target of account {account.Id} could not be described: {e.Message}");
                }
            }

            // Refusals and unknown commands are said in the log by whoever refused them.
            if (result == GmCommandResult.Executed)
                Logger.WriteLog(LogType.Command,
                    $"GM command: account {account.Id} (level {account.Level}) {entry.FamilyName}: {OneLine(entry.Text)}");

            return Write(entry);
        }

        /// <summary>
        /// Puts a GM request the client has a message of its own for on the log, as it is let
        /// through or refused. Returns the row's id; 0 when no row was written.
        /// </summary>
        public uint Request(Client client, string command, string text, GmLevel required, bool allowed) =>
            Record(client, GmCommandSource.Request, command, text, required, allowed ? GmCommandResult.Executed : GmCommandResult.Denied);

        /// <summary>Puts a command typed at the server's console on the log, before it is run. Returns the row's id; 0 when no row was written.</summary>
        public uint RecordConsole(string command, string text)
        {
            Logger.WriteLog(LogType.Command, $"GM command: console: {OneLine(Cut(text, GmCommandLogEntry.MaxTextLength))}");

            return Write(new GmCommandLogEntry
            {
                CreatedAt = UtcNow(),
                Source = (byte)GmCommandSource.Console,
                Result = (byte)GmCommandResult.Executed,
                Command = Cut(command, GmCommandLogEntry.MaxCommandLength),
                Text = Cut(text, GmCommandLogEntry.MaxTextLength)
            });
        }

        /// <summary>The command of this row was run and threw: the row says so.</summary>
        public void Failed(uint id)
        {
            var store = Store;

            if (store == null || id == 0)
                return;

            try
            {
                store.SetResult(id, GmCommandResult.Failed);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"GM audit: marking row {id} as failed failed: {e.GetBaseException().Message}");
            }
        }

        /// <summary>Runs a command whose row is <paramref name="id"/>: one that throws has its row marked failed, and throws on.</summary>
        public void Run(uint id, Action command)
        {
            try
            {
                command();
            }
            catch
            {
                Failed(id);
                throw;
            }
        }

        private uint Write(GmCommandLogEntry entry)
        {
            var store = Store;

            if (store == null)
                return 0;

            try
            {
                return store.Add(entry);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error,
                    $"GM audit: the command \"{OneLine(entry.Text)}\" of account {entry.AccountId} could not be recorded: {e.GetBaseException().Message}");

                return 0;
            }
        }

        /// <summary>Whether another refusal of this ordinary account is recorded this minute.</summary>
        private bool Admit(uint accountId)
        {
            var now = Now();

            lock (_sync)
            {
                // The accounts whose minute is long over are not worth keeping count of.
                if (_refusals.Count > 4096)
                    foreach (var stale in _refusals.Where(r => now - r.Value.Since >= MinuteMs).Select(r => r.Key).ToList())
                        _refusals.Remove(stale);

                if (!_refusals.TryGetValue(accountId, out var counted) || now - counted.Since >= MinuteMs)
                    counted = (now, 0);

                if (counted.Count >= UnprivilegedPerMinute)
                    return false;

                _refusals[accountId] = (counted.Since, counted.Count + 1);

                return true;
            }
        }

        /// <summary>What the player has selected, into the row: a player by family name and character id, a creature by its spawn's id and name.</summary>
        private static void DescribeTarget(ulong targetId, GmCommandLogEntry entry)
        {
            if (targetId == 0)
                return;

            var type = EntityManager.Instance.GetEntityType(targetId);

            switch (type)
            {
                case EntityType.Character:
                    if (EntityManager.Instance.Players.TryGetValue(targetId, out var target) && target != null)
                    {
                        entry.TargetCharacterId = target.Id;
                        entry.Target = Cut($"player {target.FamilyName}", GmCommandLogEntry.MaxTargetLength);
                    }

                    break;

                case EntityType.Creature:
                    var creature = EntityManager.Instance.GetCreature(targetId);

                    if (creature != null)
                        entry.Target = Cut($"creature {creature.DbId} {(string.IsNullOrEmpty(creature.Name) ? creature.EntityClass.ToString() : creature.Name)}",
                            GmCommandLogEntry.MaxTargetLength);

                    break;

                case EntityType.System:
                    break;

                default:
                    entry.Target = type.ToString().ToLowerInvariant();
                    break;
            }
        }

        private static string AddressOf(Client client)
        {
            try
            {
                var address = client.Socket?.RemoteAddress;

                return address == null || address.Equals(IPAddress.None) ? "" : Cut(address.ToString(), 64);
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string Cut(string text, int length) =>
            string.IsNullOrEmpty(text) ? "" : text.Length <= length ? text : text.Substring(0, length);

        private static string OneLine(string text) => (text ?? "").Replace('\r', ' ').Replace('\n', ' ');

        /// <summary>The live server's store: the character database.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public uint Add(GmCommandLogEntry entry)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.GmCommandLogs.Add(entry);
            }

            public void SetResult(uint id, GmCommandResult result)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.GmCommandLogs.SetResult(id, result);
            }
        }
    }
}
