using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Auth;
    using Commands;
    using Database;
    using Misc;

    public class AuthHostedService : BackgroundService
    {
        private const string Type = "Authentication";

        private readonly AuthContext _authContext;
        private readonly IHostApplicationLifetime _appLifetime;
        private Server _server;

        public AuthHostedService(AuthContext authContext, IHostApplicationLifetime appLifetime)
        {
            _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new Server(_authContext, _appLifetime.StopApplication);

            Logger.WriteLog(LogType.File, "Application startup!");

            ProgramBase.InitConsole(Type);

            Logger.WriteLog(LogType.Initialize, "*** Initialized Authentication Server...");

            if (!_server.Start())
            {
                Logger.WriteLog(LogType.Error, "Unable to start server!");
                return;
            }

            using(var stream = Console.OpenStandardInput())
            using(var conInput = new StreamReader(stream))
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