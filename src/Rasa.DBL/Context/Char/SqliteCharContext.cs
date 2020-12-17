using Microsoft.Extensions.Options;

namespace Rasa.Context.Char
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services;
    using Services.DbContext;

    public class SqliteCharContext : CharContext
    {
        public SqliteCharContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService, dbContextPropertyModifier)
        {
        }
    }
}