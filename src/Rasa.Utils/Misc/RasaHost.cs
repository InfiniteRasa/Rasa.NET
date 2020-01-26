using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rasa.Misc
{
    public static class RasaHost
    {
        public static IHostBuilder CreateDefaultRasaHost<THostedService>(string[] args) where THostedService : class, IHostedService
        {
            return new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureHostConfiguration(config =>
                {
                    config.AddEnvironmentVariables(prefix: "RASA_");
                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    if (env.IsDevelopment() && !string.IsNullOrWhiteSpace(env.ApplicationName))
                    {
                        config.AddUserSecrets<THostedService>();
                    }

                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.BuildServiceProvider(validateScopes: true);
                    services.AddHostedService<THostedService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();

                    if (Debugger.IsAttached && context.HostingEnvironment.IsDevelopment())
                        logging.AddDebug();
                });
        }
    }
}