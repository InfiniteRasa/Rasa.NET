using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa
{
    using Hosting;

    public class GameHost : RasaHost
    {
        /// <summary>
        /// How long a host stop (Ctrl+C, a service stop, docker stop) waits for the world loop to
        /// save everyone. Docker kills a container 10 s after asking it to stop, by default.
        /// </summary>
        public static readonly TimeSpan StopSaveTimeout = TimeSpan.FromSeconds(8);

        private readonly IRasaServer _server;

        public GameHost(IRasaServer server) : base(server)
        {
            _server = server;
        }

        /// <summary>
        /// Every player saved and taken out of the world before the process ends. The console's
        /// exit has done it already by the time it stops the host; any other stop had nobody saved.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                (_server as Game.Server)?.EvacuateForHost(StopSaveTimeout);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Saving the players as the server stopped failed: {e}");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
