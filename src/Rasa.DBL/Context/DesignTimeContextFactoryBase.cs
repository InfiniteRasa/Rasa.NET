using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Context
{
    using Binding;

    public abstract class DesignTimeContextFactoryBase
    {
        protected static T CreateDbContext<T>()
            where T : DbContext
        {
            var databaseSection = LoadConfiguration<T>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDatabaseProviderSpecificBindings(databaseSection);
            serviceCollection.AddDbContext<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        private static IConfigurationSection LoadConfiguration<T>() where T : DbContext
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();

            var databaseSection = configuration
                .GetSection("Databases");
            return databaseSection;
        }
    }
}