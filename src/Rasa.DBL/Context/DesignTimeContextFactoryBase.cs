using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Context
{
    using Binding;
    using Configuration;
    using Configuration.ContextSetup;

    public abstract class DesignTimeContextFactoryBase
    {
        protected static DbContextOptions CreateDbContextOptions(string databaseConnectionSectionName, DatabaseProvider databaseProvider)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();

            var databaseSection = configuration
                .GetSection("Databases");

            var databaseConnectionConfiguration = databaseSection
                .GetSection(databaseConnectionSectionName)
                .Get<DatabaseConnectionConfiguration>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDatabaseProviderSpecificBindings(databaseProvider);

            var setupService = serviceCollection.BuildServiceProvider()
                .GetService<IDbContextConfigurationService>();

            var optionsBuilder = new DbContextOptionsBuilder();
            setupService.Configure(optionsBuilder, databaseConnectionConfiguration);
            return optionsBuilder.Options;
        }
    }
}