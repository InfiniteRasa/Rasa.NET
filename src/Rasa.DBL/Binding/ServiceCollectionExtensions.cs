using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Binding
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Services;

    public static class ServiceCollectionExtensions
    {
        public static DatabaseProvider AddDatabaseProviderSpecificBindings(this IServiceCollection services, IConfigurationSection databaseConfigurationSection)
        {
            services.Configure<DatabaseConfiguration>(databaseConfigurationSection);

            var databaseProviderStr = databaseConfigurationSection.GetValue<string>(nameof(DatabaseConfiguration.Provider));
            var databaseProvider = DatabaseConfiguration.ConvertDatabaseProvider(databaseProviderStr);
            services.AddDependenciesByDatabaseProvider(databaseProvider);

            return databaseProvider;
        }

        private static void AddDependenciesByDatabaseProvider(this IServiceCollection services, DatabaseProvider databaseProvider)
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.MySql:
                    AddMySql(services);
                    break;
                case DatabaseProvider.Sqlite:
                    AddSqlite(services);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseProvider), $"The database provider {databaseProvider} is not known.");
            }
        }

        private static void AddMySql(IServiceCollection services)
        {
            services.AddSingleton<IConnectionStringFactory, MySqlConnectionStringFactory>();
            services.AddSingleton<IDbContextConfigurationService, MySqlDbContextConfigurationService>();
            services.AddSingleton<IDbContextPropertyModifier, MySqlDbContextPropertyModifier>();
        }

        private static void AddSqlite(IServiceCollection services)
        {
            services.AddSingleton<IConnectionStringFactory, SqliteConnectionStringFactory>();
            services.AddSingleton<IDbContextConfigurationService, SqliteDbContextConfigurationService>();
            services.AddSingleton<IDbContextPropertyModifier, SqliteDbContextPropertyModifier>();
        }
    }
}