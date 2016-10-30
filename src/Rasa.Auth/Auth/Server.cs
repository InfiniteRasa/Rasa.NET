using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using Rasa.Packets.Server;

namespace Rasa.Auth
{
    using Commands;
    using Config;
    using Data;
    using Database;
    using Database.Tables;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Communicator;
    using Structures;
    using Threading;
    using Timer;

    public class Server : ILoopable
    {
        public const int MainLoopTime = 100; // Milliseconds

        public Config Config { get; private set; }
        public LengthedSocket AuthCommunicator { get; private set; }
        
        public LengthedSocket ListenerSocket { get; private set; }
        public PacketQueue PacketQueue { get; }
        public List<Client> Clients { get; } = new List<Client>();
        public List<ServerInfo> ServerList { get; } = new List<ServerInfo>();
        public MainLoop Loop { get; }
        public Timer Timer { get; }
        public bool Running => Loop != null && Loop.Running;

        private List<CommunicatorClient> GameServerQueue { get; } = new List<CommunicatorClient>();
        private Dictionary<byte, CommunicatorClient> GameServers { get; } = new Dictionary<byte, CommunicatorClient>();

        public Server()
        {
            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);
            Timer = new Timer();

            LengthedSocket.InitializeEventArgsPool(Config.SocketAsyncConfig.MaxClients * Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            PacketQueue = new PacketQueue();

            BufferManager.Initialize(Config.SocketAsyncConfig.BufferSize, Config.SocketAsyncConfig.MaxClients, Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            DatabaseAccess.Initialize(Config.DatabaseConnectionString);

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
            Config = new Config();
            Configuration.Bind(Config);

            Logger.UpdateConfig(Config.LoggerConfig);
        }
        #endregion

        public void Disconnect(Client client)
        {
            lock (Clients)
                if (Clients.Contains(client))
                    Clients.Remove(client);
        }

        #region Socketing
        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.AuthSocketConfig.Port == 0 || Config.AuthSocketConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            Loop.Start();

            SetupCommunicator();

            ListenerSocket = new LengthedSocket(SizeType.Word);
            ListenerSocket.OnError += OnError;
            ListenerSocket.OnAccept += OnAccept;
            ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.AuthSocketConfig.Port));
            ListenerSocket.Listen(Config.AuthSocketConfig.Backlog);

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.AuthSocketConfig.Port);

            ListenerSocket.AcceptAsync();

            // TODO: Set up timed events (query stuff, internal communication, etc...)

            return true;
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
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
        private void SetupCommunicator()
        {
            if (Config.CommunicatorConfig.Port == 0 || Config.CommunicatorConfig.Address == null || Config.CommunicatorConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid Communicator config data! Can't connect!");
                return;
            }

            AuthCommunicator = new LengthedSocket(SizeType.Word);
            AuthCommunicator.OnAccept += OnCommunicatorAccept;
            AuthCommunicator.Bind(new IPEndPoint(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port));
            AuthCommunicator.Listen(Config.CommunicatorConfig.Backlog);

            AuthCommunicator.AcceptAsync();

            Timer.Add("ServerInfoUpdate", 1000, true, () =>
            {
                lock (GameServers)
                    foreach (var server in GameServers)
                        if ((DateTime.Now - server.Value.LastUpdateTime).Milliseconds > 30000)
                            server.Value.RequestServerInfo();
            });

            Logger.WriteLog(LogType.Network, $"*** Listening for Game servers on port {Config.CommunicatorConfig.Port}");
        }

        private void OnCommunicatorAccept(LengthedSocket socket)
        {
            AuthCommunicator.AcceptAsync();

            GameServerQueue.Add(new CommunicatorClient(socket, this));
        }

        public bool AuthenticateGameServer(byte serverId, string password, CommunicatorClient client)
        {
            lock (GameServers)
            {
                if (GameServers.ContainsKey(serverId))
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Debug, $"A server tried to connect to an already in use server slot! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                if (!Config.Servers.ContainsKey(serverId.ToString()))
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Debug, $"A server tried to connect to a non-defined server slot! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                if (Config.Servers[serverId.ToString()] != password)
                {
                    DisconnectCommunicator(client);
                    Logger.WriteLog(LogType.Error, $"A server tried to log in with an invalid password! Remote Address: {client.Socket.RemoteAddress}");
                    return false;
                }

                GameServerQueue.Remove(client);
                GameServers.Add(serverId, client);

                client.RequestServerInfo();

                return true;
            }
        }

        public void UpdateServerInfo(CommunicatorClient client, ServerInfoResponsePacket packet)
        {
            GenerateServerList();
            BroadcastServerList();
        }

        public void DisconnectCommunicator(CommunicatorClient client) // todo: clear server list on disconnect
        {
            lock (GameServerQueue)
                GameServerQueue.Remove(client);

            if (client.ServerId != 0)
                lock (GameServers)
                    GameServers.Remove(client.ServerId);

            Timer.Add($"Disconnect-comm-{DateTime.Now.Ticks}", 1000, false, () =>
            {
                client?.Socket?.Close();
            });

            GenerateServerList();
        }

        private void GenerateServerList()
        {
            lock (GameServers)
            {
                lock (ServerList)
                {
                    ServerList.Clear(); // only show the currently online servers, tbd: show them as offline if they were successfully connected to the auth earlier?

                    foreach (var server in GameServers)
                    {
                        ServerList.Add(new ServerInfo
                        {
                            AgeLimit = server.Value.AgeLimit,
                            PKFlag = server.Value.PKFlag,
                            CurrentPlayers = server.Value.CurrentPlayers,
                            MaxPlayers = server.Value.MaxPlayers,
                            QueuePort = server.Value.QueuePort,
                            GamePort = server.Value.GamePort,
                            Ip = server.Value.Socket.RemoteAddress,
                            ServerId = server.Key,
                            Status = 1
                        });
                    }
                }
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
            QueuedPacket packet;

            while ((packet = PacketQueue.PopIncoming()) != null)
                packet.Client.HandlePacket(packet.Packet);

            Timer.Update(delta);

            while ((packet = PacketQueue.PopOutgoing()) != null)
                packet.Client.SendPacket(packet.Packet);
        }

        public RedirectResult RedirectToGlobal(Client client, byte serverId, out ServerInfo info)
        {
            // TODO: comm. with global server, get if the client should be queued or directly connected
            info = null;
            return RedirectResult.Fail;
        }

        public void BroadcastServerList()
        {
            foreach (var c in Clients)
                if (c.State == ClientState.ServerList)
                    c.SendPacket(new SendServerListExtPacket(ServerList, c.Entry.LastServerId));
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

                Environment.Exit(0);
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
                Logger.WriteLog(LogType.Command, "Invalid create account command! Usage: create <username> <email> <password>");
                return;
            }

            var salt = new byte[20];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            var data = new AccountEntry
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

                Logger.WriteLog(LogType.Command, $"Created account: {parts[1]}! (Password: {parts[3]})");
            }
            catch
            {
                Logger.WriteLog(LogType.Error, "Username or email is already taken!");
            }
        }

        private void ProcessRestartCommand(string[] parts)
        {
            // TODO: delayed restart, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
        }

        private void ProcessShutdownCommand(string[] parts)
        {
            // TODO: delayed shutdown, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
            // TODO: add timer to report the remaining time until shutdown?
            // TODO: add timer to contact global servers to tell them periodically that we're getting shut down?
        }
        #endregion
    }
}
