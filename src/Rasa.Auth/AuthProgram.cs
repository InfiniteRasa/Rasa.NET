using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using System;

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
        }
    }
}
