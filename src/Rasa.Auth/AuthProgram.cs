using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Auth;
    using Context;
    using Hosting;
    using Repositories.AuthAccount;
    using Services;

    public class AuthProgram
    {
        public static async Task<int> Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
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

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHostedService<AuthHost>();
            services.AddSingleton<IRasaServer, Server>();

            services.AddDbContext<AuthContext>(optionsAction => 
                optionsAction.UseSqlite("Data Source=:memory:;Version=3;New=True;"));
            services.AddSingleton<IAuthAccountRepository, AuthAccountRepository>();
            services.AddSingleton<IRandomNumberService, RandomNumberService>();
        }
    }
}
