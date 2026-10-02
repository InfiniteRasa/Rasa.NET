namespace Rasa.Config
{
    /// <summary>
    /// Read-only HTTP API used by external development tools to query server data.
    /// Resource-specific mutation stays on the existing authenticated game/GM
    /// command paths rather than being exposed through this service.
    /// </summary>
    public class DataApiConfig
    {
        public bool Enabled { get; set; } = true;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 8104;
        public int Backlog { get; set; } = 64;
    }
}
