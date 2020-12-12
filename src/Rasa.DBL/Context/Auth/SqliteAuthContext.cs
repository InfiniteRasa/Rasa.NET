using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services;

    public class SqliteAuthContext : AuthContext
    {
        public SqliteAuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService, dbContextPropertyModifier)
        {
        }
    }
}