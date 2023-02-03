using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rasa.Hosting;

using Commands;

public class RasaHost : BackgroundService
{
    private readonly IRasaServer _rasaServer;

    public RasaHost(IRasaServer rasaServer)
    {
        _rasaServer = rasaServer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.WriteLog(LogType.File, "Application startup!");

        InitConsole(_rasaServer.ServerType);

        Logger.WriteLog(LogType.Initialize, "*** Initialized {0} Server...", _rasaServer.ServerType);

        if (!_rasaServer.Start())
        {
            Logger.WriteLog(LogType.Error, "Unable to start server!");
            return;
        }

        await ProcessCommandsSafe(stoppingToken);
    }

    private async Task ProcessCommandsSafe(CancellationToken stoppingToken)
    {
        try
        {
            await ProcessCommands(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // cancellationToken was fired
        }
    }

    private async Task ProcessCommands(CancellationToken stoppingToken)
    {
        while (_rasaServer.Running && !stoppingToken.IsCancellationRequested)
        {
            CommandProcessor.ProcessCommand(stoppingToken);
            await Task.Delay(25, stoppingToken);
        }
    }

    private static void InitConsole(string type)
    {
        Console.Title = $"Rasa.NET - {type} Server";

        Logger.WriteLog(LogType.Initialize, @"  _____                   _   _ ______ _______ ");
        Logger.WriteLog(LogType.Initialize, @" |  __ \                 | \ | |  ____|__   __|");
        Logger.WriteLog(LogType.Initialize, @" | |__) |__ _ ___  __ _  |  \| | |__     | |   ");
        Logger.WriteLog(LogType.Initialize, @" |  _  // _` / __|/ _` | | . ` |  __|    | |   ");
        Logger.WriteLog(LogType.Initialize, @" | | \ \ (_| \__ \ (_| |_| |\  | |____   | |   ");
        Logger.WriteLog(LogType.Initialize, @" |_|  \_\__,_|___/\__,_(_)_| \_|______|  |_|   ");
        Logger.WriteLog(LogType.Initialize, $@" Tabula Rasa server - Rasa.NET - {type}");
        Logger.WriteLog(LogType.Initialize, "");
    }
}