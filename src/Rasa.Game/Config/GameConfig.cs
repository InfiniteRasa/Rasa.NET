namespace Rasa.Config
{
    public class GameConfig
    {
        public const int DefaultTransferTimeoutSeconds = 60;
        public const double DefaultCorpseLootDistance = 6;

        public string PublicAddress { get; set; }
        public int Port { get; set; }
        public int Backlog { get; set; }
        public int TransferTimeoutSeconds { get; set; } = DefaultTransferTimeoutSeconds;
        public double CorpseLootDistance { get; set; } = DefaultCorpseLootDistance;

        /// <summary>
        /// How often the world loop's metrics go out to GM clients, in milliseconds. Zero turns
        /// it off, which is the default: the 1.16.5.0 client takes the message and does nothing
        /// with it, because GameClient::SetServerPerfMetrics is an empty stub in the retail
        /// build - see ServerPerformanceMetricsPacket. Turn it on for a client that implements
        /// it. 'perf' on the console reads the same numbers either way.
        /// </summary>
        public int PerformanceMetricsInterval { get; set; }

        /// <summary>
        /// How often every player in the world is saved, in minutes (Managers.AutoSave): position,
        /// time played, health and death penalties, cooldowns. 0 turns it off; characters are then
        /// saved only as they leave the world.
        /// </summary>
        public int AutoSaveMinutes { get; set; } = Managers.AutoSave.DefaultMinutes;
    }
}
