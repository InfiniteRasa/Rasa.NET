using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Microsoft.Extensions.Hosting;

namespace Rasa.Auth;

using Rasa.Commands;
using Rasa.Communicator;
using Rasa.Communicator.Packets;
using Rasa.Config;
using Rasa.Data;
using Rasa.Hosting;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets.Auth.Server;
using Rasa.Repositories.UnitOfWork;
using Rasa.Threading;
using Rasa.Timer;

public class Server : ILoopable, IRasaServer
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IAuthUnitOfWorkFactory _authUnitOfWorkFactory;

    public string ServerType { get; } = "Authentication";

    public const int MainLoopTime = 100; // Milliseconds

    public Config Config { get; private set; }
    public Communicator Communicator { get; } = new(CommunicatorType.Server);
    public AsyncLengthedSocket ListenerSocket { get; } = new(AsyncLengthedSocket.HeaderSizeType.Word);
    public List<Client> Clients { get; } = new();
    public List<ServerInfo> ServerList { get; } = new();
    public MainLoop Loop { get; }
    public Timer Timer { get; }
    public bool Running => Loop != null && Loop.Running;

    private readonly List<Client> _clientsToRemove = new();

    public Server(IHostApplicationLifetime hostApplicationLifetime, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _authUnitOfWorkFactory = authUnitOfWorkFactory;

        Configuration.OnLoad += ConfigLoaded;
        Configuration.OnReLoad += ConfigReLoaded;
        Configuration.Load();

        if (Config is null)
            throw new Exception("Unable to load configuration!");

        Loop = new MainLoop(this, MainLoopTime);
        Timer = new Timer();

        SetupServerList();

        RegisterConsoleCommands();

        Logger.WriteLog(LogType.Initialize, "The Auth server has been initialized!");
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
    public bool Start()
    {
        // Check the server configuration
        if (Config.AuthConfig.Port == 0 || Config.AuthConfig.Backlog == 0)
        {
            Logger.WriteLog(LogType.Error, "Invalid config values!");
            return false;
        }

        // Check the communicator configuration
        if (Config.CommunicatorConfig.Port == 0 || Config.CommunicatorConfig.Address == null || Config.CommunicatorConfig.Backlog == 0)
        {
            Logger.WriteLog(LogType.Error, "Invalid Communicator config data!");
            return false;
        }

        // Set up the listener socket
        try
        {
            ListenerSocket.OnError += OnError;
            ListenerSocket.OnAccept += OnAccept;
            ListenerSocket.StartListening(new IPEndPoint(IPAddress.Any, Config.AuthConfig.Port), Config.AuthConfig.Backlog);

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.AuthConfig.Port);
        }
        catch (Exception e)
        {
            Logger.WriteLog(LogType.Error, "Unable to create or start listening on the client socket! Exception:");
            Logger.WriteLog(LogType.Error, e);

            return false;
        }

        // Set up communicator
        Communicator.OnLoginRequest += AuthenticateGameServer;
        Communicator.OnRedirectResponse += RedirectResponse;
        Communicator.OnServerInfoResponse += UpdateServerInfo;
        Communicator.Start(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port, Config.CommunicatorConfig.Backlog);

        Logger.WriteLog(LogType.Network, "*** Listening for gameservers on port {0}", Config.CommunicatorConfig.Port);

        // Add the repeating server info request timed event
        Timer.Add("ServerInfoUpdate", 1000, true, () =>
        {
            Communicator.RequestServerInfo();
        });

        // Start the main loop
        Loop.Start();

        // TODO: Set up timed events (query stuff, internal communication, etc...)

        return true;
    }

    private static void OnError()
    {
    }

    private void OnAccept(AsyncLengthedSocket newSocket)
    {
        if (newSocket == null)
            return;

        lock (Clients)
            Clients.Add(new Client(newSocket, this, _authUnitOfWorkFactory));
    }
    #endregion

    #region Communicator
    public bool AuthenticateGameServer(Communicator client, LoginRequestPacket packet)
    {
        if (Communicator.Clients!.ContainsKey(packet.Data.Id))
        {
            Logger.WriteLog(LogType.Debug, $"A server tried to connect to an already in use server slot! Remote Address: {client.Socket.RemoteAddress}");
            return false;
        }

        if (!Config.Servers!.ContainsKey(packet.Data.Id.ToString()))
        {
            Logger.WriteLog(LogType.Debug, $"A server tried to connect to a non-defined server slot! Remote Address: {client.Socket.RemoteAddress}");
            return false;
        }

        if (Config.Servers[packet.Data.Id.ToString()] != packet.Data.Password)
        {
            Logger.WriteLog(LogType.Error, $"A server tried to log in with an invalid password! Remote Address: {client.Socket.RemoteAddress}");
            return false;
        }

        Logger.WriteLog(LogType.Communicator, $"The Game server (Id: {packet.Data.Id}, Address: {client.Socket.RemoteAddress}, Public Address: {packet.Data.Address}) has authenticated!");
        return true;
    }

    public void UpdateServerInfo()
    {
        GenerateServerList();
        BroadcastServerList();
    }

    public void RedirectResponse(Communicator client, RedirectResponsePacket packet)
    {
        Client authClient;
        lock (Clients)
            authClient = Clients.FirstOrDefault(c => c.AccountEntry.Id == packet.AccountId);

        ServerInfo info;
        lock (ServerList)
            info = ServerList.FirstOrDefault(i => i.ServerId == client.ServerData.Id);

        if (authClient != null && info != null)
            authClient.RedirectionResult(packet.Result, info);
    }

    public void RequestRedirection(Client client, byte serverId)
    {
        Communicator.RequestRedirection(serverId, new()
        {
            AccountId = client.AccountEntry.Id,
            Email = client.AccountEntry.Email,
            OneTimeKey = client.OneTimeKey,
            Username = client.AccountEntry.Username
        });
    }

    private void GenerateServerList()
    {
        lock (ServerList)
        {
            var toRemove = new List<ServerInfo>();

            ServerList.Clear();

            foreach (var client in Communicator.Clients!)
            {
                ServerList.Add(new ServerInfo
                {
                    AgeLimit = client.Value.ServerInfo!.AgeLimit,
                    PKFlag = client.Value.ServerInfo.PKFlag,
                    CurrentPlayers = client.Value.ServerInfo.CurrentPlayers,
                    MaxPlayers = client.Value.ServerInfo.MaxPlayers,
                    GamePort = client.Value.ServerInfo.GamePort,
                    QueuePort = client.Value.ServerInfo.QueuePort,
                    Ip = client.Value.ServerData!.Address,
                    ServerId = client.Key,
                    Status = 1
                });
            }
        }
    }
    #endregion

    public void Shutdown()
    {
        ListenerSocket.Close();

        Loop.Stop();

        _hostApplicationLifetime.StopApplication();
    }

    public void MainLoop(long delta)
    {
        Communicator.Update();

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
    private void RegisterConsoleCommands()
    {
        CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
        CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
        CommandProcessor.RegisterCommand("create", ProcessCreateCommand);
    }

    private void ProcessExitCommand(string[] parts)
    {
        var minutes = 0;

        if (parts.Length > 1)
            minutes = int.Parse(parts[1]);

        Timer.Add("exit", minutes * 60000, false, Shutdown);

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
