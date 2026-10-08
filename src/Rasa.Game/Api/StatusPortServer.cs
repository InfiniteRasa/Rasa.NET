using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Api
{
    using Config;

    /// <summary>
    /// The status port (ApiConfig.StatusPort): a TCP port that answers whatever a connection
    /// sends first - one byte is enough, and what it is does not matter - with the whole server
    /// status as one line of JSON, and closes:
    ///
    ///   {"game_server_status":"healthy","app_server_status":"healthy","uptimeseconds":45000,
    ///    "currentconnections":3,"peakconnections":9,"maxconnections":1024}
    ///
    /// No key. Who may ask is AllowedIps: with nothing on it, anybody; otherwise a connection
    /// from any other address is closed without a word.
    /// </summary>
    public sealed class StatusPortServer : TcpService
    {
        private readonly ServerStatus _status;
        private volatile IpAllowList _allowed = IpAllowList.Open;
        private string _described;

        public StatusPortServer(ServerStatus status) => _status = status;

        protected override string Label => "Status port";

        /// <summary>Takes the settings, on a config reload as at the start: the allow list counts from the next connection.</summary>
        public void Apply(StatusPortConfig config)
        {
            config ??= new StatusPortConfig();

            var allowed = IpAllowList.Parse(config.AllowedIps);

            _allowed = allowed;

            if (!config.Enabled || !Listen(config.BindAddress, config.Port))
            {
                if (!config.Enabled)
                    Stop();

                _described = null;
                return;
            }

            var described = $"{LocalEndPoint}|{string.Join(",", allowed.Invalid)}|{allowed.Count}";

            if (described == _described)
                return;

            _described = described;

            foreach (var entry in allowed.Invalid)
                Logger.WriteLog(LogType.Error, $"{Label}: AllowedIps entry \"{entry}\" is no address and no range; it allows nobody.");

            Logger.WriteLog(LogType.Network,
                $"{Label}: {(allowed.AllowsAll ? "any address may ask" : $"{allowed.Count} allowed address(es) or range(s)")}.");
        }

        protected override async Task Exchange(Stream stream, IPAddress remote, ExchangeLimit time)
        {
            var limit = time.Token;

            if (!_allowed.Allows(remote))
            {
                Refused($"{remote}, which is not on AllowedIps");
                return;
            }

            // Answered when something has been sent, not on connecting.
            var first = new byte[256];

            if (await stream.ReadAsync(first, 0, first.Length, limit).ConfigureAwait(false) <= 0)
                return;

            var answer = Encoding.UTF8.GetBytes(_status.FullJson() + "\n");

            await stream.WriteAsync(answer, 0, answer.Length, limit).ConfigureAwait(false);
            await stream.FlushAsync(limit).ConfigureAwait(false);
        }
    }
}
