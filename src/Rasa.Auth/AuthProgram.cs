using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Auth;
    using Binding;
    using Configuration;
    using Context.Auth;
    using Hosting;
    using Initialization;
    using Repositories.AuthAccount;
    using Services;

    public class AuthProgram
    {
        public static async Task<int> Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(ConfigureApp)
                .ConfigureServices(ConfigureServices);
            var host = hostBuilder.Build();

            try
            {
                host.Services.GetService<IInitializer>().Execute();
                await host.RunAsync();
                return 0;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Auth server ended unexpectedly. Exception:");
                Logger.WriteLog(LogType.Error, e);
                return e.HResult;
            }
        }

        private static void ConfigureApp(HostBuilderContext context, IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false);
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHostedService<AuthHost>();
            services.AddSingleton<IRasaServer, Server>();

            services.AddSingleton<IInitializer, Initializer>();

            AddDatabase(context, services);

            services.AddSingleton<IAuthAccountRepository, AuthAccountRepository>();
            services.AddSingleton<IRandomNumberService, RandomNumberService>();
        }

        private static void AddDatabase(HostBuilderContext context, IServiceCollection services)
        {

            var databaseConfigSection = context.Configuration
                .GetSection("Databases");
            services.Configure<DatabaseConfiguration>(databaseConfigSection);

            var databaseProvider = services.AddDatabaseProviderSpecificBindings(databaseConfigSection.GetValue<string>("Provider"));

            switch (databaseProvider)
            {

                case DatabaseProvider.MySql:
                    services.AddDbContext<AuthContext, MySqlAuthContext>();
                    break;
                case DatabaseProvider.Sqlite:
                    services.AddDbContext<AuthContext, SqliteAuthContext>();
                    services.AddSingleton<IInitializable>(ctx => ctx.GetService<AuthContext>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
