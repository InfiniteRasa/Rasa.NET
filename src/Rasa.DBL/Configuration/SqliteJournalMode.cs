namespace Rasa.Configuration
{
    /// <summary>
    /// How SQLite keeps a transaction's changes until they are committed (PRAGMA journal_mode).
    /// <see cref="Default"/> leaves the database as it is, which for a new file is SQLite's own
    /// default, <see cref="Delete"/>.
    /// </summary>
    public enum SqliteJournalMode
    {
        Default,

        /// <summary>A rollback journal beside the database, deleted at each commit: SQLite's default.</summary>
        Delete,

        Truncate,

        Persist,

        /// <summary>
        /// Write-ahead log: a commit appends to the -wal file beside the database, and readers go
        /// on reading while it is written. Remembered by the database file itself.
        /// </summary>
        Wal
    }
}
