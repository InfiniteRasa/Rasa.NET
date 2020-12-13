using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Binding;
    using Configuration;
    using Context.Char;
    using Game;
    using Hosting;
    using Initialization;
    using Managers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Internal;
    using Repositories.Char.Character;
    using Repositories.Char.CharacterAppearance;
    using Repositories.Char.GameAccount;

    public class GameProgram
    {
        public static async Task<int> Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
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
                Logger.WriteLog(LogType.Error, "Game server ended unexpectedly. Exception:");
                Logger.WriteLog(LogType.Error, e);
                return e.HResult;
            }
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHostedService<GameHost>();
            services.AddSingleton<IRasaServer, Server>();

            services.AddSingleton<IInitializer, Initializer>();

            AddDatabase(context, services);
            
            services.AddSingleton<IClientFactory, ClientFactory>();
            services.AddSingleton<ICharacterManager, CharacterManager>();

            services.AddScoped<IGameAccountRepository, GameAccountRepository>();
            services.AddScoped<ICharacterRepository, CharacterRepository>();
            services.AddScoped<ICharacterAppearanceRepository, CharacterAppearanceRepository>();
        }

        private static void AddDatabase(HostBuilderContext context, IServiceCollection services)
        {
            var databaseConfigSection = context.Configuration
                .GetSection("Databases");

            var databaseProvider = services.AddDatabaseProviderSpecificBindings(databaseConfigSection);

            switch (databaseProvider)
            {

                case DatabaseProvider.MySql:
                    services.RegisterDbContextFactory<CharContext, MySqlCharContext>();
                    break;
                case DatabaseProvider.Sqlite:
                    services.RegisterDbContextFactory<CharContext, SqliteCharContext>();
                    services.AddSingleton<IInitializable>(ctx => ctx.GetService<CharContext>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}