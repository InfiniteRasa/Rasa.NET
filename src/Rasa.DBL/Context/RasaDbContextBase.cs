using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context
{
    using Configuration;
    using Configuration.ContextSetup;
    using Initialization;

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