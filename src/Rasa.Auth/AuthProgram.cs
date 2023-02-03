using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa;

using Rasa.Auth;
using Rasa.Binding;
using Rasa.Configuration;
using Rasa.Context.Auth;
using Rasa.Hosting;
using Rasa.Initialization;
using Rasa.Repositories.Auth;
using Rasa.Repositories.Auth.Account;
using Rasa.Repositories.UnitOfWork;
using Rasa.Services.Random;

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

        services.AddSingleton<IAuthUnitOfWorkFactory, AuthUnitOfWorkFactory>();
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();
        services.AddScoped<IAuthAccountRepository, AuthAccountRepository>();
        services.AddSingleton<IRandomNumberService, RandomNumberService>();
    }

    private static void AddDatabase(HostBuilderContext context, IServiceCollection services)
    {
        var databaseConfigSection = context.Configuration
            .GetSection("Databases");

        var databaseProvider = services.AddDatabaseProviderSpecificBindings(databaseConfigSection);

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
                throw new Exception("Invalid database provider specified!");
        }
    }
}
