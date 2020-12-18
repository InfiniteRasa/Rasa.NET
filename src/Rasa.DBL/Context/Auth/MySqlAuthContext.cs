using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services.DbContext;

    public class MySqlAuthContext : AuthContext
    {
        public MySqlAuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier ) 
            : base(databaseConfiguration, dbContextConfigurationService, dbContextPropertyModifier)
        {
        }
    }
}