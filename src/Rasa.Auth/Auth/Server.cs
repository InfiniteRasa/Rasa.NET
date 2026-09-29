using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Hosting;

namespace Rasa.Auth
{
    using Commands;
    using Config;
    using Data;
    using Hosting;
    using Memory;
    using Networking;
    using Packets.Communicator;
    using Packets.Auth.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Auth;
    using Threading;
    using Timer;

    public class Server : ILoopable, IRasaServer
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IAuthUnitOfWorkFactory _authUnitOfWorkFactory;

        public string ServerType { get; } = "Authentication";

        public const int MainLoopTime = 100; // Milliseconds

        public Config Config { get; private set; }
        public LengthedSocket AuthCommunicator { get; private set; }
        public LengthedSocket ListenerSocket { get; private set; }
        public List<Client> Clients { get; } = new List<Client>();
       
        public List<ServerInfo> ServerList { get; } = new List<ServerInfo>();
        public MainLoop Loop { get; }
        public Timer Timer { get; }
        public bool Running => Loop != null && Loop.Running;

        private readonly List<Client> _clientsToRemove = new List<Client>();
        private List<CommunicatorClient> GameServerQueue { get; } = new List<CommunicatorClient>();
        private Dictionary<byte, CommunicatorClient> GameServers { get; } = new Dictionary<byte, CommunicatorClient>();

        public Server(IHostApplicationLifetime hostApplicationLifetime, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _authUnitOfWorkFactory = authUnitOfWorkFactory;

            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);
            Timer = new Timer();

            SetupServerList();

            LengthedSocket.InitializeEventArgsPool(Config.SocketAsyncConfig.MaxClients * Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            BufferManager.Initialize(Config.SocketAsyncConfig.BufferSize, Config.SocketAsyncConfig.MaxClients, Config.SocketAsyncConfig.ConcurrentOperationsByClient);
            
            CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
            CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
            CommandProcessor.RegisterCommand("create", ProcessCreateCommand);
            CommandProcessor.RegisterCommand("ban", parts => ProcessLockCommand(parts, true));
            CommandProcessor.RegisterCommand("unban", parts => ProcessLockCommand(parts, false));
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
            var oldConfig = Config;

            // Built and bound before it replaces the one in force: a reload runs on the file
            // watcher's thread, and anything reading Config halfway through a Bind saw a new,
            // empty Config with every value zero.
            var config = new Config();
            Configuration.Bind(config);
            Config = config;

            Logger.UpdateConfig(Config.LoggerConfig);

            // Handle reloading the config and updating the list visibility
            if (oldConfig == null || oldConfig.AuthListType == Config.AuthListType)
                return;

            lock (ServerList)
            {
                ServerList.Clear();
                SetupServerList();
                GenerateServerList();
            }
        }
        #endregion

        public void Disconnect(Client client)
        {
            lock (_clientsToRemove)
                _clientsToRemove.Add(client);
        }

        private void SetupServerList()
        {
            if (Config.AuthListType != AuthListType.All)
                return;

            foreach (var s in Config.Servers)
            {
                if (!byte.TryParse(s.Key, out byte id))
                    continue;

                ServerList.Add(new ServerInfo
                {
                    AgeLimit = 0,
                    CurrentPlayers = 0,
                    GamePort = 0,
                    Ip = IPAddress.None,
                    MaxPlayers = 0,
                    PKFlag = 0,
                    QueuePort = 0,
                    ServerId = id,
                    Status = 0
                });
            }
        }

