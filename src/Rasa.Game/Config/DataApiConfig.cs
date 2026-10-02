namespace Rasa.Config
{
    /// <summary>
    /// HTTP API used by external development tools to query server data.
    /// </summary>
    public class DataApiConfig
    {
        public bool Enabled { get; set; } = true;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 8104;
        public int Backlog { get; set; } = 64;

        /// <summary>
        /// HMAC secret used to sign Data API JWTs. Keep this out of source-controlled
        /// configuration in production (appsettings.env.json is intended for overrides).
        /// At least 32 UTF-8 bytes are required.
        /// </summary>
        public string JwtSecret { get; set; } = string.Empty;

        /// <summary>Lifetime of an issued Data API JWT, in seconds.</summary>
        public int TokenLifetimeSeconds { get; set; } = 86400;
    }
}
