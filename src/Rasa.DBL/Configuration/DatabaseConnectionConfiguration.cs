namespace Rasa.Configuration
{
    public class DatabaseConnectionConfiguration
    {
        public string Host { get; set; }
        public uint Port { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int TimeoutInMilliseconds { get; set; }

        /// <summary>
        /// Sqlite only: the journal this database is kept with (SqlitePragmaInterceptor). WAL by
        /// default, against SQLite's own rollback journal.
        ///
        /// Everything the game server writes, it writes on its one loop thread and waits for:
        /// a kill's experience, a corpse's loot, a reload. With the rollback journal each commit
        /// writes the journal, waits for the disk, writes the database, waits again and deletes
        /// the journal - measured at 2.5 to 3 ms a commit on a file-backed database
        /// (HotPathDatabaseBenchmarks), every one of them a pause for every map. In WAL a commit
        /// is one append to the log, and a reader (the web API, a save) is not held up by a
        /// writer.
        ///
        /// WAL keeps two files beside the database, -wal and -shm, while it is open: a backup
        /// copied while the server runs needs all three, or SQLite's own backup.
        /// </summary>
        public SqliteJournalMode JournalMode { get; set; } = SqliteJournalMode.Wal;

        /// <summary>
        /// Sqlite only: how long each commit waits for the disk. Normal by default, which with
        /// <see cref="JournalMode"/> WAL is SQLite's own recommendation: the database cannot be
        /// corrupted, and what a power cut can cost is the last commits before it - not
        /// anything a crash of the server itself can cost. Full waits for the disk at every
        /// commit; Auth keeps it (databasesettings.json): it writes only at a login or a new
        /// account, and an account is not something to lose.
        /// </summary>
        public SqliteSynchronous Synchronous { get; set; } = SqliteSynchronous.Normal;
    }
}
