using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Rasa.Api
{
    /// <summary>
    /// What the status listeners report, kept where their threads can read it without touching
    /// the world or waiting on it.
    ///
    /// The world loop writes: every tick it says it is alive, and once a second how many
    /// players hold a slot (<see cref="Beat"/>). The listeners' threads only read. They take no
    /// lock the loop holds, so a loop that has stopped ticking - the one thing a health check
    /// is for - is reported, not waited for.
    ///
    ///  - game_server_status is healthy while the server has finished loading, has not been
    ///    shut down, is listening for players and its loop ticked within <see cref="StallMs"/>;
    ///  - app_server_status is healthy while the link to the Auth server is up and logged in
    ///    (<see cref="AuthLinked"/>): without it nobody can get from the login to this world;
    ///  - currentconnections is Server.CurrentPlayers, the number the server list shows and
    ///    that maxconnections (ServerInfoConfig.MaxPlayers) is the limit of;
    ///  - peakconnections is the most that number has been since the server started;
    ///  - uptimehours is the time since the server opened its ports, in hours to two places.
    /// </summary>
    public sealed class ServerStatus
    {
        public const string Healthy = "healthy";
        public const string Unhealthy = "unhealthy";

        /// <summary>How often the player count is read, in milliseconds.</summary>
        public const int SampleMs = 1000;

        /// <summary>The clock, in milliseconds; replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether the link to the Auth server is up and logged in; read on a listener's thread.</summary>
        public Func<bool> AuthLinked { get; set; } = () => false;

        private int _stallMs = 15000;
        private long _startedAt = -1;
        private long _lastBeat = -1;
        private long _lastSample = -1;
        private int _current;
        private int _peak;
        private int _max;
        private int _ready;
        private int _stopped;
        private int _listening;

        /// <summary>How long the loop may go without a tick and still be healthy, in milliseconds.</summary>
        public int StallMs
        {
            get => Volatile.Read(ref _stallMs);
            set => Volatile.Write(ref _stallMs, Math.Max(1000, value));
        }

        /// <summary>The server has opened its ports: uptime counts from here.</summary>
        public void Started()
        {
            Interlocked.CompareExchange(ref _startedAt, Now(), -1);
            Volatile.Write(ref _stopped, 0);
        }

        /// <summary>Everything is loaded: the server is ready for players.</summary>
        public void Ready() => Volatile.Write(ref _ready, 1);

        /// <summary>The server is shutting down.</summary>
        public void Stopped() => Volatile.Write(ref _stopped, 1);

        /// <summary>
        /// From the world loop, every tick: it is alive, whether the player port is open, and -
        /// once a second, which is when <paramref name="current"/> is asked - how many players
        /// hold a slot of how many.
        /// </summary>
        public void Beat(Func<int> current, int max, bool listening)
        {
            var now = Now();

            Volatile.Write(ref _lastBeat, now);
            Volatile.Write(ref _listening, listening ? 1 : 0);

            var last = Volatile.Read(ref _lastSample);

            if (last >= 0 && now - last < SampleMs)
                return;

            Volatile.Write(ref _lastSample, now);

            var count = Math.Max(0, current?.Invoke() ?? 0);

            Volatile.Write(ref _current, count);
            Volatile.Write(ref _max, Math.Max(0, max));

            if (count > Volatile.Read(ref _peak))
                Volatile.Write(ref _peak, count);
        }

        public bool GameHealthy
        {
            get
            {
                var beat = Volatile.Read(ref _lastBeat);

                return Volatile.Read(ref _ready) == 1
                       && Volatile.Read(ref _stopped) == 0
                       && Volatile.Read(ref _listening) == 1
                       && beat >= 0
                       && Now() - beat <= StallMs;
            }
        }

        public bool AppHealthy
        {
            get
            {
                try
                {
                    return Volatile.Read(ref _stopped) == 0 && (AuthLinked?.Invoke() ?? false);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Hours since <see cref="Started"/>, to two places; 0 before it.</summary>
        public double UptimeHours
        {
            get
            {
                var started = Volatile.Read(ref _startedAt);

                return started < 0 ? 0 : Math.Round(Math.Max(0, Now() - started) / 3600000.0, 2);
            }
        }

        public int CurrentConnections => Volatile.Read(ref _current);
        public int PeakConnections => Volatile.Read(ref _peak);
        public int MaxConnections => Volatile.Read(ref _max);

        /// <summary>Both healths, read once: what is written and what it is judged by are the same reading.</summary>
        public (bool Game, bool App) Health => (GameHealthy, AppHealthy);

        /// <summary>{"game_server_status":"healthy","app_server_status":"healthy"}</summary>
        public string HealthJson() => HealthJson(Health);

        public static string HealthJson((bool Game, bool App) health) => Json(writer => WriteHealth(writer, health));

        /// <summary>{"uptimehours":12.5,"currentconnections":3,"peakconnections":9,"maxconnections":1024}</summary>
        public string StatusJson() => Json(WriteStatus);

        /// <summary>Both of the above as one object, the health first: what the status port answers.</summary>
        public string FullJson()
        {
            var health = Health;

            return Json(writer =>
            {
                WriteHealth(writer, health);
                WriteStatus(writer);
            });
        }

        private static void WriteHealth(Utf8JsonWriter writer, (bool Game, bool App) health)
        {
            writer.WriteString("game_server_status", health.Game ? Healthy : Unhealthy);
            writer.WriteString("app_server_status", health.App ? Healthy : Unhealthy);
        }

        private void WriteStatus(Utf8JsonWriter writer)
        {
            writer.WriteNumber("uptimehours", UptimeHours);
            writer.WriteNumber("currentconnections", CurrentConnections);
            writer.WriteNumber("peakconnections", PeakConnections);
            writer.WriteNumber("maxconnections", MaxConnections);
        }

        /// <summary>One JSON object, its properties written by <paramref name="write"/>.</summary>
        public static string Json(Action<Utf8JsonWriter> write)
        {
            using var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                write(writer);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
