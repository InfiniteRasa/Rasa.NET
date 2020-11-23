﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Rasa.Auth
{
    using System.Threading;
    using Commands;
    using Config;
    using Data;
    using Database;
    using Database.Tables.Auth;
    using Hosting;
    using Memory;
    using Networking;
    using Packets.Communicator;
    using Packets.Auth.Server;
    using Structures;
    using Threading;
    using Timer = Timer.Timer;

    public class Server : ILoopable, IRasaServer
    {
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

        private CancellationTokenSource _stopTokenSource;

        public Server()
        {
            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);
            Timer = new Timer();

            SetupServerList();

            LengthedSocket.InitializeEventArgsPool(Config.SocketAsyncConfig.MaxClients * Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            BufferManager.Initialize(Config.SocketAsyncConfig.BufferSize, Config.SocketAsyncConfig.MaxClients, Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            AuthDatabaseAccess.Initialize(Config.DatabaseConnectionString);

            CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
            CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
            CommandProcessor.RegisterCommand("create", ProcessCreateCommand);
        }

        ~Server()
        {
            Shutdown();
        }

        #region Configuration
        private static void ConfigReLoaded()
        {
            Logger.WriteLog(LogType.Initialize, "Config file reloaded by external change!");

            // Totally reload the configuration, because it's automatic reload case can only handle one reload. Our code's bug?
            Configuration.Load();
        }

        private void ConfigLoaded()
        {
            var oldConfig = Config;

            Config = new Config();
            Configuration.Bind(Config);

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
        public bool Start(CancellationTokenSource stopToken)
        {
            _stopTokenSource = stopToken;
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

        private void OnAccept(LengthedSocket newSocket)
        {
            ListenerSocket.AcceptAsync();

            if (newSocket == null)
                return;

            lock (Clients)
                Clients.Add(new Client(newSocket, this));
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

            AuthCommunicator.AcceptAsync();

            Timer.Add("ServerInfoUpdate", 1000, true, () =>
            {
                lock (GameServers)
                    foreach (var server in GameServers)
                        if ((DateTime.Now - server.Value.LastRequestTime).TotalMilliseconds > 30000)
                            server.Value.RequestServerInfo();
            });

            Logger.WriteLog(LogType.Network, $"*** Listening for Game servers on port {Config.CommunicatorConfig.Port}");

            return true;
        }

        private void OnCommunicatorAccept(LengthedSocket socket)
        {
            AuthCommunicator.AcceptAsync();

            lock (GameServers)
                GameServerQueue.Add(new CommunicatorClient(socket, this));

            Logger.WriteLog(LogType.Network, $"A Game server has connected! Remote: {socket.RemoteAddress}");
        }

        public bool AuthenticateGameServer(LoginRequestPacket packet, CommunicatorClient client)
        {
            lock (GameServers)
            {
                if (GameServers.ContainsKey(packet.ServerId))
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Debug, $"A server tried to connect to an already in use server slot! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                if (!Config.Servers.ContainsKey(packet.ServerId.ToString()))
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Debug, $"A server tried to connect to a non-defined server slot! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                if (Config.Servers[packet.ServerId.ToString()] != packet.Password)
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Error, $"A server tried to log in with an invalid password! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                GameServerQueue.Remove(client);
                GameServers.Add(packet.ServerId, client);

                Logger.WriteLog(LogType.Network, $"The Game server (Id: {packet.ServerId}, Address: {client.Socket.RemoteAddress}, Public Address: {packet.PublicAddress}) has authenticated! Requesting info...");

                return true;
            }
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
                authClient = Clients.FirstOrDefault(c => c.AccountEntry.Id == packet.AccountId);

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

                if (client.ServerId != 0)
                    GameServers.Remove(client.ServerId);

                GenerateServerList();
            }

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
            Timer.Update(delta);

            if (Clients.Count == 0)
                return;

            lock (Clients)
            {
                foreach (var c in Clients)
                    c.Update(delta);

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
            lock (Clients)
                foreach (var c in Clients)
                    if (c.State == ClientState.ServerList)
                        c.SendPacket(new SendServerListExtPacket(ServerList, c.AccountEntry.LastServerId));
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

                _stopTokenSource.Cancel(false);
                //Environment.Exit(0);
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

        private static void ProcessCreateCommand(string[] parts)
        {
            if (parts.Length < 4)
            {
                Logger.WriteLog(LogType.Command, "Invalid create account command! Usage: create <email> <username> <password>");
                return;
            }

            var salt = new byte[20];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            var data = new AuthAccountEntry
            {
                Email = parts[1],
                Username = parts[2],
                Password = parts[3],
                Salt = BitConverter.ToString(salt).Replace("-", "").ToLower()
            };

            data.HashPassword();

            try
            {
                AccountTable.InsertAccount(data);

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
