using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rasa.AuthServer
{
    using AuthConfig;
    using Commands;
    using Communicator;
    using Config;
    using Memory;
    using Networking;
    using Packets;
    using Threading;
    using Timer;

    public class Server : ILoopable
    {
        public const int MainLoopTime = 100; // Milliseconds

        public AuthConfig Config { get; private set; }
        public Communicator AuthCommunicator { get; private set; }
        public LengthedSocket ListenerSocket { get; private set; }
        public PacketQueue PacketQueue { get; }
        public List<AuthClient> Clients { get; } = new List<AuthClient>();
        public MainLoop Loop { get; }
        public Timer Timer { get; }
        public bool Running => Loop != null && Loop.Running;

        public Server()
        {
            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);
            Timer = new Timer();

            LengthedSocket.InitializeEventArgsPool(Config.SocketConfig.MaxClients * Config.SocketConfig.ConcurrentOperationsByClient);

            PacketQueue = new PacketQueue();

            BufferManager.Initialize(Config.SocketConfig.BufferSize, Config.SocketConfig.MaxClients, Config.SocketConfig.ConcurrentOperationsByClient);

            CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
            CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
        }

        ~Server()
        {
            Shutdown();
        }

        private static void ConfigReLoaded()
        {
            Logger.WriteLog(LogType.Initialize, "Config file reloaded by external change!");

            // Totally reload the configuration, because it's automatic reload case can only handle one reload. Our code's bug?
            Configuration.Load();
        }

        private void ConfigLoaded()
        {
            Config = new AuthConfig();
            Configuration.Bind(Config);

            Logger.UpdateConfig(Config.LoggerConfig);
        }

        public bool AuthenticateServer(byte id, string password)
        {
            if (Config == null || !Config.Servers.ContainsKey(id.ToString()))
                return false;

            return Config.Servers[id.ToString()] == password;
        }

        public void Disconnect(AuthClient client)
        {
            lock (Clients)
                if (Clients.Contains(client))
                    Clients.Remove(client);
        }

        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.SocketConfig.Port == 0 || Config.SocketConfig.BackLog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            Loop.Start();

            AuthCommunicator = new Communicator(Communicator.CommunicatorType.Auth, Config.CommunicatorConfig);

            ListenerSocket = new LengthedSocket(SizeType.Word);
            ListenerSocket.OnError += OnError;
            ListenerSocket.OnAccept += AcceptCompleted;
            ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.SocketConfig.Port));
            ListenerSocket.Listen(Config.SocketConfig.BackLog);

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.SocketConfig.Port);

            ListenerSocket.AcceptAsync();

            // TODO: Set up timed events (query stuff, internal communication, etc...)

            return true;
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        private void AcceptCompleted(LengthedSocket newSocket)
        {
            ListenerSocket.AcceptAsync();

            if (newSocket == null)
                return;

            lock (Clients)
                Clients.Add(new AuthClient(newSocket, this));
        }

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
