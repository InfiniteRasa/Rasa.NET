using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rasa.Hosting
{
    using Commands;

    public class RasaHost : BackgroundService
    {
        /// <summary>The process exit code of a server that could not start.</summary>
        public const int StartFailedExitCode = 1;

        private readonly IRasaServer _rasaServer;
        private readonly IHostApplicationLifetime _lifetime;

        public RasaHost(IRasaServer rasaServer, IHostApplicationLifetime lifetime)
        {
            _rasaServer = rasaServer;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.WriteLog(LogType.File, "Application startup!");

            InitConsole(_rasaServer.ServerType);

            Logger.WriteLog(LogType.Initialize, "*** Initialized {0} Server...", _rasaServer.ServerType);

            if (!TryStart())
            {
                // The process ends, with an exit code that says so. It used to log this and stay
                // up with nothing running - the ports closed, the console reading nothing - and
                // a service manager or a restart script saw a server that was still going. An
                // exception out of Start ended this method instead, and with no logging set up
                // for the host that was not shown at all: the process exited 0, silently.
                Logger.WriteLog(LogType.Error, "Unable to start server! Stopping.");
                Environment.ExitCode = StartFailedExitCode;
                _lifetime?.StopApplication();
                return;
            }

            await ProcessCommandsSafe(stoppingToken);
        }

        private bool TryStart()
        {
            try
            {
                return _rasaServer.Start();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Starting the server failed:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
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
                await CommandProcessor.ProcessCommand(stoppingToken);
                await Task.Delay(25, stoppingToken);
            }
        }

        private void InitConsole(string type)
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
}