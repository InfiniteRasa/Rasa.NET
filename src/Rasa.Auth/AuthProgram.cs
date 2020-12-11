using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Auth;
    using Binding;
    using Config;
    using Configuration;
    using Context;
    using Hosting;
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

            var databaseSection = context.Configuration
                .GetSection("Databases");

            var databaseProvider = databaseSection.GetValue<DatabaseProvider>("Provider");
            services.AddDatabaseProviderSpecificBindings(databaseProvider);

            var configSection = databaseSection.GetSection("Auth");
            services.Configure<DatabaseConnectionConfiguration>(configSection);

            services.AddDbContext<AuthContext>();

            services.AddSingleton<IAuthAccountRepository, AuthAccountRepository>();
            services.AddSingleton<IRandomNumberService, RandomNumberService>();
        }
    }
}
