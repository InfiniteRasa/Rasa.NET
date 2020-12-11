using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Binding
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;

    public static class ServiceCollectionExtensions
    {
        public static DatabaseProvider AddDatabaseProviderSpecificBindings(this IServiceCollection services, string databaseProviderStr)
        {
            var databaseProvider = DatabaseConfiguration.ConvertProvider(databaseProviderStr);
            services.AddDatabaseProviderSpecificBindings(databaseProvider);
            return databaseProvider;
        }

        public static void AddDatabaseProviderSpecificBindings(this IServiceCollection services, DatabaseProvider databaseProvider)
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
        }

        private static void AddSqlite(IServiceCollection services)
        {
            services.AddSingleton<IConnectionStringFactory, SqliteConnectionStringFactory>();
            services.AddSingleton<IDbContextConfigurationService, SqliteDbContextConfigurationService>();
        }
    }
}