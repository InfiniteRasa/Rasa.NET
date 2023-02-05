using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using JetBrains.Annotations;

namespace Rasa.Context
{
    using Configuration;
    using Configuration.ContextSetup;
    using Initialization;
    using Repositories;
    using Structures.Interfaces;
    using System;

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

        public void Initialize()
        {
            if (_databaseConfiguration.Value.GetDatabaseProvider() == DatabaseProvider.Sqlite)
            {
                Console.WriteLine($"Please wait, loading Sqlite database...");
                this.Database.Migrate();
                Console.WriteLine("Database ready.");
            }
        }
    }
}