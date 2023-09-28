using System;
using System.Diagnostics;
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
    using Game.Handlers;
    using Hosting;
    using Initialization;
    using Repositories.Char;
    using Repositories.Char.CensorWord;
    using Repositories.Char.Character;
    using Repositories.Char.CharacterAbilityDrawer;
    using Repositories.Char.CharacterAppearance;
    using Repositories.Char.CharacterInventory;
    using Repositories.Char.CharacterLockbox;
    using Repositories.Char.CharacterLogos;
    using Repositories.Char.CharacterMission;
    using Repositories.Char.CharacterOption;
    using Repositories.Char.CharacterSkills;
    using Repositories.Char.CharacterTeleporter;
    using Repositories.Char.CharacterTitle;
    using Repositories.Char.Clan;
    using Repositories.Char.ClanInventory;
    using Repositories.Char.ClanMember;
    using Repositories.Char.Friend;
    using Repositories.Char.GameAccount;
    using Repositories.Char.Ignored;
    using Repositories.Char.Items;
    using Repositories.Char.UserOption;
    using Repositories.UnitOfWork;
    using Repositories.World;

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
                Console.WriteLine("Game server ended unexpectedly. Exception:");
                Console.WriteLine(e);
                Debugger.Break();
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

            services.AddSingleton<IGameUnitOfWorkFactory, GameUnitOfWorkFactory>();

            // Client handling
            services.AddSingleton<IClientFactory, ClientFactory>();
            services.AddTransient<Client>();
            services.AddTransient<ClientPacketHandler>();

            // Managers
            // removed all manager interfaces until someone fix it
            //services.AddSingleton<ICharacterManager, CharacterManager>();

            // Char
            services.AddScoped<ICharUnitOfWork, CharUnitOfWork>();
            services.AddScoped<IGameAccountRepository, GameAccountRepository>();
            services.AddScoped<ICensoredWordRepository, CensoredWordRepository>();
            services.AddScoped<ICharacterRepository, CharacterRepository>();
            services.AddScoped<ICharacterAbilityDrawerRepository, CharacterAbilityDrawerRepository>();
            services.AddScoped<ICharacterAppearanceRepository, CharacterAppearanceRepository>();
            services.AddScoped<ICharacterInventoryRepository, CharacterInventoryRepository>();
            services.AddScoped<ICharacterLockboxRepository, CharacterLockboxRepository>();
            services.AddScoped<ICharacterLogosRepository, CharacterLogosRepository>();
            services.AddScoped<ICharacterMissionRepository, CharacterMissionRepository>();
            services.AddScoped<ICharacterOptionRepository, CharacterOptionRepository>();
            services.AddScoped<ICharacterSkillsRepository, CharacterSkillsRepository>();
            services.AddScoped<ICharacterTeleporterRepository, CharacterTeleporterRepository>();
            services.AddScoped<ICharacterTitleRepository, CharacterTitleRepository>();
            services.AddScoped<IClanRepository, ClanRepository>();
            services.AddScoped<IClanInventoryRepository, ClanInventoryRepository>();
            services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
            services.AddScoped<IFriendRepository, FriendRepository>();
            services.AddScoped<IIgnoredRepository, IgnoredRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IUserOptionRepository, UserOptionRepository>();

            // World
            services.AddScoped<IWorldUnitOfWork, WorldUnitOfWork>();
            services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            services.AddScoped<ICreatureRepository, CreatureRepository>();
            services.AddScoped<IEntityClassRepository, EntityClassRepository>();
            services.AddScoped<IFootlockerRepository, FootlockerRepository>();
            services.AddScoped<ILogosRepository, LogosRepository>();
            services.AddScoped<IMapInfoRepository, MapInfoRepository>();
            services.AddScoped<INpcMissionRepository, NpcMissionRepository>();
            services.AddScoped<INpcMissionRewardRepository, NpcMissionRewardRepository>();
            services.AddScoped<INpcPackageRepository, NpcPackageRepository>();
            services.AddScoped<IPlayerRandomNameRepository, PlayerRandomNameRepository>();
            services.AddScoped<ISpawnpoolRepository, SpawnpoolRepository>();
            services.AddScoped<ITeleporterRepository, TeleporterRepository>();
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