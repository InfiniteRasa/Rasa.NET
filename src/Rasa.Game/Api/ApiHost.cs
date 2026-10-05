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
            Rest.Register(new IngameSessionChallengeEndpoint(IngameSessions));
            Rest.Register(new IngameSessionExchangeEndpoint(IngameSessions));
            Rest.Register(new IngameItemCategoriesEndpoint(IngameSessions));
            Rest.Register(new IngameItemsEndpoint(IngameSessions));
            Rest.Register(new IngameItemDetailsEndpoint(IngameSessions));
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
