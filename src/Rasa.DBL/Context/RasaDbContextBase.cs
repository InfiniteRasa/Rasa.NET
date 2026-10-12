using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;

using JetBrains.Annotations;

namespace Rasa.Context
{
    using Configuration;
    using Configuration.ContextSetup;
    using Initialization;
    using Repositories;
    using Structures.Interfaces;

    public abstract class RasaDbContextBase : DbContext, IInitializable
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextConfigurationService _dbContextConfigurationService;

        protected RasaDbContextBase(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextConfigurationService = dbContextConfigurationService;
        }

        public IQueryable<T> CreateTrackingQuery<T>(IQueryable<T> query)
            where T : class
        {
            return query.AsTracking();
        }

        public IQueryable<T> CreateNoTrackingQuery<T>(IQueryable<T> query)
            where T : class
        {
            return query.AsNoTracking();
        }

        [CanBeNull]
        public T Find<T>(IQueryable<T> query, uint id)
            where T : class, IHasId
        {
            return query.FirstOrDefault(e => e.Id == id);
        }

        [NotNull]
        public T FindEnsuring<T>(IQueryable<T> query, uint id)
            where T : class, IHasId
        {
            return query.FirstOrDefault(e => e.Id == id) ?? throw CreateNotFound<T>(id);
        }

        [CanBeNull]
        public T GetReadable<T>(IQueryable<T> query, uint id)
            where T : class, IHasId
        {
                query = CreateNoTrackingQuery(query);
                    return Find(query, id);
        }

        [NotNull]
        public T GetReadableEnsuring<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
            return GetReadable(dbSet, id) ?? throw CreateNotFound<T>(id);
        }

        [CanBeNull]
        public T GetWritable<T>(IQueryable<T> query, uint id)
            where T : class, IHasId
        {
            query = CreateTrackingQuery(query);
            return Find(query, id);
        }

        [NotNull]
        public T GetWritableEnsuring<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
            return GetWritable(dbSet, id) ?? throw CreateNotFound<T>(id);
        }

        private static EntityNotFoundException CreateNotFound<T>(uint id) where T : class, IHasId
        {

            return new EntityNotFoundException(typeof(T).Name, nameof(IHasId.Id), id);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = GetDatabaseConnectionConfiguration();
            _dbContextConfigurationService.Configure(optionsBuilder, configuration);
        }

        protected abstract DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration();

        /// <summary>
        /// Applies the pending migrations for this context, one at a time, naming each one as it
        /// starts and again as it finishes.
        ///
        /// Database.Migrate() applies the whole backlog behind a single call, so a first run on an
        /// empty database - which is every migration this repository has, some of them seeding
        /// thousands of rows - showed one line and then nothing for however long it took. Naming
        /// the migration that is running is the difference between a server that looks wedged and
        /// one that is visibly working, and when a migration does fail, the last line printed is
        /// the one that failed.
        ///
        /// IMigrator.Migrate(target) applies everything up to and including its target; walking
        /// GetPendingMigrations() in order therefore applies exactly one per call.
        ///
        /// This is still Sqlite only, as it was: MySql schemas are managed outside the server, and
        /// a server that migrated them on boot would apply a schema change to a live database
        /// without anyone asking it to. A MySql database is checked instead (CheckMySqlSchema).
        /// </summary>
        public void Initialize()
        {
            var context = GetType().Name;

            if (_databaseConfiguration.Value.GetDatabaseProvider() != DatabaseProvider.Sqlite)
            {
                CheckMySqlSchema(context);
                return;
            }

            var pending = this.Database.GetPendingMigrations().ToList();

            if (pending.Count == 0)
            {
                Console.WriteLine($"{context}: schema is up to date, nothing to apply.");
                return;
            }

            Console.WriteLine($"{context}: applying {pending.Count} migration{(pending.Count == 1 ? "" : "s")}, please wait...");

            var migrator = this.GetService<IMigrator>();
            var run = Stopwatch.StartNew();

            for (var i = 0; i < pending.Count; i++)
            {
                var step = Stopwatch.StartNew();

                Console.WriteLine($"{context}: [{i + 1}/{pending.Count}] {pending[i]} - starting");
                migrator.Migrate(pending[i]);
                step.Stop();
                Console.WriteLine($"{context}: [{i + 1}/{pending.Count}] {pending[i]} - done in {step.ElapsedMilliseconds} ms");
            }

            run.Stop();

            Console.WriteLine($"{context}: {pending.Count} migration{(pending.Count == 1 ? "" : "s")} applied in {run.ElapsedMilliseconds} ms. Database ready.");
        }

        /// <summary>
        /// Names the migrations a MySql database has not had, as an error, and starts anyway: the
        /// server does not change a MySql schema itself, and whoever runs it may have applied a
        /// change by hand. Nothing used to look, so a database behind the code ran until a query
        /// failed - an account.password still varchar(64), for one, locked accounts out on the
        /// rehash of their password (BR-155).
        /// </summary>
        private void CheckMySqlSchema(string context)
        {
            List<string> pending;

            try
            {
                pending = this.Database.GetPendingMigrations().ToList();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"{context}: could not read which migrations this database has had: {e.Message}");
                return;
            }

            if (pending.Count == 0)
                Console.WriteLine($"{context}: schema is up to date.");
            else
                Logger.WriteLog(LogType.Error, PendingMigrationsReport(context, pending));
        }

        /// <summary>What CheckMySqlSchema says of a database missing these migrations, oldest first.</summary>
        internal static string PendingMigrationsReport(string context, IReadOnlyList<string> pending)
        {
            const int Listed = 10;

            var names = pending.Count <= Listed
                ? string.Join(", ", pending)
                : $"{string.Join(", ", pending.Take(Listed))} and {pending.Count - Listed} more, up to {pending[pending.Count - 1]}";

            return $"{context}: this MySql database has not had {pending.Count} migration{(pending.Count == 1 ? "" : "s")}: {names}. " +
                   "The server does not migrate MySql; apply them (dotnet ef database update, or their SQL scripts) before anyone logs in, " +
                   "or what they add will fail when it is used.";
        }
    }
}