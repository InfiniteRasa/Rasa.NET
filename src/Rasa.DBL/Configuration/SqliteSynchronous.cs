namespace Rasa.Configuration
{
    /// <summary>
    /// How long a SQLite commit waits for the disk (PRAGMA synchronous). <see cref="Default"/>
    /// leaves the connection as SQLite opens it, which is <see cref="Full"/>.
    /// </summary>
    public enum SqliteSynchronous
    {
        Default,

        /// <summary>Never waits. A power cut or an operating system crash can corrupt the database.</summary>
        Off,

        /// <summary>
        /// With <see cref="SqliteJournalMode.Wal"/>: a commit does not wait for the disk, a
        /// checkpoint does. The database cannot be corrupted by a crash of any kind; a power cut
        /// or an operating system crash can lose the last commits before it. A crash of the
        /// server's own process loses nothing: what it committed is with the operating system.
        /// </summary>
        Normal,

        /// <summary>Every commit waits until the disk has it: SQLite's default.</summary>
        Full,

        Extra
    }
}
