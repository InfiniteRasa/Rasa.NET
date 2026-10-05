namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's MapInstances, one entry by map context id: a map that runs in several
    /// shared copies (Managers.MapChannelManager's instances). A map with no entry, or with
    /// MaxCopies 1, has its own channel and nothing else.
    /// </summary>
    public class MapInstanceConfig
    {
        /// <summary>How many players one copy holds before the next opens.</summary>
        public int Capacity { get; set; } = 32;

        /// <summary>The most copies that run at once, the map's own channel among them.</summary>
        public int MaxCopies { get; set; } = 8;

        /// <summary>How long a further copy stands empty before it is closed. The map's own channel is never closed.</summary>
        public int IdleCloseSeconds { get; set; } = 300;
    }
}
