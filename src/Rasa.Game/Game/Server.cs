using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Hosting;

namespace Rasa.Game;

using Rasa.Commands;
using Rasa.Communicator;
using Rasa.Config;
using Rasa.Hosting;
using Rasa.Login;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets;
using Rasa.Queue;
using Rasa.Structures;
using Rasa.Threading;
using Rasa.Timer;

public class Server : ILoopable, IRasaServer
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IClientFactory _clientFactory;

    public string ServerType { get; } = "Game";

    public const int MainLoopTime = 100; // Milliseconds
    public const int SendBufferSize = 512;

    public Config Config { get; private set; }
    public IPAddress PublicAddress { get; }
    public Communicator AuthCommunicator { get; } = new(CommunicatorType.Client);
    public AsyncLengthedSocket ListenerSocket { get; } = new(AsyncLengthedSocket.HeaderSizeType.Dword, false);
    public QueueManager QueueManager { get; private set; }
    public LoginManager LoginManager { get; } = new();
    public List<Client> Clients { get; } = new();
    public Dictionary<uint, LoginAccountEntry> IncomingClients { get; } = new();
    public MainLoop Loop { get; }
    public Timer Timer { get; } = new();
    public bool Running => Loop != null && Loop.Running;
    public bool IsFull => CurrentPlayers >= Config.ServerInfoConfig.MaxPlayers;
    public ushort CurrentPlayers { get; set; }

    private readonly List<Client> _clientsToRemove = new List<Client>();

    public Server(IHostApplicationLifetime hostApplicationLifetime, IClientFactory clientFactory)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _clientFactory = clientFactory;

        Configuration.OnLoad += ConfigLoaded;
        Configuration.OnReLoad += ConfigReLoaded;
        Configuration.Load();

        Loop = new MainLoop(this, MainLoopTime);

        PublicAddress = IPAddress.Parse(Config.GameConfig.PublicAddress);

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
        lock (_clientsToRemove)
            _clientsToRemove.Add(client);
    }

    public void MainLoop(long delta)
    {
        Timer.Update(delta);

        if (Clients.Count == 0)
            return;

        lock (Clients)
        {
            foreach (var client in Clients)
                client.Update(delta);

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

    public bool Start()
    {
        // If no config file has been found, these values are 0 by default
        if (Config.GameConfig.Port == 0 || Config.GameConfig.Backlog == 0)
        {
            Logger.WriteLog(LogType.Error, "Invalid config values!");
            return false;
        }

        Loop.Start();

        SetupCommunicator();

        QueueManager = new QueueManager(this);
        LoginManager.OnLogin += OnLogin;

        try
        {
            
            ListenerSocket.OnAccept += OnAccept;
            ListenerSocket.StartListening(new IPEndPoint(IPAddress.Any, Config.GameConfig.Port), Config.GameConfig.Backlog);
        }
        catch (Exception e)
        {
            Logger.WriteLog(LogType.Error, "Unable to create or start listening on the client socket! Exception:");
            Logger.WriteLog(LogType.Error, e);

            return false;
        }

        Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.GameConfig.Port);

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

        Timer.Add("QueueManagerUpdate", Config.QueueConfig.UpdateInterval, true, () =>
        {
            QueueManager.Update(Config.ServerInfoConfig.MaxPlayers - CurrentPlayers);
        });

        return true;
    }

    private void OnLogin(LoginClient client)
    {
        lock (Clients)
        {
            var newClient = _clientFactory.Create(client.Socket, client.Data, this);
            Clients.Add(newClient);
        }
    }

    private void OnAccept(AsyncLengthedSocket newSocket)
    {
        if (newSocket == null)
            return;

        LoginManager.LoginSocket(newSocket);
    }

    public LoginAccountEntry AuthenticateClient(Client client, uint accountId, uint oneTimeKey)
    {
        lock (IncomingClients)
        {
            if (!IncomingClients.ContainsKey(accountId))
                return null;

            var entry = IncomingClients[accountId];
            if (entry == null || entry.OneTimeKey != oneTimeKey)
                return null;

            IncomingClients.Remove(accountId);

            return entry;
        }
    }

    public bool IsBanned(uint accountId)
    {
        return false; // TODO
    }

    public bool IsAlreadyLoggedIn(uint accountId)
    {
        lock (Clients)
            foreach (var client in Clients)
                if (client.IsAuthenticated() && client.AccountEntry.Id == accountId)
                    return true;

        return false;
    }

    public void Shutdown()
    {
        AuthCommunicator.Close();

        ListenerSocket.Close();

        Loop.Stop();

        _hostApplicationLifetime.StopApplication();
    }

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
        if (AuthCommunicator.Connected)
            AuthCommunicator.Close();

        try
        {
            AuthCommunicator.OnConnect += OnCommunicatorConnect;
            AuthCommunicator.OnError += OnCommunicatorError;
            AuthCommunicator.OnLoginResponse += OnCommunicatorLoginResponse;
            AuthCommunicator.OnRedirectRequest += OnCommunicatorRedirectRequest;
            AuthCommunicator.OnServerInfoRequest += OnCommunicatorServerInfoRequest;
            AuthCommunicator.Start(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port);
        }
        catch (Exception e)
        {
            Logger.WriteLog(LogType.Error, "Unable to create or start listening on the Auth server socket! Retrying soon... Exception:");
            Logger.WriteLog(LogType.Error, e);
        }

        Logger.WriteLog(LogType.Network, $"*** Connecting to auth server! Address: {Config.CommunicatorConfig.Address}:{Config.CommunicatorConfig.Port}");
    }

    private void OnCommunicatorError()
    {
        Timer.Add("CommReconnect", 10000, false, () =>
        {
            if (!AuthCommunicator?.Connected ?? true)
                ConnectCommunicator();
        });

        Logger.WriteLog(LogType.Error, "Could not connect to the Auth server! Trying again in a few seconds...");
    }

    private void OnCommunicatorConnect(ServerData data)
    {
        Logger.WriteLog(LogType.Network, "*** Connected to the Auth Server!");

        data.Id = Config.ServerInfoConfig.Id;
        data.Address = PublicAddress;
        data.Password = Config.ServerInfoConfig.Password;
    }

    private void OnCommunicatorLoginResponse(CommunicatorActionResult result)
    {
        if (result == CommunicatorActionResult.Success)
        {
            Logger.WriteLog(LogType.Network, "Successfully authenticated with the Auth server!");
            return;
        }

        AuthCommunicator.Close();

        Logger.WriteLog(LogType.Error, "Could not authenticate with the Auth server! Shutting down internal communication!");
    }

    private void OnCommunicatorServerInfoRequest(ServerInfo info)
    {
        info.AgeLimit = Config.ServerInfoConfig.AgeLimit;
        info.PKFlag = Config.ServerInfoConfig.PKFlag;
        info.CurrentPlayers = CurrentPlayers;
        info.GamePort = Config.GameConfig.Port;
        info.QueuePort = Config.QueueConfig.Port;
        info.MaxPlayers = (ushort)Config.SocketAsyncConfig.MaxClients;
    }

    private bool OnCommunicatorRedirectRequest(RedirectRequest request)
    {
        lock (IncomingClients)
        {
            IncomingClients.Remove(request.AccountId);
            IncomingClients.Add(request.AccountId, new LoginAccountEntry(request));
        }

        return true;
    }
    #endregion

    #region Commands
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
