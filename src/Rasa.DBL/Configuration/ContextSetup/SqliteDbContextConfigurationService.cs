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
        }
    }
}