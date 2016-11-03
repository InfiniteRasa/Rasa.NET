using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Game
{
    using Commands;
    using Config;
    using Data;
    using Database;
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
        public Dictionary<uint, ClientInfo> IncomingClients { get; } = new Dictionary<uint, ClientInfo>();
        public MainLoop Loop { get; }
        public Timer Timer { get; }
        public bool Running => Loop != null && Loop.Running;
        public ushort CurrentPlayers { get; set; }

        private readonly PacketRouter<Server, CommOpcode> _router = new PacketRouter<Server, CommOpcode>();

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
                Clients.Remove(client);
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

        #region Socketing
        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.GameSocketConfig.GamePort == 0 || Config.GameSocketConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            Loop.Start();

            SetupCommunicator();

            ListenerSocket = new LengthedSocket(SizeType.Word);
            ListenerSocket.OnError += OnError;
            ListenerSocket.OnAccept += OnAccept;
            ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.GameSocketConfig.GamePort));
            ListenerSocket.Listen(Config.GameSocketConfig.Backlog);

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.GameSocketConfig.GamePort);

            ListenerSocket.AcceptAsync();

            // TODO: Set up timed events (query stuff, internal communication, etc...)

            Timer.Add("SessionExpire", 10000, true, () =>
            {
                var toRemove = new List<uint>();

                lock (IncomingClients)
                {
                    toRemove.AddRange(IncomingClients.Where(ic => ic.Value.ExpireTime < DateTime.Now).Select(ic => ic.Key));

                    foreach (var rem in toRemove)
                        IncomingClients.Remove(rem);
                }
            });

            return true;
        }

        private static void OnError(SocketAsyncEventArgs args)
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

        public bool AuthenticateClient(Client client, uint accountId, uint oneTimeKey)
        {
            lock (IncomingClients)
            {
                if (!IncomingClients.ContainsKey(accountId))
                    return false;

                var info = IncomingClients[accountId];

                IncomingClients.Remove(accountId);

                return info.OneTimeKey == oneTimeKey;
            }
        }

        public void Shutdown()
        {
            AuthCommunicator?.Close();
            AuthCommunicator = null;

            ListenerSocket?.Close();
            ListenerSocket = null;

            Loop.Stop();
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

            AuthCommunicator = new LengthedSocket(SizeType.Word);
            AuthCommunicator.OnConnect += OnCommunicatorConnect;
            AuthCommunicator.ConnectAsync(new IPEndPoint(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port));

            Logger.WriteLog(LogType.Network, $"*** Connecting to auth server! Address: {Config.CommunicatorConfig.Address}:{Config.CommunicatorConfig.Port}");
        }

        private void OnCommunicatorConnect(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Timer.Add("CommReconnect", 10000, false, () =>
                {
                    if (!AuthCommunicator.Connected)
                        ConnectCommunicator();
                });

                Logger.WriteLog(LogType.Error, "Could not connect to the Auth server! Trying again in a few seconds...");
                return;
            }

            Logger.WriteLog(LogType.Network, "*** Connected to the Auth Server!");

            AuthCommunicator.OnReceive += OnCommunicatorReceive;
            AuthCommunicator.Send(new LoginRequestPacket
            {
                ServerId = Config.ServerInfoConfig.Id,
                Password = Config.ServerInfoConfig.Password
            }, null);

            AuthCommunicator.ReceiveAsync();
        }

        private void OnCommunicatorReceive(BufferData data)
        {
            var opcode = (CommOpcode) data.Buffer[data.BaseOffset + data.Offset++];

            var packetType = _router.GetPacketType(opcode);
            if (packetType == null)
                return;

            var packet = Activator.CreateInstance(packetType) as IOpcodedPacket<CommOpcode>;
            if (packet == null)
                return;

            packet.Read(data.GetReader());

            _router.RoutePacket(this, packet);
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.LoginResponse)]
        private void MsgLoginResponse(LoginResponsePacket packet)
        {
            if (packet.Response == CommLoginReason.Success)
                return;

            AuthCommunicator?.Close();
            AuthCommunicator = null;

            Logger.WriteLog(LogType.Error, "Could not authenticate with the Auth server! Shutting down internal communication!");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [PacketHandler(CommOpcode.ServerInfoRequest)]
        private void MsgGameInfoRequest(ServerInfoRequestPacket packet)
        {
            AuthCommunicator.Send(new ServerInfoResponsePacket
            {
                AgeLimit = Config.ServerInfoConfig.AgeLimit,
                PKFlag = Config.ServerInfoConfig.PKFlag,
                CurrentPlayers = CurrentPlayers,
                GamePort = Config.GameSocketConfig.GamePort,
                QueuePort = Config.GameSocketConfig.QueuePort,
                MaxPlayers = (ushort) Config.SocketAsyncConfig.MaxClients
            }, null);
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.RedirectRequest)]
        private void MsgRedirectRequest(RedirectRequestPacket packet)
        {
            lock (IncomingClients)
            {
                if (IncomingClients.ContainsKey(packet.AccountId))
                    IncomingClients.Remove(packet.AccountId);

                IncomingClients.Add(packet.AccountId, new ClientInfo
                {
                    AccountId = packet.AccountId,
                    OneTimeKey = packet.OneTimeKey,
                    ExpireTime = DateTime.Now.AddMinutes(1)
                });
            }

            AuthCommunicator.Send(new RedirectResponsePacket
            {
                AccountId = packet.AccountId,
                Response = CurrentPlayers >= Config.ServerInfoConfig.MaxPlayers ? RedirectResult.Queue : RedirectResult.Success
            }, null);
        }
        #endregion

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
