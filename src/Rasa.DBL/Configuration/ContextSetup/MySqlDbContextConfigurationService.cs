using Microsoft.EntityFrameworkCore;

namespace Rasa.Configuration.ContextSetup
{
    using ConnectionStrings;

    public class MySqlDbContextConfigurationService : IDbContextConfigurationService
    {
        private readonly IConnectionStringFactory _connectionStringFactory;

        public MySqlDbContextConfigurationService(IConnectionStringFactory connectionStringFactory)
        {
            _connectionStringFactory = connectionStringFactory;
        }

        public void Configure(DbContextOptionsBuilder dbContextOptionsBuilder, DatabaseConnectionConfiguration configuration)
        {
            var connectionString = _connectionStringFactory.Create(configuration);
            dbContextOptionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}