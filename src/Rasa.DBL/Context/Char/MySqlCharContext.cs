using Microsoft.Extensions.Options;

namespace Rasa.Context.Char
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services;

    public class MySqlCharContext : CharContext
    {
        public MySqlCharContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService, dbContextPropertyModifier)
        {
        }
    }
}