using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Commands;
    using Database;
    using Game;
    using Misc;

    internal class GameHostedService : BackgroundService
    {
        private const string Type = "Game";
        private Server _server;
        private readonly WorldContext _worldContext;
        private readonly CharacterContext _charContext;
        private readonly IHostApplicationLifetime _appLifetime;

        public GameHostedService(WorldContext worldContext, CharacterContext charContext, IHostApplicationLifetime appLifetime)
        {
            _worldContext = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
            _charContext = charContext ?? throw new ArgumentNullException(nameof(charContext));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new Server(_worldContext, _charContext, _appLifetime.StopApplication);

            Logger.WriteLog(LogType.File, "Application startup!");

            ProgramBase.InitConsole(Type);

            Logger.WriteLog(LogType.Initialize, "*** Initialized Game Server...");

            if (!_server.Start())
            {
                Logger.WriteLog(LogType.Error, "Unable to start server!");
                return;
            }

            using (var stream = Console.OpenStandardInput())
            using (var conInput = new StreamReader(stream))
            {
                while (_server.Running && !stoppingToken.IsCancellationRequested)
                {
                    await Task.WhenAny(CommandProcessor.ProcessCommandAsync(conInput), Task.Delay(Timeout.Infinite, stoppingToken));
                }
            }
        }

        public override void Dispose()
        {
            _server.Shutdown();

            base.Dispose();
        }
    }
}