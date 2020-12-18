using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Binding;
    using Configuration;
    using Context.Char;
    using Context.World;
    using Game;
    using Hosting;
    using Initialization;
    using Managers;
    using Repositories.Char;
    using Repositories.Char.Character;
    using Repositories.Char.CharacterAppearance;
    using Repositories.Char.GameAccount;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Services.Sql;

    public class GameProgram
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
                Logger.WriteLog(LogType.Error, "Game server ended unexpectedly. Exception:");
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
            services.AddHostedService<GameHost>();
            services.AddSingleton<IRasaServer, Server>();

            services.AddSingleton<IInitializer, Initializer>();

            AddDatabase(context, services);

            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
            
            services.AddSingleton<IClientFactory, ClientFactory>();
            services.AddSingleton<ICharacterManager, CharacterManager>();

            // Char
            services.AddScoped<ICharUnitOfWork, CharUnitOfWork>();
            services.AddScoped<IGameAccountRepository, GameAccountRepository>();
            services.AddScoped<ICharacterRepository, CharacterRepository>();
            services.AddScoped<ICharacterAppearanceRepository, CharacterAppearanceRepository>();

            // World
            services.AddScoped<IWorldUnitOfWork, WorldUnitOfWork>();
            services.AddScoped<IItemTemplateItemClassRepository, ItemTemplateItemClassRepository>();
            services.AddScoped<IPlayerRandomNameRepository, PlayerRandomNameRepository>();
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
                    services.RegisterDbContextFactory<WorldContext, MySqlWorldContext>();
                    break;
                case DatabaseProvider.Sqlite:
                    services.RegisterDbContextFactory<CharContext, SqliteCharContext>();
                    services.RegisterDbContextFactory<WorldContext, SqliteWorldContext>();
                    services.AddSingleton<IInitializable>(ctx => ctx.GetService<CharContext>());
                    services.AddSingleton<IInitializable>(ctx => ctx.GetService<WorldContext>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}