using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Binding
{
    using System;
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseProviderSpecificBindings(this IServiceCollection services, DatabaseProvider databaseProvider)
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
            return services;
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