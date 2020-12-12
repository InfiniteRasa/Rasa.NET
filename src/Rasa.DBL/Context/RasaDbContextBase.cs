using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context
{
    using System;
    using Configuration;
    using Configuration.ContextSetup;
    using Initialization;
    using JetBrains.Annotations;
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

        [CanBeNull]
        public T GetReadable<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
                return dbSet
                    .AsNoTracking()
                    .FirstOrDefault(e => e.Id == id);
        }

        [NotNull]
        public T GetReadableEnsuring<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
            return GetReadable(dbSet, id) ?? throw new EntityNotFoundException(typeof(T).Name, nameof(IHasId.Id), id);
        }

        [CanBeNull]
        public T GetWritable<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
            return dbSet
                .AsTracking()
                .FirstOrDefault(e => e.Id == id);
        }

        [NotNull]
        public T GetWritableEnsuring<T>(IQueryable<T> dbSet, uint id)
            where T : class, IHasId
        {
            return GetWritable(dbSet, id) ?? throw new EntityNotFoundException(typeof(T).Name, nameof(IHasId.Id), id);
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
                this.Database.Migrate();
            }
        }
    }
}