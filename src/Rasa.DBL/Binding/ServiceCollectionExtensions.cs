using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Binding
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context;
    using Services.DbContext;

    public static class ServiceCollectionExtensions
    {
        public static void RegisterDbContextFactory<TContext, TContextImplementation>(this IServiceCollection services) 
            where TContext : RasaDbContextBase
            where TContextImplementation : TContext
        {
            services.AddDbContextFactory<TContextImplementation>();
            services.AddScoped<TContext>(ctx => ctx.GetService<IDbContextFactory<TContextImplementation>>().CreateDbContext());
        }

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