using System;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Configuration.ContextSetup
{
    using ConnectionStrings;
    using Services.DbContext;

    public class MySqlDbContextConfigurationService : IDbContextConfigurationService
    {
        private readonly IConnectionStringFactory _connectionStringFactory;
        private readonly Func<string, ServerVersion> _detectServerVersion;

        /// <summary>
        /// The server version per connection string, detected once. This service is a
        /// singleton and Configure runs from every context's OnConfiguring - that is, once per
        /// unit of work - and ServerVersion.AutoDetect opens a connection and runs
        /// SELECT VERSION() every time it is called. Every database access therefore cost an
        /// extra round trip before it started; PlayerTryFireWeapon opens a unit of work per
        /// shot. The server does not change version while the process runs.
        /// </summary>
        private readonly ConcurrentDictionary<string, ServerVersion> _serverVersions = new();

        public MySqlDbContextConfigurationService(IConnectionStringFactory connectionStringFactory)
            : this(connectionStringFactory, ServerVersion.AutoDetect)
        {
        }

        internal MySqlDbContextConfigurationService(
            IConnectionStringFactory connectionStringFactory,
            Func<string, ServerVersion> detectServerVersion)
        {
            _connectionStringFactory = connectionStringFactory;
            _detectServerVersion = detectServerVersion;
        }

        public void Configure(DbContextOptionsBuilder dbContextOptionsBuilder, DatabaseConnectionConfiguration configuration)
        {
            var connectionString = _connectionStringFactory.Create(configuration);

            // Not GetOrAdd with the detect inside: if the database is unreachable AutoDetect
            // throws, and nothing must be cached for that.
            if (!_serverVersions.TryGetValue(connectionString, out var serverVersion))
            {
                serverVersion = _detectServerVersion(connectionString);
                _serverVersions.TryAdd(connectionString, serverVersion);
            }

            dbContextOptionsBuilder.UseMySql(connectionString, serverVersion);
            dbContextOptionsBuilder.ReplaceService<IHistoryRepository, MySqlMigrationHistoryRepository>();
        }
    }
}