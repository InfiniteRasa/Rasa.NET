using Microsoft.Extensions.Options;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services.DbContext;

    public abstract class WorldContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected WorldContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.World;
        }
    }
}