        #region Socketing
        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.AuthConfig.Port == 0 || Config.AuthConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            try
            {
                ListenerSocket = new LengthedSocket(SizeType.Word);
                ListenerSocket.OnError += OnError;
                ListenerSocket.OnAccept += OnAccept;
                ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.AuthConfig.Port));
                ListenerSocket.Listen(Config.AuthConfig.Backlog);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the client socket! Exception:");
                Logger.WriteLog(LogType.Error, e);

                return false;
            }

            Loop.Start();

            if (!SetupCommunicator())
                return false;

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.AuthConfig.Port);

            ListenerSocket.AcceptAsync();

            // TODO: Set up timed events (query stuff, internal communication, etc...)

            return true;
        }

        private static void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null &&
                args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        /// <summary>
        /// Auth connections allowed from one address at once. Each login is its own short
        /// connection, and nothing capped how many one address could hold open.
        /// </summary>
        private const int MaxConnectionsPerAddress = 16;

        private long _nextAcceptRefusalLogTick;
        private int _acceptRefusalsSinceLog;

        /// <summary>
        /// Failed logins per address. Every attempt is a database read and a hash on this loop,
        /// and nothing slowed a guesser down: one connection per guess, as fast as they could open
        /// them. After MaxLoginFailures within LoginFailureWindow the address is refused without a
        /// look at the database for LoginBlock. Kept per address rather than per account, so a
        /// stranger cannot lock someone else out by getting their password wrong.
        /// Main loop only: logins are handled there.
        /// </summary>
        private readonly Dictionary<IPAddress, (int Count, DateTime WindowStart, DateTime BlockedUntil)> _loginFailures = new();

        private const int MaxLoginFailures = 5;
        private static readonly TimeSpan LoginFailureWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LoginBlock = TimeSpan.FromMinutes(5);

        public bool IsLoginBlocked(IPAddress address)
        {
            return _loginFailures.TryGetValue(address, out var record) && record.BlockedUntil > DateTime.UtcNow;
        }

        public void RecordLoginFailure(IPAddress address)
        {
            var now = DateTime.UtcNow;

            // Forget addresses whose window and block are both over, so the table stays the size
            // of the current trouble rather than of every address that ever mistyped.
            if (_loginFailures.Count > 1024)
                foreach (var stale in _loginFailures.Where(f => f.Value.BlockedUntil < now && now - f.Value.WindowStart > LoginFailureWindow).Select(f => f.Key).ToList())
                    _loginFailures.Remove(stale);

            var (count, windowStart, blockedUntil) = _loginFailures.TryGetValue(address, out var record)
                ? record
                : (0, now, DateTime.MinValue);

            if (now - windowStart > LoginFailureWindow)
                (count, windowStart) = (0, now);

            count++;

            if (count >= MaxLoginFailures && blockedUntil < now)
            {
                blockedUntil = now + LoginBlock;
                (count, windowStart) = (0, now);

                Logger.WriteLog(LogType.Security, $"{MaxLoginFailures} failed logins from {address} within {LoginFailureWindow.TotalMinutes:F0} min; refusing its logins for {LoginBlock.TotalMinutes:F0} min.");
            }

            _loginFailures[address] = (count, windowStart, blockedUntil);
        }

        public void RecordLoginSuccess(IPAddress address)
        {
            _loginFailures.Remove(address);
        }

        private void OnAccept(LengthedSocket newSocket)
        {
            ListenerSocket.AcceptAsync();

            if (newSocket == null)
                return;

            var address = newSocket.RemoteAddress;

            lock (Clients)
            {
                if (Clients.Count(c => c.State != ClientState.Disconnected && address.Equals(c.Socket.RemoteAddress)) < MaxConnectionsPerAddress)
                {
                    Clients.Add(new Client(newSocket, this, _authUnitOfWorkFactory));
                    return;
                }
            }

            newSocket.Close();

            var refused = System.Threading.Interlocked.Increment(ref _acceptRefusalsSinceLog);
            var now = Environment.TickCount64;

            if (now < System.Threading.Interlocked.Read(ref _nextAcceptRefusalLogTick))
                return;

            System.Threading.Interlocked.Exchange(ref _nextAcceptRefusalLogTick, now + 5000);
            System.Threading.Interlocked.Exchange(ref _acceptRefusalsSinceLog, 0);

            Logger.WriteLog(LogType.Security, $"Refused an auth connection from {address}: {MaxConnectionsPerAddress} already open from it ({refused} refused since the last of these).");
        }
        #endregion

        #region Communicator
        private bool SetupCommunicator()
        {
            if (Config.CommunicatorConfig.Port == 0 || Config.CommunicatorConfig.Address == null || Config.CommunicatorConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid Communicator config data! Can't connect!");
                return false;
            }

            try
            {
                AuthCommunicator = new LengthedSocket(SizeType.Word);
                AuthCommunicator.OnAccept += OnCommunicatorAccept;
                AuthCommunicator.Bind(new IPEndPoint(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port));
                AuthCommunicator.Listen(Config.CommunicatorConfig.Backlog);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the communicator socket! Exception:");
                Logger.WriteLog(LogType.Error, e);

                return false;
            }

            WarnAboutCommunicatorExposure();

            AuthCommunicator.AcceptAsync();

            Timer.Add("ServerInfoUpdate", 1000, true, () =>
            {
                lock (GameServers)
                    foreach (var server in GameServers)
                        if ((DateTime.Now - server.Value.LastRequestTime).TotalMilliseconds > 30000)
                            server.Value.RequestServerInfo();
            });

            Timer.Add("CommunicatorLoginExpire", 5000, true, ExpireCommunicatorLogins);

            Logger.WriteLog(LogType.Network, $"*** Listening for Game servers on port {Config.CommunicatorConfig.Port}");

            return true;
        }

        /// <summary>
        /// The communicator port hands whoever logs in on it every player's world login key. The
        /// only thing guarding it is the per-server password, so say so at startup when it is
        /// listening beyond this machine with a password anyone could guess.
        /// </summary>
        private void WarnAboutCommunicatorExposure()
        {
            var bound = IPAddress.Parse(Config.CommunicatorConfig.Address);

            if (IPAddress.IsLoopback(bound))
                return;

            var weak = (Config.Servers ?? new Dictionary<string, string>())
                .Where(s => string.IsNullOrEmpty(s.Value) || s.Value.Length < 12 || s.Value.StartsWith("test", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Key)
                .ToList();

            if (weak.Count == 0)
                return;

            Logger.WriteLog(LogType.Security,
                $"The communicator listens on {bound}:{Config.CommunicatorConfig.Port}, beyond this machine, and server "
                + $"slot(s) {string.Join(", ", weak)} have a default, empty or short password. Anyone who can reach that port "
                + "with the password can register as a game server and receive players' login keys. Set long passwords in "
                + "Servers (and the matching ServerInfoConfig.Password on each game server), or bind the communicator to "
                + "127.0.0.1 when auth and game run on the same machine, or firewall the port.");
        }

        /// <summary>How long a connection to the communicator port has to log in as a game server.</summary>
        private static readonly TimeSpan CommunicatorLoginTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Closes communicator connections that have not logged in within CommunicatorLoginTimeout.
        ///
        /// A connection sat in GameServerQueue until it sent a LoginRequest or its socket failed.
        /// A game server logs in the moment it connects, so anything still waiting after that is
        /// not one - a port scanner, a health check, a misconfigured server - and each held a
        /// pooled receive buffer and its args for as long as it stayed open. Run from the auth
        /// loop's timer; the closes happen outside the GameServers lock, because closing goes
        /// through DisconnectCommunicator, which regenerates the server list.
        /// </summary>
        private void ExpireCommunicatorLogins()
        {
            List<CommunicatorClient> stale;
            var cutoff = DateTime.UtcNow - CommunicatorLoginTimeout;

            lock (GameServers)
            {
                // A connection that has logged in is a game server, however it got into the
                // queue; it goes out of the queue, not off the link. One already let go (a login
                // refused before its accept was handled) has nothing left to close.
                GameServerQueue.RemoveAll(c => c.ServerId != 0 || c.IsDisconnected);

                stale = GameServerQueue.Where(c => c.ConnectedTime < cutoff).ToList();
            }

            if (stale.Count == 0)
                return;

            Logger.WriteLog(LogType.Network, $"Closing {stale.Count} communicator connection(s) that did not log in as a game server within {CommunicatorLoginTimeout.TotalSeconds:F0} s.");

            foreach (var client in stale)
                client.Disconnect();
        }

        private void OnCommunicatorAccept(LengthedSocket socket)
        {
            AuthCommunicator.AcceptAsync();

            Logger.WriteLog(LogType.Network, $"A Game server has connected! Remote: {socket.RemoteAddress}");

            // The client starts receiving in its constructor, and a game server sends its login the
            // moment it connects, so the login can be handled on a completion thread before this
            // line runs - it used to be, every time on loopback ("has authenticated!" logged before
            // "has connected!"). AuthenticateGameServer then found nothing to take out of the queue,
            // the connection was added to it here afterwards as if still waiting, and 30 s later
            // ExpireCommunicatorLogins closed the live game server. Both sides decide under the
            // GameServers lock: a connection that has already claimed a server id, or has already
            // been refused and let go, is not queued.
            var client = new CommunicatorClient(socket, this);

            lock (GameServers)
                if (client.ServerId == 0 && !client.IsDisconnected)
                    GameServerQueue.Add(client);
        }

        public bool AuthenticateGameServer(LoginRequestPacket packet, CommunicatorClient client)
        {
            // Lock order everywhere in this class is Clients -> ServerList -> GameServers, so
            // nothing that holds GameServers may go on to regenerate the server list.
            // DisconnectCommunicator does exactly that, which is why the rejections are decided
            // under the lock and carried out after it.
            string rejection;
            LogType rejectionLogType;

            lock (GameServers)
            {
                if (client.ServerId != 0)
                {
                    // One login per connection. A second one naming another id would have left
                    // the first slot pointing at this connection with nothing to remove it.
                    rejection = $"Game server {client.ServerId} tried to log in again, as {packet.ServerId}!";
                    rejectionLogType = LogType.Security;
                }
                else if (packet.ServerId == 0)
                {
                    // 0 means "not logged in" everywhere in CommunicatorClient.
                    rejection = "A server tried to connect to server slot 0!";
                    rejectionLogType = LogType.Security;
                }
                else if (GameServers.ContainsKey(packet.ServerId))
                {
                    rejection = "A server tried to connect to an already in use server slot!";
                    rejectionLogType = LogType.Debug;
                }
                else if (!Config.Servers.ContainsKey(packet.ServerId.ToString()))
                {
                    rejection = "A server tried to connect to a non-defined server slot!";
                    rejectionLogType = LogType.Debug;
                }
                else if (!PasswordMatches(Config.Servers[packet.ServerId.ToString()], packet.Password))
                {
                    rejection = "A server tried to log in with an invalid password!";
                    rejectionLogType = LogType.Security;
                }
                else
                {
                    // Taken on only now, in the same locked step that claims the slot, so there
                    // is no moment where the connection holds the slot without the id that
                    // DisconnectCommunicator gives it back by.
                    client.ServerId = packet.ServerId;
                    client.PublicAddress = packet.PublicAddress;

                    GameServerQueue.Remove(client);
                    GameServers.Add(packet.ServerId, client);

                    Logger.WriteLog(LogType.Network, $"The Game server (Id: {packet.ServerId}, Address: {client.Socket.RemoteAddress}, Public Address: {packet.PublicAddress}) has authenticated! Requesting info...");

                    return true;
                }
            }

            DisconnectCommunicator(client);
            Logger.WriteLog(rejectionLogType, $"{rejection} Remote Address: {client.Socket.RemoteAddress}");

            return false;
        }

        /// <summary>
        /// The configured password against the one offered, in time that does not depend on how
        /// much of it matched. An empty configured password matches nothing.
        /// </summary>
        private static bool PasswordMatches(string expected, string offered)
        {
            if (string.IsNullOrEmpty(expected) || offered == null)
                return false;

            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
            var offeredHash = SHA256.HashData(Encoding.UTF8.GetBytes(offered));

            return CryptographicOperations.FixedTimeEquals(expectedHash, offeredHash);
        }

        public void UpdateServerInfo(CommunicatorClient client, ServerInfoResponsePacket packet)
        {
            GenerateServerList();
            BroadcastServerList();
        }

        public void RedirectResponse(CommunicatorClient client, RedirectResponsePacket packet)
        {
            Client authClient;
            lock (Clients)
                // AccountEntry is null on every connection still at the login screen, and this
                // runs on the communicator thread whenever a game server answers a redirect.
                authClient = Clients.FirstOrDefault(c => c.AccountEntry != null && c.AccountEntry.Id == packet.AccountId);

            ServerInfo info;
            lock (ServerList)
                info = ServerList.FirstOrDefault(i => i.ServerId == client.ServerId);

            if (authClient != null && info != null)
                authClient.RedirectionResult(packet.Response, info);
        }

        public void RequestRedirection(Client client, byte serverId)
        {
            lock (GameServers)
                if (GameServers.ContainsKey(serverId))
                    GameServers[serverId].RequestRedirection(client);
        }

        public void DisconnectCommunicator(CommunicatorClient client)
        {
            if (client == null)
                return;

            lock (GameServers)
            {
                GameServerQueue.Remove(client);

                // Only this connection's own entry. Removing by id alone took out whichever
                // server held that slot - the live one, when a rejected login carried its id.
                if (client.ServerId != 0 && GameServers.TryGetValue(client.ServerId, out var registered) && registered == client)
                    GameServers.Remove(client.ServerId);
            }

            // Outside the GameServers lock: GenerateServerList takes ServerList and then
            // GameServers, and calling it from inside GameServers inverted that order against
            // UpdateServerInfo, so one game server dropping while another reported its info
            // could deadlock both communicator threads for good.
            GenerateServerList();

            Timer.Add($"Disconnect-comm-{DateTime.Now.Ticks}", 1000, false, () =>
            {
                client.Socket?.Close();
            });

            Logger.WriteLog(LogType.Network, $"The game server (Id: {client.ServerId}, Address: {client.Socket.RemoteAddress}) has disconnected!");
        }

        private void GenerateServerList()
        {
            lock (ServerList)
            {
                var toRemove = new List<ServerInfo>();

                lock (GameServers)
                {
                    foreach (var sInfo in ServerList)
                    {
                        if (GameServers.TryGetValue(sInfo.ServerId, out CommunicatorClient client))
                        {
                            sInfo.Setup(client.PublicAddress, client.QueuePort, client.GamePort, client.AgeLimit, client.PKFlag, client.CurrentPlayers, client.MaxPlayers);
                            continue;
                        }

                        if (Config.AuthListType == AuthListType.Online)
                        {
                            toRemove.Add(sInfo);
                            continue;
                        }

                        sInfo.Clear();
                    }

                    foreach (var server in GameServers)
                    {
                        if (ServerList.All(s => s.ServerId != server.Key))
                        {
                            ServerList.Add(new ServerInfo
                            {
                                AgeLimit = server.Value.AgeLimit,
                                PKFlag = server.Value.PKFlag,
                                CurrentPlayers = server.Value.CurrentPlayers,
                                MaxPlayers = server.Value.MaxPlayers,
                                QueuePort = server.Value.QueuePort,
                                GamePort = server.Value.GamePort,
                                Ip = server.Value.PublicAddress,
                                ServerId = server.Key,
                                Status = 1
                            });
                        }
                    }
                }

                if (toRemove.Count == 0)
                    return;

                foreach (var rem in toRemove)
                    ServerList.Remove(rem);
            }
        }
        #endregion

        public void Shutdown()
        {
            ListenerSocket?.Close();
            ListenerSocket = null;

            Loop.Stop();
        }

        public void MainLoop(long delta)
        {
            // Same rule as the game server: this is the only thread, and a fault in one
            // connection must not cost the tick for the connections after it in the list.
            try
            {
                Timer.Update(delta);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error updating the auth server timers: {e}");
            }

            if (Clients.Count == 0)
                return;

            lock (Clients)
            {
                foreach (var c in Clients)
                {
                    try
                    {
                        c.Update(delta);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Error updating auth client {c.Socket?.RemoteAddress}, disconnecting it: {e}");

                        try
                        {
                            c.Close();
                        }
                        catch (Exception inner)
                        {
                            Logger.WriteLog(LogType.Error, $"And closing it threw as well: {inner}");
                        }
                    }
                }

                if (_clientsToRemove.Count > 0)
                {
                    lock (_clientsToRemove)
                    {
                        foreach (var client in _clientsToRemove)
                            Clients.Remove(client);

                        _clientsToRemove.Clear();
                    }
                }
            }
        }

        public void BroadcastServerList()
        {
            var servers = GetServerListSnapshot();

            lock (Clients)
                foreach (var c in Clients)
                    if (c.State == ClientState.ServerList)
                        c.SendPacket(new SendServerListExtPacket(servers, c.AccountEntry.LastServerId));
        }

        /// <summary>
        /// A copy of the server list to serialize from. Send writes the packet on the calling
        /// thread, and the list is rebuilt on the communicator threads, so handing the live
        /// list to a packet let the main loop enumerate it while GenerateServerList changed it.
        /// </summary>
        public List<ServerInfo> GetServerListSnapshot()
        {
            lock (ServerList)
                return ServerList.Select(s => s.Copy()).ToList();
        }

        #region Commands
        private void ProcessExitCommand(string[] parts)
        {
            var minutes = 0;

            if (parts.Length > 1)
                minutes = int.Parse(parts[1]);

            Timer.Add("exit", minutes * 60000, false, () =>
            {
                Shutdown();
                _hostApplicationLifetime.StopApplication();
            });

            Logger.WriteLog(LogType.Command, $"Exiting the server in {minutes} minute(s).");
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

        /// <summary>
        /// ban &lt;username&gt; / unban &lt;username&gt;. Sets account.locked, which GetByUserName
        /// already refuses at login, then deals with anyone already past that check: the player's
        /// auth connection (server list screen) is closed here, and every connected game server is
        /// told so it can kick them from the queue or world and refuse a pending handoff.
        /// </summary>
        private void ProcessLockCommand(string[] parts, bool locked)
        {
            var command = locked ? "ban" : "unban";

            if (parts.Length < 2)
            {
                Logger.WriteLog(LogType.Command, $"Invalid {command} command! Usage: {command} <username>");
                return;
            }

            AuthAccountEntry account;

            try
            {
                using var unitOfWork = _authUnitOfWorkFactory.Create();
                account = unitOfWork.AuthAccountRepository.SetLocked(parts[1], locked);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not {command} {parts[1]}: {e.Message}");
                return;
            }

            if (account == null)
            {
                Logger.WriteLog(LogType.Command, $"No account with username {parts[1]}.");
                return;
            }

            if (locked)
            {
                List<Client> connected;

                // AccountEntry is null until a client has logged in.
                lock (Clients)
                    connected = Clients.Where(c => c.AccountEntry != null && c.AccountEntry.Id == account.Id).ToList();

                foreach (var client in connected)
                    client.Close();
            }

            var notice = new AccountLockChangedPacket { AccountId = account.Id, Locked = locked };
            var notified = 0;

            lock (GameServers)
                foreach (var server in GameServers.Values)
                {
                    if (!server.Connected)
                        continue;

                    server.Socket.Send(notice);
                    notified++;
                }

            Logger.WriteLog(LogType.Command, $"{(locked ? "Banned" : "Unbanned")} account {account.Username} ({account.Id}); notified {notified} game server(s).");
        }

        private void ProcessCreateCommand(string[] parts)
        {
            if (parts.Length < 4)
            {
                Logger.WriteLog(LogType.Command, "Invalid create account command! Usage: create <email> <username> <password>");
                return;
            }

            var email = parts[1];
            var userName = parts[2];
            var password = parts[3];

            try
            {
                using var unitOfWork = _authUnitOfWorkFactory.Create();
                unitOfWork.AuthAccountRepository.Create(email, userName, password);
                unitOfWork.Complete();

                Logger.WriteLog(LogType.Command, $"Created account: {parts[2]}! (Password: {parts[3]})");
            }
            catch
            {
                Logger.WriteLog(LogType.Error, "Username or email is already taken!");
            }
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
