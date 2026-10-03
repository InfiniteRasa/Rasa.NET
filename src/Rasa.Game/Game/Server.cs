using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Rasa.Game
{
    using Commands;
    using Config;
    using Data;
    using Hosting;
    using Login;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Communicator;
    using Packets.Game.Server;
    using Queue;
    using Repositories.UnitOfWork;
    using Structures;
    using Threading;
    using Timer;

    public class Server : ILoopable, IRasaServer
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IClientFactory _clientFactory;
        public static IGameUnitOfWorkFactory GameUnitOfWorkFactory;

        public string ServerType { get; } = "Game";

        public const int MainLoopTime = 100; // Milliseconds

        public Config Config { get; private set; }
        public IPAddress PublicAddress { get; }
        public LengthedSocket AuthCommunicator { get; private set; }
        public LengthedSocket ListenerSocket { get; private set; }
        public QueueManager QueueManager { get; private set; }
        public LoginManager LoginManager { get; set; } = new LoginManager();
        public static List<Client> Clients { get; } = new List<Client>();
        public Dictionary<uint, LoginAccountEntry> IncomingClients { get; } = new Dictionary<uint, LoginAccountEntry>();
        public MainLoop Loop { get; }
        public Timer Timer { get; } = new Timer();
        public bool Running => Loop != null && Loop.Running;
        public bool IsFull => CurrentPlayers >= Config.ServerInfoConfig.MaxPlayers;

        /// <summary>
        /// Players holding a slot: every world connection past login (character selection,
        /// loading, in world, teleporting), plus queue clients already handed off to the world
        /// port but not yet logged in there - without those, a burst of arrivals all see a free
        /// slot before any of them reaches the world. Computed on demand; it used to be a settable
        /// property nothing ever set, so it read 0, IsFull was never true, the queue let everyone
        /// straight through and the server list always showed an empty server.
        /// </summary>
        public ushort CurrentPlayers
        {
            get
            {
                int count;

                lock (Clients)
                    count = Clients.Count(c => c.IsAuthenticated());

                if (QueueManager != null)
                    count += QueueManager.RedirectingClients;

                return (ushort)Math.Min(count, ushort.MaxValue);
            }
        }

        private readonly List<Client> _clientsToRemove = new List<Client>();

        /// <summary>
        /// Accounts Auth has reported as banned while this process was connected to it. Auth refuses
        /// a locked account at login, so this only has to cover players who were already past that
        /// check when the ban landed - a redirect in flight is refused at the world login.
        /// </summary>
        private readonly HashSet<uint> _lockedAccounts = new HashSet<uint>();
        private readonly PacketRouter<Server, CommOpcode> _router = new PacketRouter<Server, CommOpcode>();
        public Server(
            IHostApplicationLifetime hostApplicationLifetime,
            IClientFactory clientFactory,
            IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _clientFactory = clientFactory;
            GameUnitOfWorkFactory= gameUnitOfWorkFactory;

            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);

            PublicAddress = IPAddress.Parse(Config.GameConfig.PublicAddress);

            LengthedSocket.InitializeEventArgsPool(Config.SocketAsyncConfig.MaxClients * Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            BufferManager.Initialize(Config.SocketAsyncConfig.BufferSize, Config.SocketAsyncConfig.MaxClients, Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
            CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
            CommandProcessor.RegisterCommand("gm", ProcessGmCommand);
            CommandProcessor.RegisterCommand("petition", ProcessPetitionCommand);
            CommandProcessor.RegisterCommand("flag", ProcessFlagCommand);
            CommandProcessor.RegisterCommand("perf", ProcessPerfCommand);
            CommandProcessor.RegisterCommand("maperrors", ProcessMapErrorsCommand);
            CommandProcessor.RegisterCommand("kb", ProcessKbCommand);
            CommandProcessor.RegisterCommand("voice", ProcessVoiceCommand);
            CommandProcessor.RegisterCommand("announce", ProcessAnnounceCommand);
            CommandProcessor.RegisterCommand("kick", ProcessKickCommand);
            CommandProcessor.RegisterCommand("mute", ProcessMuteCommand);
            CommandProcessor.RegisterCommand("unmute", ProcessUnmuteCommand);
            CommandProcessor.RegisterCommand("motd", ProcessMotdCommand);
        }

        ~Server()
        {
            Shutdown();
        }

        #region Configuration
        private static void ConfigReLoaded()
        {
            // Configuration re-registers for every change and runs ConfigLoaded after this; it no
            // longer needs a Load() from here to hear the next one.
            Logger.WriteLog(LogType.Initialize, "Config file reloaded by external change!");
        }

        private void ConfigLoaded()
        {
            // Built and bound before it replaces the one in force: a reload runs on the file
            // watcher's thread, and the loop reading Config halfway through a Bind saw a new,
            // empty Config with every value zero.
            var config = new Config();
            Configuration.Bind(config);
            Config = config;

            Logger.UpdateConfig(Config.LoggerConfig);

            AutoSave.IntervalMinutes = Config.GameConfig?.AutoSaveMinutes ?? AutoSave.DefaultMinutes;

            // The maps that run in several shared copies. A copy already open stays as it is.
            MapInstancePolicies.Apply(Config.MapInstances);

            // The maps entered as a squad's own instance. An instance already open stays as it is.
            SquadInstancePolicies.Apply(Config.SquadInstances);

            // The numbers of the Edmund Range match; a match being played keeps its clock.
            Battlegrounds.Instance.Config = Config.Battleground ?? new BattlegroundConfig();

            // Off, log or refuse for each of the three movement checks, and the three weapon checks.
            MovementChecks.Config = Config.MovementChecks ?? new MovementChecksConfig();
            WeaponChecks.Config = Config.WeaponChecks ?? new WeaponChecksConfig();

            // A message changed by editing the file goes to everyone in the world, from the loop;
            // the first load is before anyone is here.
            if (MessageOfTheDay.Apply(Config.MessageOfTheDay) && _motdApplied)
                RunOnLoop("motd", () =>
                {
                    List<Client> clients;

                    lock (Clients)
                        clients = Clients.ToList();

                    var sent = MessageOfTheDay.SendToAll(clients);
                    Logger.WriteLog(LogType.Initialize, $"Message of the day changed; sent to {sent} player(s).");
                });

            _motdApplied = true;

            ServerFlagManager.Instance.LoadConfiguredFlags(Config.GameDataConfig?.ServerFlags);
            CharacterManager.LoadEnabledRaces(Config.GameDataConfig?.EnabledRaces);

            if (!KnowledgeBaseManager.Instance.Load(Config.GameDataConfig?.KnowledgeBaseFile, out var kbProblem))
                Logger.WriteLog(LogType.Initialize, $"Knowledge base: {kbProblem}. SearchKB will answer with nothing.");

            // A reload turns voice on or off, or moves it, without a restart. The first load comes
            // before the world is up; Start opens the voice port with the others.
            if (_voiceApplied)
                Voice.VoiceServer.Instance.Apply(Config.VoiceConfig, Config.GameConfig?.PublicAddress);

            // The REST API and the status port the same: keys and allow lists from the next
            // request, a changed port by reopening the listener.
            if (_apiApplied)
                Api.ApiHost.Instance.Apply(Config.ApiConfig);
        }

        /// <summary>Set once Start has opened the status listeners, so reloads apply ApiConfig too.</summary>
        private bool _apiApplied;

        /// <summary>Set once Start has applied VoiceConfig, so reloads apply it too.</summary>
        private bool _voiceApplied;

        /// <summary>Set after the first config load, so only a change made later is pushed to players.</summary>
        private bool _motdApplied;

        private long _loopWorkSequence;

        /// <summary>
        /// Runs work on the main loop's next pass. The console and the config watcher have threads
        /// of their own; anything that sends to clients or walks the world goes through here.
        /// </summary>
        private void RunOnLoop(string name, Action work) =>
            Timer.Add($"{name}:{Interlocked.Increment(ref _loopWorkSequence)}", 1, false, work);
        #endregion

        public void Disconnect(Client client)
        {
            lock (_clientsToRemove)
                _clientsToRemove.Add(client);
        }

        /// <summary>CurrentPlayers, as the status listeners' sample asks for it.</summary>
        private Func<int> _playersHoldingASlot;

        public void MainLoop(long delta)
        {
            // For the status listeners (Api.ServerStatus), whose threads read only what is left
            // for them here: the loop is alive, and once a second how many players hold a slot.
            Api.ApiHost.Instance.Status.Beat(_playersHoldingASlot ??= () => CurrentPlayers, Config?.ServerInfoConfig?.MaxPlayers ?? 0, ListenerSocket != null);

            // The host is stopping (Ctrl+C, a service stop, docker stop): everyone saved and out,
            // on this thread, which owns the world (EvacuateForHost waits for it).
            var evacuation = Interlocked.Exchange(ref _evacuationRequest, null);

            if (evacuation != null)
            {
                try
                {
                    EvacuateAll();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Saving the players before the server stopped failed: {e}");
                }
                finally
                {
                    evacuation.Set();
                }

                return;
            }

            // One thread runs the timers, the world and every client in turn. Anything that
            // escapes here costs the rest of the tick - the clients after the one that threw
            // are not serviced at all - so each part is held to its own failure.
            try
            {
                Timer.Update(delta);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error updating the server timers: {e}");
            }

            if (Clients.Count == 0)
                return;

            // The whole tick runs under the Clients lock, the map workers included. OnLogin
            // adds to Clients from a socket thread under this lock, and the workers read the
            // list without it - PartyManager.ExpireHeldMembers every tick, and every
            // Server.Clients.Find reached from a queued action or a manager - so a login
            // landing mid-tick could hand a Find a half-added slot or an enumerator a
            // "collection was modified". Logins wait for the end of the tick instead, which
            // is at most the loop interval.
            lock (Clients)
            {
                try
                {
                    MapChannelManager.Instance.MapChannelWorker(delta);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Error in the map channel worker: {e}");
                }

                foreach (var client in Clients)
                {
                    if (client.PendingTransfer != null &&
                        MapChannelManager.Instance.CheckTransferTimeout(client))
                        continue;

                    // Client.Update guards its own handlers and disconnects the client that
                    // threw. This is for the rest of it - Close(), the socket, the packet
                    // queue - so a fault in one connection cannot leave every client after it
                    // in the list frozen for the tick.
                    try
                    {
                        client.Update(delta);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Error updating client {client.Socket?.RemoteAddress}, disconnecting it: {e}");

                        try
                        {
                            client.Close(false);
                        }
                        catch (Exception inner)
                        {
                            Logger.WriteLog(LogType.Error, $"And closing it threw as well: {inner}");
                        }
                    }
                }

                if (_clientsToRemove.Count > 0)
                {
                    List<Client> dropped;

                    lock (_clientsToRemove)
                    {
                        foreach (var client in _clientsToRemove)
                        {
                            Clients.Remove(client);

                            // Nothing else reaches this client now, and this is the MainLoop,
                            // which is the thread its inbound stream belongs to.
                            try
                            {
                                client.ReleaseInboundBuffers();
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(LogType.Error, $"Failed to release inbound buffers for a disconnected client: {e}");
                            }
                        }

                        dropped = new List<Client>(_clientsToRemove);
                        _clientsToRemove.Clear();
                    }

                    // Any of those whose character is still in the world, on no map's list where a
                    // map worker would find it. Outside the lock: removing a player reaches into
                    // every manager, and a connection closed from any of them comes back through
                    // Disconnect for this lock - as one closed on a socket thread does, holding
                    // that connection's own lock while it waits.
                    foreach (var client in dropped)
                        MapChannelManager.Instance.CleanupDisconnected(client);
                }
            }
        }

        #region Socketing

        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.GameConfig.Port == 0 || Config.GameConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            // The world first, then the doors. Everything but the mission check used to come
            // after the loop ticking, the auth link logging in (which puts this world on the
            // server list), and both the world and queue ports accepting - while about twenty
            // loaders still had the maps, clans, dynamic objects, navmesh and abilities to build.
            // The first connection to finish its key exchange had the map worker walking
            // MapChannelArray while MapChannelInit was still adding to it. A restart is exactly
            // when everyone reconnects at once. ServerStartupLifecycle runs these in that order:
            // load everything; then bind and listen; then start accepting; then start the loop;
            // and last of all log in to the auth server, which is what tells players this world
            // is up.
            return new ServerStartupLifecycle(
                ValidateMissionReadiness,
                () => Loop.Start(),
                SetupCommunicator,
                CreateListenerSocket,
                RegisterLoginAndQueue,
                BeginAcceptingClients,
                RegisterStartupTimers,
                LoadRemainingRuntimeData,
                PublishReady,
                Shutdown).Start();
        }

        private bool ValidateMissionReadiness()
        {
            EntityClassManager.Instance.LoadEntityClasses();
            var missionValidation = MissionApplication.Instance.LoadMissions();
            return LogMissionValidationAndCheckReadiness(missionValidation);
        }

        private void CreateListenerSocket()
        {
            try
            {
                ListenerSocket = new LengthedSocket(SizeType.Dword, false);
                ListenerSocket.OnError += OnError;
                ListenerSocket.OnAccept += OnAccept;
                ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.GameConfig.Port));
                ListenerSocket.Listen(Config.GameConfig.Backlog);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the client socket! Exception:");
                Logger.WriteLog(LogType.Error, e);
                throw;
            }
        }

        private void RegisterLoginAndQueue()
        {
            LoginManager.OnLogin += OnLogin;
            QueueManager = new QueueManager(this);
        }

        private void BeginAcceptingClients()
        {
            ListenerSocket.AcceptAsync();
            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.GameConfig.Port);

            // Squad voice chat. A voice port that cannot be bound leaves voice off, not the world.
            _voiceApplied = true;
            Voice.VoiceServer.Instance.Apply(Config.VoiceConfig, Config.GameConfig.PublicAddress);

            if (Config.VoiceConfig?.Enabled != true)
                Logger.WriteLog(LogType.Initialize, "Squad voice chat is off (VoiceConfig.Enabled).");

            // The REST API and the status port. Either one failing to open its port leaves that
            // one off, not the world. Until PublishReady they report the game server unhealthy.
            var api = Api.ApiHost.Instance;

            api.Status.AuthLinked = () => AuthLinkUp;

            // /addaccount asks the Auth server, whose the accounts are, over the link to it.
            _accountRelay.Connected = () => AuthLinkUp;
            _accountRelay.Send = request => (AuthCommunicator ?? throw new InvalidOperationException("the link to the Auth server is down")).Send(request);
            api.Accounts.Create = _accountRelay.Create;
            api.Status.Started();
            _apiApplied = true;
            api.Apply(Config.ApiConfig);
        }

        private void RegisterStartupTimers()
        {
            Timer.Add("SessionExpire", 10000, true, () =>
            {
                var toRemove = new List<uint>();

                // A redirect session lasts one minute, which used to be harmless because the queue
                // never held anyone. With a real player count it does: keep the session alive while
                // its account is still connected to the queue, so a player who waited more than a
                // minute is not refused at the world login. Once the queue lets go of them they have
                // the usual minute to arrive.
                var queued = QueueManager?.ConnectedUserIds() ?? new HashSet<uint>();

                lock (IncomingClients)
                {
                    foreach (var entry in IncomingClients)
                        if (queued.Contains(entry.Key))
                            entry.Value.ExpireTime = DateTime.Now.AddMinutes(1);

                    toRemove.AddRange(IncomingClients.Where(ic => ic.Value.ExpireTime < DateTime.Now).Select(ic => ic.Key));

                    foreach (var rem in toRemove)
                        IncomingClients.Remove(rem);
                }
            });

            // Auctions run for 12 to 72 hours, so the exact minute one ends never matters; five
            // minutes keeps the sweep off the hot path while still returning an expired item
            // before the seller can wonder where it went. Auctions that ran out while the server
            // was down are caught by this first pass and by the check at login.
            Timer.Add("AuctionExpire", 300000, true, () => AuctionHouseManager.Instance.ExpireAuctions());

            Timer.Add("PreLoginExpire", 5000, true, ExpirePreLogin);

            Timer.Add("QueueManagerUpdate", Config.QueueConfig.UpdateInterval, true, () =>
            {
                QueueManager.Update(Config.ServerInfoConfig.MaxPlayers - CurrentPlayers);
            });

            // The client has a readout for how the world loop is doing, next to its network RTT
            // and variance ones. It only ever shows them in the diagnostics overlay, so they go
            // to the accounts that can open it rather than to everyone: a player has nowhere to
            // see this, and how hard the server is breathing is not their business.
            if (Config.GameConfig.PerformanceMetricsInterval > 0)
                Timer.Add("PerformanceMetrics", Config.GameConfig.PerformanceMetricsInterval, true, SendPerformanceMetrics);
        }

        private void LoadRemainingRuntimeData()
        {
            CreatureManager.Instance.CreatureInit();
            SpawnPoolManager.Instance.SpawnPoolInit();
            ChatCommandsManager.Instance.RegisterChatCommands();
            MapChannelManager.Instance.MapChannelInit();
            NavMeshManager.Instance.NavMeshInit(Config.GameDataConfig?.NavMeshPath);
            ClanManager.Instance.ClansInit();

            // The records of clan feuds, squad wargames and battleground matches go to the
            // character database from here on. Before the feuds are read back: each one that
            // is still running finds its open record again.
            PvpRecords.Instance.Load(new PvpRecords.ServerStore(GameUnitOfWorkFactory));

            // The clan feuds and challenges kept through the last restart; from here on every
            // change to them is kept.
            ClanFeuds.Instance.Load(new ClanFeuds.ServerStore(GameUnitOfWorkFactory));
            DynamicObjectManager.Instance.InitDynamicObjects();
            MapTriggerManager.Instance.MapTriggerInit();
            MapLinkManager.Instance.MapLinkInit();

            // After the control points and the map links: a battleground's are both.
            Battlegrounds.Instance.Init();
            RegionManager.Instance.RegionInit();
            EmitterManager.Instance.EmitterInit();
            MapMarkerManager.Instance.MapMarkerInit();
            SpawnPoolManager.Instance.ValidatePools();
            RecipeManager.Instance.RecipeInit();
            AbilityManager.Instance.AbilityInit();
            ManifestationManager.Instance.LoadSkillClasses();

            // After AbilityInit, which loads the action data it checks the creature rows against.
            CreatureManager.Instance.ValidateActions();
        }

        private void PublishReady()
        {
            // Last line of Start(), and it has to stay last. It used to sit inside
            // MapChannelInit, which is the sixth of the loaders above - so the navmesh, the
            // clans, the dynamic objects, the map triggers, the map links, the regions, the
            // map markers, the recipes, the abilities and the skill classes all loaded after
            // the server had announced it was ready, and anyone reading the console had no way
            // to tell a server still loading from one that was up.
            Logger.WriteLog(LogType.Initialize, "");
            Logger.WriteLog(LogType.Initialize, "Server ready!");

            // And only now does the health check say so.
            Api.ApiHost.Instance.Status.Ready();
        }

        internal static bool LogMissionValidationAndCheckReadiness(
            Structures.Missions.MissionValidationReport missionValidation)
        {
            if (missionValidation == null)
                throw new ArgumentNullException(nameof(missionValidation));

            foreach (var diagnostic in missionValidation.Diagnostics)
                Logger.WriteLog(LogType.Error, diagnostic.ToOperatorMessage());
            if (!missionValidation.BlocksReadiness)
                return true;

            Logger.WriteLog(
                LogType.Error,
                "Mission content validation failed for required content; the Game server will not report ready.");
            return false;
        }

        private void OnLogin(LoginClient client)
        {
            lock (Clients)
            {
                var newClient = _clientFactory.Create(client.Socket, client.Data, this);
                Clients.Add(newClient);
            }
        }

        private static void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        /// <summary>How long a world connection has to finish the key exchange, and then to log in.</summary>
        private static readonly TimeSpan PreLoginTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// World connections from one address that may be short of a login at once. Logged-in
        /// connections are limited by accounts; these were limited by nothing, and each holds a
        /// receive buffer from the pool the whole process shares.
        /// </summary>
        private const int MaxPreLoginPerAddress = 8;

        private long _nextAcceptRefusalLogTick;
        private int _acceptRefusalsSinceLog;

        private void OnAccept(LengthedSocket newSocket)
        {
            // Null once Shutdown has closed it; a last accept can still complete after that.
            ListenerSocket?.AcceptAsync();

            if (newSocket == null)
                return;

            // Going down: nobody new into a world about to close (ShutdownSchedule).
            if (RefusesArrivals)
            {
                newSocket.Close();
                return;
            }

            var address = newSocket.RemoteAddress;
            int pending;

            lock (Clients)
                pending = Clients.Count(c => c.State == ClientState.Connected && address.Equals(c.Socket.RemoteAddress));

            pending += LoginManager.CountFrom(address);

            if (pending >= MaxPreLoginPerAddress)
            {
                newSocket.Close();

                var refused = System.Threading.Interlocked.Increment(ref _acceptRefusalsSinceLog);
                var now = Environment.TickCount64;

                if (now >= System.Threading.Interlocked.Read(ref _nextAcceptRefusalLogTick))
                {
                    System.Threading.Interlocked.Exchange(ref _nextAcceptRefusalLogTick, now + 5000);
                    System.Threading.Interlocked.Exchange(ref _acceptRefusalsSinceLog, 0);

                    Logger.WriteLog(LogType.Security, $"Refused a world connection from {address}: {MaxPreLoginPerAddress} already waiting to log in from it ({refused} refused since the last of these).");
                }

                return;
            }

            LoginManager.LoginSocket(newSocket);
        }

        /// <summary>
        /// Closes world connections that have sat in the key exchange, or after it without logging
        /// in, for longer than PreLoginTimeout. Neither stage had any timeout.
        /// </summary>
        private void ExpirePreLogin()
        {
            var exchanges = LoginManager.ExpireStalled(PreLoginTimeout);

            List<Client> stalled;
            var cutoff = DateTime.UtcNow - PreLoginTimeout;

            lock (Clients)
                stalled = Clients.Where(c => c.State == ClientState.Connected && c.ConnectedTime < cutoff).ToList();

            foreach (var client in stalled)
                client.Close(false);

            if (exchanges + stalled.Count > 0)
                Logger.WriteLog(LogType.Network, $"Closed {exchanges + stalled.Count} world connection(s) that did not log in within {PreLoginTimeout.TotalSeconds:F0} s.");
        }

        public LoginAccountEntry AuthenticateClient(Client client, uint accountId, uint oneTimeKey)
        {
            LoginAccountEntry entry;

            lock (IncomingClients)
            {
                if (!IncomingClients.TryGetValue(accountId, out entry))
                    return null;

                if (entry == null || entry.OneTimeKey != oneTimeKey)
                    return null;

                IncomingClients.Remove(accountId);
            }

            // The handoff has been taken up: the queue connection it came through, if the
            // client still has it open, no longer holds a slot of its own.
            QueueManager?.Arrived(accountId);

            return entry;
        }

        /// <summary>
        /// Whether the auth server has sent a redirect for this account with this key that has
        /// not expired and not been taken up at the world port. Read only: AuthenticateClient is
        /// what consumes it.
        /// </summary>
        public bool HasPendingLogin(uint accountId, uint oneTimeKey)
        {
            lock (IncomingClients)
                return IncomingClients.TryGetValue(accountId, out var entry)
                       && entry != null
                       && entry.OneTimeKey == oneTimeKey
                       && entry.ExpireTime >= DateTime.Now;
        }

        public bool IsBanned(uint accountId)
        {
            lock (_lockedAccounts)
                return _lockedAccounts.Contains(accountId);
        }

        /// <summary>
        /// Called by a world login for its account, after the one-time key and the ban check.
        /// Closes every other connection that account holds, and says whether the new login can
        /// go ahead now.
        ///
        /// This used to refuse the new login instead and leave the old connection alone - and a
        /// connection whose peer vanished without closing it (a power cut, a sleeping laptop, a
        /// dropped Wi-Fi link, a client that crashed on the loading screen) is never noticed at
        /// the character screen or on a loading screen, so the player was told the account was
        /// in use until the server restarted. The one asking now is the one who just passed the
        /// auth server with the account's password; the older connection is either dead or the
        /// same person somewhere else, and in both cases the newest one should win.
        ///
        /// A connection with no character in the world - logged in, or at the character screen
        /// - is gone as soon as it is closed, and the login carries on. One that has a character
        /// in the world, or on its way into it, leaves a manifestation that the map worker takes
        /// out over the next tick or so (Close only flags it; see RemovePlayer). Loading the same
        /// character again while that one is still registered puts two of it in the world, so
        /// that login is refused with AlreadyLoggedIn; by the time the player has been through
        /// the auth server again the old one has left, and the next attempt goes through. The
        /// same holds while any map still has a connection of this account waiting to be removed,
        /// whatever became of the connection itself.
        /// </summary>
        public bool TakeOverSessions(Client newClient, uint accountId)
        {
            List<Client> others;

            lock (Clients)
                others = Clients.Where(c => c != newClient && c.IsAuthenticated() && c.AccountEntry?.Id == accountId).ToList();

            var inWorld = false;

            foreach (var old in others)
            {
                // Decided before the close, which moves the state to Disconnected.
                var hadCharacter = HasCharacterInWorld(old);

                inWorld |= hadCharacter;

                Logger.WriteLog(LogType.Security,
                    $"Account {accountId} logged in from {newClient.Socket?.RemoteAddress}; closing its earlier connection from {old.Socket?.RemoteAddress} (state {old.State}{(hadCharacter ? ", character in the world" : "")}).");

                old.Close();
            }

            return !inWorld && !MapChannelManager.Instance.HoldsClientOf(accountId);
        }

        /// <summary>Whether this connection's character is in a map, on its way into one, or still registered from one.</summary>
        private static bool HasCharacterInWorld(Client client)
        {
            if (client.State == ClientState.Loading || client.State == ClientState.Ingame || client.State == ClientState.Teleporting)
                return true;

            var player = client.Player;

            return player != null
                   && EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered)
                   && registered == player;
        }

        public void Shutdown()
        {
            // Reached from the exit countdown, the host stopping, a failed start and the
            // finalizer; the second and later are nothing. MainLoop.Stop throws on a loop that is
            // not running.
            if (Interlocked.Exchange(ref _shutDown, 1) == 1)
                return;

            AuthCommunicator?.Close();
            AuthCommunicator = null;

            ListenerSocket?.Close();
            ListenerSocket = null;

            Voice.VoiceServer.Instance.Stop();

            Api.ApiHost.Instance.Status.Stopped();
            Api.ApiHost.Instance.Stop();

            if (Loop.Running)
                Loop.Stop();
        }

        private int _shutDown;
        #endregion

        #region Shutting down with the players saved

        /// <summary>The exit countdown under way, or null; set from the console thread, read on the loop.</summary>
        private ShutdownSchedule _shutdownSchedule;

        /// <summary>1 once every player has been saved and taken out (EvacuateAll).</summary>
        private int _evacuated;

        /// <summary>Set by EvacuateForHost for the loop to act on; set again by the loop when done.</summary>
        private ManualResetEventSlim _evacuationRequest;

        /// <summary>Whether the world is being emptied for a shutdown: no connection that drops lingers in a fight (CombatLogout).</summary>
        public bool IsShuttingDown => Volatile.Read(ref _evacuated) == 1;

        /// <summary>Whether new connections to the world are turned away: the last minute of a countdown, or after.</summary>
        public bool RefusesArrivals =>
            IsShuttingDown || (Volatile.Read(ref _shutdownSchedule)?.RefusesArrivals(Environment.TickCount64) ?? false);

        private const string CountdownTimer = "ShutdownCountdown";

        /// <summary>
        /// exit                      - save everyone and stop now
        /// exit &lt;minutes&gt; [reason] - warn the players, then save everyone and stop
        /// exit cancel               - call off a countdown
        ///
        /// The players are warned in chat as the time runs down (ShutdownSchedule). At the end
        /// every connection is closed and every character taken out of the world the way a
        /// logout does it - position, time played, health, death penalties and cooldowns saved,
        /// a dead player sent to their hospital - before the server stops. It used to stop with
        /// no warning and nobody saved: every player came back where they had last logged in.
        /// </summary>
        private void ProcessExitCommand(string[] parts)
        {
            if (parts.Length > 1 && string.Equals(parts[1], "cancel", StringComparison.OrdinalIgnoreCase))
            {
                if (Interlocked.Exchange(ref _shutdownSchedule, null) == null)
                {
                    Logger.WriteLog(LogType.Command, "There is no shutdown to cancel.");
                    return;
                }

                Timer.Remove(CountdownTimer);
                Timer.Add("ShutdownCancelled", 1, false, () => Announce(ShutdownSchedule.CancelledMessage));
                Logger.WriteLog(LogType.Command, "Shutdown cancelled.");
                return;
            }

            var minutes = 0d;

            if (parts.Length > 1 && (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out minutes)
                                     || minutes < 0 || minutes > ShutdownSchedule.MaxMinutes || double.IsNaN(minutes)))
            {
                Logger.WriteLog(LogType.Command, "Usage: exit [minutes] [reason] | exit cancel");
                return;
            }

            var reason = string.Join(" ", parts.Skip(2)).Trim();
            var schedule = new ShutdownSchedule(Environment.TickCount64, (long)Math.Round(minutes * 60000), reason);

            // A new exit replaces a countdown already running.
            Volatile.Write(ref _shutdownSchedule, schedule);
            Timer.Add(CountdownTimer, 1000, true, ShutdownTick);

            // The first warning goes out on the loop's next pass rather than from this thread.
            Timer.Add("ShutdownFirstWarning", 1, false, ShutdownTick);

            Logger.WriteLog(LogType.Command, minutes > 0
                ? $"Shutting down in {ShutdownSchedule.Describe(schedule.SecondsLeft(Environment.TickCount64))}" + (schedule.Reason == null ? "." : $" ({schedule.Reason}).") + " 'exit cancel' calls it off."
                : "Saving everyone and shutting down now.");
        }

        /// <summary>announce &lt;message&gt;: a line in chat for everyone in the world (Moderation).</summary>
        private void ProcessAnnounceCommand(string[] parts)
        {
            var text = string.Join(" ", parts.Skip(1)).Trim();

            if (text.Length == 0)
            {
                Logger.WriteLog(LogType.Command, "Usage: announce <message>");
                return;
            }

            RunOnLoop("announce", () => Moderation.Announce(text));
        }

        /// <summary>kick &lt;familyName&gt; [reason]</summary>
        private void ProcessKickCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Logger.WriteLog(LogType.Command, "Usage: kick <familyName> [reason]");
                return;
            }

            var reason = string.Join(" ", parts.Skip(2));
            RunOnLoop("kick", () => Logger.WriteLog(LogType.Command, Moderation.Kick(parts[1], reason, null).Text));
        }

        /// <summary>mute &lt;familyName&gt; &lt;minutes&gt; [reason]</summary>
        private void ProcessMuteCommand(string[] parts)
        {
            if (parts.Length < 3 || !int.TryParse(parts[2], out var minutes))
            {
                Logger.WriteLog(LogType.Command, "Usage: mute <familyName> <minutes> [reason]");
                return;
            }

            var reason = string.Join(" ", parts.Skip(3));
            RunOnLoop("mute", () => Logger.WriteLog(LogType.Command, Moderation.Mute(parts[1], minutes, reason, null).Text));
        }

        /// <summary>unmute &lt;familyName&gt;</summary>
        private void ProcessUnmuteCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Logger.WriteLog(LogType.Command, "Usage: unmute <familyName>");
                return;
            }

            RunOnLoop("unmute", () => Logger.WriteLog(LogType.Command, Moderation.Unmute(parts[1], null).Text));
        }

        /// <summary>motd: the message of the day in force.</summary>
        private static void ProcessMotdCommand(string[] parts)
        {
            var config = MessageOfTheDay.Current;

            if (!MessageOfTheDay.HasMessage(config))
            {
                Logger.WriteLog(LogType.Command, "There is no message of the day (MessageOfTheDay.Text is empty).");
                return;
            }

            Logger.WriteLog(LogType.Command, $"Message of the day ({(config.ShowEveryLogin ? "every login" : "once per change")}): {config.Text}");

            foreach (var (languageId, text) in MessageOfTheDay.Messages(config).Where(m => m.Key != 1))
                Logger.WriteLog(LogType.Command, $"  language {languageId}: {text}");
        }

        /// <summary>The countdown, every second on the loop: the warnings due, and at the end the shutdown.</summary>
        private void ShutdownTick()
        {
            var schedule = Volatile.Read(ref _shutdownSchedule);

            if (schedule == null)
                return;

            var now = Environment.TickCount64;

            if (schedule.IsDue(now))
            {
                Timer.Remove(CountdownTimer);
                Interlocked.CompareExchange(ref _shutdownSchedule, null, schedule);

                EvacuateAll();
                Shutdown();
                _hostApplicationLifetime.StopApplication();
                return;
            }

            var warning = schedule.WarningDue(now);

            if (warning != null)
                Announce(warning);
        }

        /// <summary>A system message to every player in the world, and the console.</summary>
        private void Announce(string message)
        {
            List<Client> listeners;

            lock (Clients)
                listeners = Clients.Where(c => c.State == ClientState.Ingame || c.State == ClientState.Teleporting || c.State == ClientState.Loading).ToList();

            foreach (var client in listeners)
            {
                try
                {
                    CommunicatorManager.Instance.SystemMessage(client, message);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Could not send the shutdown notice to a client: {e.Message}");
                }
            }

            Logger.WriteLog(LogType.Command, $"Announced to {listeners.Count} player(s): {message}");
        }

        /// <summary>
        /// Every connection closed and every character saved and taken out of the world, now, on
        /// the loop thread: what each one's logout would have done, all at once (RemoveAllFlaggedPlayers
        /// has no time budget). Once only.
        /// </summary>
        private void EvacuateAll()
        {
            if (Interlocked.Exchange(ref _evacuated, 1) == 1)
                return;

            List<Client> clients;
            var removed = 0;

            lock (Clients)
            {
                clients = Clients.ToList();

                // Close flags each character in the world for removal (no lingering: IsShuttingDown)
                // and saves its position.
                foreach (var client in clients)
                {
                    try
                    {
                        client.Close(false);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Closing a connection for the shutdown threw: {e}");
                    }
                }

                try
                {
                    removed = MapChannelManager.Instance.RemoveAllFlaggedPlayers();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Taking the players out of the world for the shutdown threw: {e}");
                }
            }

            // Those on no map's list - on a loading screen, mid-transfer - and whatever else the
            // main loop clears up after a dropped connection.
            foreach (var client in clients)
            {
                try
                {
                    MapChannelManager.Instance.CleanupDisconnected(client);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Clearing up a connection for the shutdown threw: {e}");
                }
            }

            Logger.WriteLog(LogType.Initialize, $"Shutdown: {clients.Count} connection(s) closed, {removed} character(s) saved and taken out of the world.");
        }

        /// <summary>
        /// The host is stopping: has the loop save everyone and take them out (EvacuateAll), waits
        /// up to <paramref name="timeout"/> for it, then shuts down. Nothing if that is done already.
        /// </summary>
        public void EvacuateForHost(TimeSpan timeout)
        {
            if (!IsShuttingDown && Running)
            {
                using var done = new ManualResetEventSlim(false);

                Volatile.Write(ref _evacuationRequest, done);

                if (!done.Wait(timeout))
                {
                    Interlocked.CompareExchange(ref _evacuationRequest, null, done);
                    Logger.WriteLog(LogType.Error, $"The world loop did not save the players within {timeout.TotalSeconds:0} s of the stop; stopping anyway.");
                }
            }

            Shutdown();
        }
        #endregion

        #region Communicator
        private void SetupCommunicator()
        {
            if (Config.CommunicatorConfig.Port == 0 || Config.CommunicatorConfig.Address == null)
            {
                Logger.WriteLog(LogType.Error, "Invalid Communicator config data! Can't connect!");
                return;
            }

            ConnectCommunicator();
        }

        public void ConnectCommunicator()
        {
            if (AuthCommunicator?.Connected ?? false)
                AuthCommunicator?.Close();

            try
            {
                var socket = new LengthedSocket(SizeType.Word);

                AuthCommunicator = socket;
                socket.OnConnect += OnCommunicatorConnect;
                socket.OnError += OnCommunicatorError;
                socket.OnError += _ => AuthLinkLost(socket);
                socket.OnDrop += reason => OnCommunicatorDrop(socket, reason);
                socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port));
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the Auth server socket! Retrying soon... Exception:");
                Logger.WriteLog(LogType.Error, e);
            }

            Logger.WriteLog(LogType.Network, $"*** Connecting to auth server! Address: {Config.CommunicatorConfig.Address}:{Config.CommunicatorConfig.Port}");
        }

        /// <summary>
        /// The auth link that has logged in, while it stands: what app_server_status goes by
        /// (Api.ServerStatus). Written on socket threads and read on the status listeners'.
        /// </summary>
        private volatile LengthedSocket _authLinked;

        /// <summary>Whether this world is connected and logged in to the Auth server, so that a login can be handed on to it.</summary>
        internal bool AuthLinkUp
        {
            get
            {
                var link = _authLinked;

                return link != null && ReferenceEquals(link, AuthCommunicator) && link.Connected;
            }
        }

        /// <summary>
        /// Requests for an account on their way to the Auth server and back (the REST API's
        /// /addaccount): asked on a REST thread, answered on the link's.
        /// </summary>
        private readonly Api.AccountRelay _accountRelay = new Api.AccountRelay();

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.CreateAccountResponse)]
        private void MsgCreateAccountResponse(CreateAccountResponsePacket packet)
        {
            _accountRelay.Answer(packet);
        }

        /// <summary>That link has failed or been closed; a newer one that has logged in since is not touched.</summary>
        private void AuthLinkLost(LengthedSocket socket)
        {
            if (ReferenceEquals(_authLinked, socket))
                _authLinked = null;
        }

        private void OnCommunicatorError(SocketAsyncEventArgs args)
        {
            ScheduleCommunicatorReconnect();

            Logger.WriteLog(LogType.Error, "Could not connect to the Auth server! Trying again in a few seconds...");
        }

        private void ScheduleCommunicatorReconnect()
        {
            Timer.Add("CommReconnect", 10000, false, () =>
            {
                if (!AuthCommunicator?.Connected ?? true)
                    ConnectCommunicator();
            });
        }

        /// <summary>
        /// The socket layer gave up on the auth link without a socket error: no pooled buffer to
        /// re-arm its receive with (it re-arms after every message), none to send with, a full
        /// send queue, or a frame that would not decode. It logs that once and raises OnDrop;
        /// after a receive-side drop nothing reads the link again, after a send-side one nothing
        /// is written to it, and the socket itself stays open and Connected.
        ///
        /// Nothing handled OnDrop here, and the reconnect only ever came from OnError, so a
        /// dropped link stayed dropped: RedirectRequests went unread, every world login failed
        /// its session check, the auth server went on listing this world as up, and only a
        /// restart brought it back. The link is closed now - which is what makes Connected false,
        /// the condition the reconnect waits for - and a reconnect is scheduled exactly as a
        /// socket error schedules one. A drop from the connect itself (no args left to connect
        /// with) is retried the same way.
        /// </summary>
        private void OnCommunicatorDrop(LengthedSocket socket, string reason)
        {
            // Only the link in use: one already replaced by a reconnect has nothing left to say.
            if (socket != AuthCommunicator)
                return;

            AuthLinkLost(socket);

            Logger.WriteLog(LogType.Error, $"The link to the Auth server was dropped ({reason}); world logins cannot complete until it is back. Reconnecting in a few seconds...");

            // Closing completes any receive still armed with an error; this is not a failed
            // connect, and the reconnect below is already on its way.
            socket.OnError -= OnCommunicatorError;
            socket.Close();

            ScheduleCommunicatorReconnect();
        }

        private void OnCommunicatorConnect(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                OnCommunicatorError(args);
                return;
            }

            Logger.WriteLog(LogType.Network, "*** Connected to the Auth Server!");

            AuthCommunicator.OnReceive += OnCommunicatorReceive;
            AuthCommunicator.Send(new LoginRequestPacket
            {
                ServerId = Config.ServerInfoConfig.Id,
                Password = Config.ServerInfoConfig.Password,
                PublicAddress = PublicAddress
            });

            AuthCommunicator.ReceiveAsync();
        }

        /// <summary>
        /// Socket completion thread, carrying messages from the auth server - including the ban
        /// notifications, which do real work on this side. Losing the auth link over one bad
        /// message is not worth it, and letting the exception reach the socket layer's catch-all
        /// is exactly what did that, under a log line that names the socket rather than the
        /// message.
        /// </summary>
        private void OnCommunicatorReceive(BufferData data)
        {
            CommOpcode? opcode = null;

            try
            {
                opcode = (CommOpcode) data.Buffer[data.BaseOffset + data.Offset++];

                var packetType = _router.GetPacketType(opcode.Value);
                if (packetType == null)
                    return;

                var packet = Activator.CreateInstance(packetType) as IOpcodedPacket<CommOpcode>;
                if (packet == null)
                    return;

                packet.Read(data.GetReader());

                _router.RoutePacket(this, packet);
            }
            catch (Exception e)
            {
                var what = opcode.HasValue ? $"a {opcode.Value} message" : "a message whose opcode could not be read";
                Logger.WriteLog(LogType.Error, $"Error handling {what} from the auth server: {e}");
            }
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.LoginResponse)]
        private void MsgLoginResponse(LoginResponsePacket packet)
        {
            if (packet.Response == CommLoginReason.Success)
            {
                _authLinked = AuthCommunicator;
                Logger.WriteLog(LogType.Network, "Successfully authenticated with the Auth server!");
                return;
            }

            // Taken once: MsgGameInfoRequest and MsgRedirectRequest are dispatched from this
            // same frame loop and used to reach straight through this field, so a rejected login
            // followed by a buffered server-info request in the same read nulled it between the
            // two and the second handler threw. The link was then half-dead with nothing to
            // bring it back - only a socket error schedules a reconnect, and no socket error
            // had happened.
            var communicator = AuthCommunicator;

            AuthCommunicator = null;
            communicator?.Close();

            Logger.WriteLog(LogType.Error, "Could not authenticate with the Auth server! Shutting down internal communication!");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [PacketHandler(CommOpcode.ServerInfoRequest)]
        private void MsgGameInfoRequest(ServerInfoRequestPacket packet)
        {
            // Nulled by MsgLoginResponse on this same thread, from the same read.
            AuthCommunicator?.Send(new ServerInfoResponsePacket
            {
                AgeLimit = Config.ServerInfoConfig.AgeLimit,
                PKFlag = Config.ServerInfoConfig.PKFlag,
                CurrentPlayers = CurrentPlayers,
                GamePort = Config.GameConfig.Port,
                QueuePort = Config.QueueConfig.Port,
                // The configured cap the queue enforces. This used to report the socket pool size
                // (SocketAsyncConfig.MaxClients), which the auth server list showed as capacity.
                MaxPlayers = Config.ServerInfoConfig.MaxPlayers
            });
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.AccountLockChanged)]
        private void MsgAccountLockChanged(AccountLockChangedPacket packet)
        {
            lock (_lockedAccounts)
            {
                if (packet.Locked)
                    _lockedAccounts.Add(packet.AccountId);
                else
                    _lockedAccounts.Remove(packet.AccountId);
            }

            if (!packet.Locked)
            {
                Logger.WriteLog(LogType.Security, $"Account {packet.AccountId} unbanned by Auth.");
                return;
            }

            // Leave any pending IncomingClients entry in place: the world login consumes it and then
            // reaches IsBanned, so the player is told "account locked" instead of a generic
            // authentication failure. It expires on its own if they never arrive.
            QueueManager?.Disconnect(packet.AccountId);

            List<Client> online;

            // AccountEntry is null until the world login message is processed.
            lock (Clients)
                online = Clients.Where(c => c.AccountEntry != null && c.AccountEntry.Id == packet.AccountId).ToList();

            // Close() runs from socket threads already (OnError), saves the character and flags an
            // in-world player for removal on the MainLoop, so it is safe from this communicator thread.
            foreach (var client in online)
                client.Close();

            Logger.WriteLog(LogType.Security, $"Account {packet.AccountId} banned by Auth; disconnected {online.Count} world connection(s).");
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.RedirectRequest)]
        private void MsgRedirectRequest(RedirectRequestPacket packet)
        {
            lock (IncomingClients)
            {
                if (IncomingClients.ContainsKey(packet.AccountId))
                    IncomingClients.Remove(packet.AccountId);

                IncomingClients.Add(packet.AccountId, new LoginAccountEntry(packet));
            }

            AuthCommunicator?.Send(new RedirectResponsePacket
            {
                AccountId = packet.AccountId,
                Response = RedirectResult.Success
            });
        }
        #endregion

        #region Commands
        /// <summary>
        /// gm &lt;familyName&gt; [level] - reads or sets an account's GM level.
        ///
        /// This lives on the Game console rather than the Auth one because game_account.level is
        /// in the character database, which Auth has no connection to. It is a console command
        /// rather than an in-game one so that granting it never depends on already having it.
        ///
        /// The level takes effect immediately for a player who is logged in: AccountEntry is read
        /// from the database once at login, so writing the row alone would leave them at their old
        /// level until they relogged.
        /// </summary>
        private void ProcessGmCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Logger.WriteLog(LogType.Command,
                    "Usage: gm <familyName> [level]. Levels: "
                    + string.Join(", ", Enum.GetValues<GmLevel>().Select(l => $"{(byte)l} {l}")));
                return;
            }

            var familyName = parts[1];

            using var unitOfWork = GameUnitOfWorkFactory.CreateChar();

            var account = unitOfWork.GameAccounts.FindByFamilyName(familyName);

            if (account == null)
            {
                Logger.WriteLog(LogType.Command, $"No account has the family name {familyName}.");
                return;
            }

            if (parts.Length == 2)
            {
                Logger.WriteLog(LogType.Command,
                    $"{account.FamilyName} (account {account.Id}) is level {account.Level} ({Describe(account.Level)}).");
                return;
            }

            if (!TryParseLevel(parts[2], out var level))
            {
                Logger.WriteLog(LogType.Command,
                    $"'{parts[2]}' is not a level. Use a number 0-255, or one of: "
                    + string.Join(", ", Enum.GetNames<GmLevel>()));
                return;
            }

            unitOfWork.GameAccounts.UpdateAccountLevel(account.Id, level);

            // The console runs on its own thread; every other walk of this list is under the
            // lock, and this one was not. The main loop removes disconnected clients from it on
            // every tick, so a gm command timed against one threw "Collection was modified" out
            // of the enumerator - caught by the command processor, so the level silently failed
            // to reach the player who was already logged in.
            Client online;

            lock (Clients)
                online = Clients.FirstOrDefault(c => c.AccountEntry?.Id == account.Id);

            if (online?.AccountEntry != null)
                online.AccountEntry.Level = level;

            Logger.WriteLog(LogType.Command,
                $"{account.FamilyName} (account {account.Id}) is now level {level} ({Describe(level)})"
                + (online == null ? "." : ", and is logged in - it applies now."));
        }

        /// <summary>Accepts a number or a rank name, so 'gm Ellimist admin' works as well as 10.</summary>
        private static bool TryParseLevel(string value, out byte level)
        {
            if (byte.TryParse(value, out level))
                return true;

            if (Enum.TryParse<GmLevel>(value, true, out var named))
            {
                level = (byte)named;
                return true;
            }

            return false;
        }

        /// <summary>Names the rank a level reaches, since the levels in between are legal too.</summary>
        private static string Describe(byte level)
        {
            var reached = Enum.GetValues<GmLevel>().Where(l => (byte)l <= level).ToList();

            return reached.Count == 0 ? "none" : reached.Max().ToString();
        }

        /// <summary>
        /// petition list [open|resolved|cancelled|all] [count] - newest first, open by default
        /// petition show &lt;id&gt;                                 - the whole thing, body included
        /// petition resolve &lt;id&gt; [what you did]               - close it, and tell them if online
        ///
        /// The client has no window that reads a petition back and the GM half of 9.5 was never
        /// wired, so this is the read path: without it the table is only reachable with SQL.
        /// </summary>
        /// <summary>
        /// flag list | flag set &lt;name|id&gt; | flag clear &lt;name|id&gt;
        ///
        /// Changes apply to everyone who is in the world now and to everyone who logs in after,
        /// but only for as long as the server runs: the set that survives a restart is the one in
        /// GameDataConfig.ServerFlags.
        /// </summary>
        /// <summary>
        /// Hands the loop metrics for the window just finished to every GM in the world, and
        /// starts the next window. Runs on the loop thread, from the Timer.
        /// </summary>
        private void SendPerformanceMetrics()
        {
            var metrics = Loop.TakeMetrics();

            List<Client> watching;

            lock (Clients)
                watching = Clients.Where(c => c != null && c.State == ClientState.Ingame
                                              && c.AccountEntry != null
                                              && c.AccountEntry.Level >= (byte)GmLevel.Observer).ToList();

            if (watching.Count == 0)
                return;

            var packet = new ServerPerformanceMetricsPacket(metrics.LoopsPerSecond, metrics.PeakMs);

            foreach (var client in watching)
                client.CallMethod(SysEntity.ClientMethodId, packet);
        }

        /// <summary>
        /// perf - what the loop has done since the last time the metrics went out.
        /// </summary>
        /// <summary>
        /// maperrors [clear] - what the world load and the spawn path found wrong with the data.
        /// </summary>
        /// <summary>
        /// kb [reload|show &lt;id&gt;] - what is in the knowledge base, and re-reading the file.
        /// </summary>
        private void ProcessKbCommand(string[] parts)
        {
            var kb = KnowledgeBaseManager.Instance;
            var what = parts.Length > 1 ? parts[1].ToLowerInvariant() : "list";

            if (what == "reload")
            {
                Logger.WriteLog(LogType.Command,
                    kb.Load(Config.GameDataConfig?.KnowledgeBaseFile, out var problem)
                        ? $"Knowledge base reloaded: {kb.Count} article(s)."
                        : $"Knowledge base not reloaded ({problem}); the {kb.Count} already loaded are untouched.");
                return;
            }

            if (what == "show")
            {
                if (parts.Length < 3 || !uint.TryParse(parts[2], out var id) || kb.Get(id) == null)
                {
                    Logger.WriteLog(LogType.Command, "Usage: kb show <id>");
                    return;
                }

                var article = kb.Get(id);

                Logger.WriteLog(LogType.Command, $"#{article.Id} {article.Title}");

                if (article.Keywords.Count > 0)
                    Logger.WriteLog(LogType.Command, $"   keywords: {string.Join(", ", article.Keywords)}");

                foreach (var line in article.Body.Split('\n'))
                    Logger.WriteLog(LogType.Command, $"   {line.TrimEnd()}");

                return;
            }

            var all = kb.All();

            if (all.Count == 0)
            {
                Logger.WriteLog(LogType.Command,
                    $"No articles loaded (from {kb.LoadedFrom ?? Config.GameDataConfig?.KnowledgeBaseFile ?? "nowhere"}). "
                    + "Usage: kb [reload|show <id>]");
                return;
            }

            Logger.WriteLog(LogType.Command, $"{all.Count} article(s) from {kb.LoadedFrom}:");

            foreach (var article in all)
                Logger.WriteLog(LogType.Command, $"   #{article.Id} {article.Title}");
        }

        private void ProcessMapErrorsCommand(string[] parts)
        {
            var errors = MapErrorManager.Instance;

            if (parts.Length > 1 && parts[1].ToLowerInvariant() == "clear")
            {
                errors.Clear();
                Logger.WriteLog(LogType.Command, "Map errors cleared. They come back as the data is used again.");
                return;
            }

            var summary = errors.Summary();

            if (summary.Count == 0)
            {
                Logger.WriteLog(LogType.Command, "Nothing wrong with the world data so far.");
                return;
            }

            foreach (var (mapContextId, count) in summary)
            {
                Logger.WriteLog(LogType.Command,
                    mapContextId == MapErrorManager.ServerWide
                        ? $"Server-wide: {count} error(s)"
                        : $"Map {mapContextId}: {count} error(s)");

                foreach (var error in errors.ErrorsFor(mapContextId))
                    Logger.WriteLog(LogType.Command, $"   {error}");
            }
        }

        /// <summary>voice: whether voice chat is on, and who is in which squad's group.</summary>
        private void ProcessVoiceCommand(string[] parts)
        {
            foreach (var line in Voice.VoiceServer.Instance.Describe())
                Logger.WriteLog(LogType.Command, line);
        }

        private void ProcessPerfCommand(string[] parts)
        {
            var metrics = Loop.PeekMetrics();

            Logger.WriteLog(LogType.Command,
                $"Main loop: {metrics.LoopsPerSecond:0.0} loops/s ({metrics.Loops} in {metrics.WindowMs:0} ms), "
                + $"slowest pass {metrics.PeakMs:0.00} ms, target {MainLoopTime} ms.");

            int players;

            lock (Clients)
                players = Clients.Count(c => c != null && c.State == ClientState.Ingame);

            int connections;

            lock (Clients)
                connections = Clients.Count;

            Logger.WriteLog(LogType.Command,
                $"{players} player(s) in the world, {connections} connection(s), "
                + $"metrics going to GMs every {Config.GameConfig.PerformanceMetricsInterval} ms.");
        }

        private void ProcessFlagCommand(string[] parts)
        {
            var flags = ServerFlagManager.Instance;

            if (parts.Length < 2 || parts[1].ToLowerInvariant() == "list")
            {
                var set = flags.Flags;

                Logger.WriteLog(LogType.Command,
                    set.Count == 0
                        ? "No server flags are set."
                        : "Set: " + string.Join(", ", set.Select(f => $"{f} ({(uint)f})")));

                Logger.WriteLog(LogType.Command, "Known: " + ServerFlagManager.KnownFlags());
                Logger.WriteLog(LogType.Command, "Usage: flag list | flag set <name|id> | flag clear <name|id>");
                return;
            }

            var action = parts[1].ToLowerInvariant();

            if (action != "set" && action != "clear")
            {
                Logger.WriteLog(LogType.Command, "Usage: flag list | flag set <name|id> | flag clear <name|id>");
                return;
            }

            if (parts.Length < 3 || !ServerFlagManager.TryParse(parts[2], out var flag))
            {
                Logger.WriteLog(LogType.Command,
                    $"'{(parts.Length < 3 ? string.Empty : parts[2])}' is not a server flag. Known: "
                    + ServerFlagManager.KnownFlags());
                return;
            }

            var changed = action == "set" ? flags.Set(flag) : flags.Clear(flag);

            if (!changed)
                Logger.WriteLog(LogType.Command, $"{flag} was already {(action == "set" ? "set" : "clear")}.");
        }

        private void ProcessPetitionCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Logger.WriteLog(LogType.Command,
                    "Usage: petition list [open|resolved|cancelled|all] [count] | petition show <id> "
                    + "| petition resolve <id> [note]");
                return;
            }

            switch (parts[1].ToLowerInvariant())
            {
                case "list":
                    PetitionList(parts);
                    return;

                case "show":
                    PetitionShow(parts);
                    return;

                case "resolve":
                    PetitionResolve(parts);
                    return;

                default:
                    Logger.WriteLog(LogType.Command, $"'{parts[1]}' is not a petition command. Use list, show or resolve.");
                    return;
            }
        }

        private static void PetitionList(string[] parts)
        {
            PetitionStatus? status = PetitionStatus.Open;

            if (parts.Length > 2 && !parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<PetitionStatus>(parts[2], true, out var named))
                {
                    Logger.WriteLog(LogType.Command,
                        $"'{parts[2]}' is not a status. Use " + string.Join(", ", Enum.GetNames<PetitionStatus>()) + " or all.");
                    return;
                }

                status = named;
            }
            else if (parts.Length > 2)
                status = null;

            var limit = 20;
            if (parts.Length > 3 && !int.TryParse(parts[3], out limit))
            {
                Logger.WriteLog(LogType.Command, $"'{parts[3]}' is not a count.");
                return;
            }

            var petitions = PetitionManager.Instance.List(status, Math.Clamp(limit, 1, 200));

            if (petitions.Count == 0)
            {
                Logger.WriteLog(LogType.Command,
                    status == null ? "No petitions have been filed." : $"No {status} petitions.");
                return;
            }

            Logger.WriteLog(LogType.Command,
                $"{petitions.Count} petition(s), newest first"
                + (status == null ? ":" : $", {status}:"));

            foreach (var petition in petitions)
                Logger.WriteLog(LogType.Command,
                    $"  #{petition.Id} {(PetitionType)petition.Type} {(PetitionStatus)petition.Status} "
                    + $"account {petition.AccountId} map {petition.MapContextId} "
                    + $"{petition.CreatedAt:yyyy-MM-dd HH:mm} - {petition.Summary}");
        }

        private static void PetitionShow(string[] parts)
        {
            if (parts.Length < 3 || !uint.TryParse(parts[2], out var id))
            {
                Logger.WriteLog(LogType.Command, "Usage: petition show <id>");
                return;
            }

            var petition = PetitionManager.Instance.Get(id);

            if (petition == null)
            {
                Logger.WriteLog(LogType.Command, $"There is no petition #{id}.");
                return;
            }

            Logger.WriteLog(LogType.Command,
                $"Petition #{petition.Id} ({(PetitionType)petition.Type}, {(PetitionStatus)petition.Status})\n"
                + $"  filed    {petition.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n"
                + $"  account  {petition.AccountId}, character {petition.CharacterId}\n"
                + $"  where    map {petition.MapContextId} at "
                + $"{petition.PosX:F1}, {petition.PosY:F1}, {petition.PosZ:F1}\n"
                + $"  subject  {petition.Summary}\n"
                + (string.IsNullOrWhiteSpace(petition.Resolution) ? "" : $"  answer   {petition.Resolution}\n")
                + $"\n{petition.Body}");
        }

        private static void PetitionResolve(string[] parts)
        {
            if (parts.Length < 3 || !uint.TryParse(parts[2], out var id))
            {
                Logger.WriteLog(LogType.Command, "Usage: petition resolve <id> [what you did]");
                return;
            }

            var note = parts.Length > 3 ? string.Join(" ", parts[3..]) : string.Empty;

            if (!PetitionManager.Instance.Resolve(id, note))
            {
                var petition = PetitionManager.Instance.Get(id);

                Logger.WriteLog(LogType.Command,
                    petition == null
                        ? $"There is no petition #{id}."
                        : $"Petition #{id} is already {(PetitionStatus)petition.Status}.");
                return;
            }

            Logger.WriteLog(LogType.Command,
                $"Petition #{id} resolved" + (string.IsNullOrWhiteSpace(note) ? "." : $": {note}"));
        }

        private static void ProcessReloadCommand(string[] parts)
        {
            if (parts.Length > 1 && parts[1] == "config")
            {
                Configuration.Load();
                return;
            }

            Logger.WriteLog(LogType.Command, "Invalid reload command!");
        }

        /*private void ProcessRestartCommand(string[] parts)
        {
            // TODO: delayed restart, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
        }

        private void ProcessShutdownCommand(string[] parts)
        {
            // TODO: delayed shutdown, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
            // TODO: add timer to report the remaining time until shutdown?
            // TODO: add timer to contact global servers to tell them periodically that we're getting shut down?
        }*/
        #endregion
    }
}
