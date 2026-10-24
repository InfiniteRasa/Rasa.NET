using Microsoft.EntityFrameworkCore;

namespace Rasa.Configuration.ContextSetup
{
    using ConnectionStrings;

    public class SqliteDbContextConfigurationService : IDbContextConfigurationService
    {
        private readonly IConnectionStringFactory _connectionStringFactory;

        public SqliteDbContextConfigurationService(IConnectionStringFactory connectionStringFactory)
        {
            _connectionStringFactory = connectionStringFactory;
        }

        public void Configure(DbContextOptionsBuilder dbContextOptionsBuilder, DatabaseConnectionConfiguration configuration)
        {
            var connectionString = _connectionStringFactory.Create(configuration);
            dbContextOptionsBuilder.UseSqlite(connectionString);

            // The journal and how long a commit waits for the disk: SqlitePragmaInterceptor.
            var pragmas = SqlitePragmaInterceptor.For(configuration.JournalMode, configuration.Synchronous);

            if (pragmas != null)
                dbContextOptionsBuilder.AddInterceptors(pragmas);
        }
    }
}