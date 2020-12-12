using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Context
{
    using Binding;
    using Configuration;

    public abstract class DesignTimeContextFactoryBase
    {
        protected static T CreateDbContext<T>(DatabaseProvider overwriteDatabaseProvider)
            where T : DbContext
        {
            var databaseSection = LoadConfiguration(overwriteDatabaseProvider);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDatabaseProviderSpecificBindings(databaseSection);
            serviceCollection.AddDbContext<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        private static IConfigurationSection LoadConfiguration(DatabaseProvider overwriteDatabaseProvider)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();

            var databaseSection = configuration
                .GetSection("Databases");

            databaseSection[nameof(DatabaseConfiguration.Provider)] = overwriteDatabaseProvider.ToString();

            return databaseSection;
        }
    }
}