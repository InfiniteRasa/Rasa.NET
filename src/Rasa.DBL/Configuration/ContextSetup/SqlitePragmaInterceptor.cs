using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Rasa.Configuration.ContextSetup
{
    /// <summary>
    /// Sets a database's journal mode and synchronous level (DatabaseConnectionConfiguration)
    /// on every connection EF opens to it.
    ///
    /// Microsoft.Data.Sqlite's connection string has no keyword for either, and synchronous is
    /// a setting of the connection, not of the file, so it is said again at each open - one
    /// statement, against a pooled connection, which is microseconds. The journal mode is the
    /// file's: once a database is in WAL, asking again changes nothing.
    ///
    /// A journal mode that cannot be changed - another process has the file open in the middle
    /// of a write - is logged and the connection used as it is: the database is as safe as it
    /// was, only slower to commit to.
    /// </summary>
    public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
    {
        private static readonly ConcurrentDictionary<(SqliteJournalMode, SqliteSynchronous), SqlitePragmaInterceptor> Shared = new();

        private readonly string _pragmas;
        private int _failuresLogged;

        private SqlitePragmaInterceptor(string pragmas)
        {
            _pragmas = pragmas;
        }

        /// <summary>The statements a connection is opened with, or null for none: both left at their default.</summary>
        public string Pragmas => _pragmas;

        /// <summary>The interceptor for these settings, one for every context that has them; null when both are left at SQLite's default.</summary>
        public static SqlitePragmaInterceptor For(SqliteJournalMode journalMode, SqliteSynchronous synchronous)
        {
            var pragmas = PragmasFor(journalMode, synchronous);

            return pragmas == null ? null : Shared.GetOrAdd((journalMode, synchronous), _ => new SqlitePragmaInterceptor(pragmas));
        }

        /// <summary>The PRAGMA statements for these settings, the journal mode first: synchronous is read against it.</summary>
        internal static string PragmasFor(SqliteJournalMode journalMode, SqliteSynchronous synchronous)
        {
            var statements = new List<string>();

            if (journalMode != SqliteJournalMode.Default)
                statements.Add($"PRAGMA journal_mode={journalMode.ToString().ToUpperInvariant()};");

            if (synchronous != SqliteSynchronous.Default)
                statements.Add($"PRAGMA synchronous={synchronous.ToString().ToUpperInvariant()};");

            return statements.Count == 0 ? null : string.Join(" ", statements);
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            Apply(connection);
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Apply(connection);
            return Task.CompletedTask;
        }

        private void Apply(DbConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();

                command.CommandText = _pragmas;
                command.ExecuteNonQuery();
            }
            catch (DbException error)
            {
                // Once is enough to say it: every open after would say it again.
                if (Interlocked.Increment(ref _failuresLogged) <= 3)
                    Logger.WriteLog(LogType.Error, $"Could not set '{_pragmas}' on {connection.DataSource}: {error.Message}");
            }
        }
    }
}
