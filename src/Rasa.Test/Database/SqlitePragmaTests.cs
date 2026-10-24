using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Services.DbContext;

    /// <summary>
    /// A SQLite database is opened with the journal mode and synchronous level its
    /// DatabaseConnectionConfiguration names (SqlitePragmaInterceptor): WAL and Normal unless
    /// it says otherwise, and the server's own databasesettings.json keeps Auth at Full.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SqlitePragmaTests
    {
        private string _directory;

        [TestInitialize]
        public void MakeFolder()
        {
            _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void DeleteFolder()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }

        [TestMethod]
        public void ByDefaultADatabaseIsKeptInWalAndACommitDoesNotWaitForTheDisk()
        {
            var configuration = new DatabaseConnectionConfiguration();

            Assert.AreEqual(SqliteJournalMode.Wal, configuration.JournalMode);
            Assert.AreEqual(SqliteSynchronous.Normal, configuration.Synchronous);

            var (journal, synchronous) = Opened(configuration);

            Assert.AreEqual("wal", journal);
            Assert.AreEqual(1L, synchronous, "NORMAL");
        }

        [TestMethod]
        [DataRow(SqliteJournalMode.Delete, SqliteSynchronous.Full, "delete", 2L)]
        [DataRow(SqliteJournalMode.Wal, SqliteSynchronous.Full, "wal", 2L)]
        [DataRow(SqliteJournalMode.Truncate, SqliteSynchronous.Extra, "truncate", 3L)]
        [DataRow(SqliteJournalMode.Wal, SqliteSynchronous.Off, "wal", 0L)]
        public void EachSettingIsTheOneTheDatabaseIsOpenedWith(
            SqliteJournalMode journalMode, SqliteSynchronous synchronous, string journal, long level)
        {
            var opened = Opened(new DatabaseConnectionConfiguration { JournalMode = journalMode, Synchronous = synchronous });

            Assert.AreEqual(journal, opened.Journal);
            Assert.AreEqual(level, opened.Synchronous);
        }

        [TestMethod]
        public void DefaultLeavesTheDatabaseAsSqliteHasIt()
        {
            Assert.IsNull(SqlitePragmaInterceptor.For(SqliteJournalMode.Default, SqliteSynchronous.Default));

            var opened = Opened(new DatabaseConnectionConfiguration
            {
                JournalMode = SqliteJournalMode.Default,
                Synchronous = SqliteSynchronous.Default
            });

            Assert.AreEqual("delete", opened.Journal, "a new file's journal");
            Assert.AreEqual(2L, opened.Synchronous, "FULL, SQLite's own");
        }

        [TestMethod]
        public void TheStatementsSetTheJournalBeforeTheSynchronousLevel()
        {
            Assert.AreEqual("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;",
                SqlitePragmaInterceptor.PragmasFor(SqliteJournalMode.Wal, SqliteSynchronous.Normal));
            Assert.AreEqual("PRAGMA synchronous=FULL;",
                SqlitePragmaInterceptor.PragmasFor(SqliteJournalMode.Default, SqliteSynchronous.Full));
            Assert.AreEqual("PRAGMA journal_mode=DELETE;",
                SqlitePragmaInterceptor.PragmasFor(SqliteJournalMode.Delete, SqliteSynchronous.Default));
            Assert.AreSame(
                SqlitePragmaInterceptor.For(SqliteJournalMode.Wal, SqliteSynchronous.Normal),
                SqlitePragmaInterceptor.For(SqliteJournalMode.Wal, SqliteSynchronous.Normal),
                "one interceptor for every context with the same settings");
        }

        /// <summary>The settings the server ships with: every database in WAL, Auth's commits waiting for the disk.</summary>
        [TestMethod]
        public void TheShippedSettingsKeepAuthAtFull()
        {
            var settings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("databasesettings.json", false, false)
                .Build()
                .GetSection("Databases")
                .Get<DatabaseConfiguration>();

            Assert.AreEqual(SqliteJournalMode.Wal, settings.Auth.JournalMode);
            Assert.AreEqual(SqliteSynchronous.Full, settings.Auth.Synchronous);
            Assert.AreEqual(SqliteJournalMode.Wal, settings.Char.JournalMode);
            Assert.AreEqual(SqliteSynchronous.Normal, settings.Char.Synchronous);
            Assert.AreEqual(SqliteJournalMode.Wal, settings.World.JournalMode);
            Assert.AreEqual(SqliteSynchronous.Normal, settings.World.Synchronous);
        }

        /// <summary>A setting file from before these keys: the defaults, WAL and Normal.</summary>
        [TestMethod]
        public void SettingsWithoutTheKeysTakeTheDefaults()
        {
            var path = Path.Combine(_directory, "old.json");
            File.WriteAllText(path, "{ \"Databases\": { \"Provider\": \"Sqlite\", \"Char\": { \"Database\": \"rasachar\" } } }");

            var settings = new ConfigurationBuilder()
                .AddJsonFile(path, false, false)
                .Build()
                .GetSection("Databases")
                .Get<DatabaseConfiguration>();

            Assert.AreEqual(SqliteJournalMode.Wal, settings.Char.JournalMode);
            Assert.AreEqual(SqliteSynchronous.Normal, settings.Char.Synchronous);
        }

        /// <summary>A database file opened through a context with these settings: what SQLite says it is using.</summary>
        private (string Journal, long Synchronous) Opened(DatabaseConnectionConfiguration configuration)
        {
            configuration.Database = Path.Combine(_directory, Guid.NewGuid().ToString("N"));

            using var context = new SqliteCharContext(
                Options.Create(new DatabaseConfiguration { Provider = "Sqlite", Char = configuration }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());

            context.Database.OpenConnection();

            using var command = context.Database.GetDbConnection().CreateCommand();

            command.CommandText = "PRAGMA journal_mode";
            var journal = (string)command.ExecuteScalar();
            command.CommandText = "PRAGMA synchronous";
            var synchronous = (long)command.ExecuteScalar();

            return (journal, synchronous);
        }
    }
}
