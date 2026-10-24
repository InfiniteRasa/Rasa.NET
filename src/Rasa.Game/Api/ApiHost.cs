using System;

namespace Rasa.Api
{
    using Config;
    using Ingame;

    /// <summary>
    /// The server's status listeners and what they report: the REST API with its endpoints, the
    /// status port, and the <see cref="ServerStatus"/> both read. Server opens them with the
    /// player port, applies the settings again on every config reload, and closes them when it
    /// shuts down.
    ///
    /// A new REST endpoint is registered in the constructor here.
    /// </summary>
    public sealed class ApiHost
    {
        private static readonly Lazy<ApiHost> Lazy = new Lazy<ApiHost>(() => new ApiHost());

        public static ApiHost Instance => Lazy.Value;

        public ServerStatus Status { get; }
        public ApiServer Rest { get; }
        public StatusPortServer StatusPort { get; }

        /// <summary>POST /addaccount; Server gives it its way to the Auth server.</summary>
        public AddAccountEndpoint Accounts { get; } = new AddAccountEndpoint();

        /// <summary>Authentication shared by all /ingame endpoints.</summary>
        public IngameSessionService IngameSessions { get; } = new IngameSessionService();

        /// <summary>GET /monsterflags and POST /updatemonsterflags, for gametools' Monster Flag Editor; Server gives them their store.</summary>
        public MonsterFlagsEndpoint MonsterFlags { get; } = new MonsterFlagsEndpoint();
        public UpdateMonsterFlagsEndpoint UpdateMonsterFlags { get; } = new UpdateMonsterFlagsEndpoint();

        /// <summary>GET /lootpools and POST /updatelootpools, for gametools' Loot Table Editor; Server gives them their store.</summary>
        public WeightedLootEndpoint WeightedLoot { get; } = new WeightedLootEndpoint();
        public UpdateWeightedLootEndpoint UpdateWeightedLoot { get; } = new UpdateWeightedLootEndpoint();

        /// <summary>Who is online, for GET /usersonline: the world loop reads the clients into it once a second.</summary>
        public OnlineUsers Users { get; } = new OnlineUsers();

        /// <summary>POST /kickuser; Server gives it its way onto the world loop.</summary>
        public KickUserEndpoint KickUser { get; } = new KickUserEndpoint();

        /// <summary>POST /sendmessage; Server gives it its way onto the world loop.</summary>
        public SendMessageEndpoint SendMessage { get; } = new SendMessageEndpoint();

        /// <summary>GET /getopenpetitions and /getallpetitions; Server gives them their store.</summary>
        public PetitionsEndpoint OpenPetitions { get; } = new PetitionsEndpoint(openOnly: true);
        public PetitionsEndpoint AllPetitions { get; } = new PetitionsEndpoint(openOnly: false);

        /// <summary>POST /respondpetition and /closepetition; Server gives them their way onto the world loop.</summary>
        public PetitionActionEndpoint RespondPetition { get; } = new PetitionActionEndpoint(close: false);
        public PetitionActionEndpoint ClosePetition { get; } = new PetitionActionEndpoint(close: true);

        /// <summary>The log of every request the REST API is sent (api_log); Server gives it its store.</summary>
        public ApiAudit Audit { get; } = new ApiAudit();

        public ApiHost() : this(new ServerStatus())
        {
        }

        public ApiHost(ServerStatus status)
        {
            Status = status;
            Rest = new ApiServer();
            StatusPort = new StatusPortServer(status);

            Rest.Register(new HealthCheckEndpoint(status));
            Rest.Register(new ServerStatusEndpoint(status));
            Rest.Register(Accounts);
            Rest.Register(new IngameSessionExchangeEndpoint(IngameSessions));
            Rest.Register(new IngameItemCategoriesEndpoint(IngameSessions));
            Rest.Register(new IngameItemsEndpoint(IngameSessions));
            Rest.Register(new IngameItemDetailsEndpoint(IngameSessions));
            Rest.Register(new IngameItemUpdateEndpoint(IngameSessions));
            Rest.Register(MonsterFlags);
            Rest.Register(UpdateMonsterFlags);
            Rest.Register(WeightedLoot);
            Rest.Register(UpdateWeightedLoot);
            Rest.Register(new IngameMonsterFlagsEndpoint(IngameSessions, MonsterFlags));
            Rest.Register(new IngameUpdateMonsterFlagsEndpoint(IngameSessions, UpdateMonsterFlags));
            Rest.Register(new IngameWeightedLootEndpoint(IngameSessions, WeightedLoot));
            Rest.Register(new IngameUpdateWeightedLootEndpoint(IngameSessions, UpdateWeightedLoot));
            Rest.Register(new IngameCreaturesEndpoint(IngameSessions));
            Rest.Register(new IngameCreatureActionsEndpoint(IngameSessions));
            Rest.Register(new IngameCreatureDetailsEndpoint(IngameSessions));
            Rest.Register(new IngameCreatureUpdateEndpoint(IngameSessions));
            Rest.Register(new IngameMissionsEndpoint(IngameSessions));
            Rest.Register(new IngameMissionDetailsEndpoint(IngameSessions));
            Rest.Register(new IngameNpcGreetingsEndpoint(IngameSessions));
            Rest.Register(new IngameNpcGreetingUpdateEndpoint(IngameSessions));
            Rest.Register(new UsersOnlineEndpoint(Users));
            Rest.Register(KickUser);
            Rest.Register(SendMessage);
            Rest.Register(OpenPetitions);
            Rest.Register(AllPetitions);
            Rest.Register(RespondPetition);
            Rest.Register(ClosePetition);
            Rest.Audit = Audit;
        }

        /// <summary>The settings in force from now. A listener that cannot open its port stays off; the world is not held up.</summary>
        public void Apply(ApiConfig config)
        {
            config ??= new ApiConfig();

            Status.StallMs = Math.Min(Math.Max(1, config.LoopStallSeconds), 86400) * 1000;

            IngameSessions.Apply(config.Rest);

            try
            {
                Rest.Apply(config.Rest);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"REST API: the settings could not be applied: {e}");
            }

            try
            {
                StatusPort.Apply(config.StatusPort);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Status port: the settings could not be applied: {e}");
            }
        }

        public void Stop()
        {
            Rest.Stop();
            StatusPort.Stop();
        }
    }
}
