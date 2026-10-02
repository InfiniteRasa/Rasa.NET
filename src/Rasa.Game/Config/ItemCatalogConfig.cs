namespace Rasa.Config
{
    /// <summary>
    /// Read-only HTTP catalog used by external development tools to browse the
    /// server's loaded item templates. Item grants deliberately stay on the
    /// existing authenticated GM command path (.giveitem).
    /// </summary>
    public class ItemCatalogConfig
    {
        public bool Enabled { get; set; } = true;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 8104;
        public int Backlog { get; set; } = 64;
    }
}
