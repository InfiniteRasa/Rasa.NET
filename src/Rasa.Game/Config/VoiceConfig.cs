namespace Rasa.Config
{
    /// <summary>
    /// Squad voice chat: the Talkback voice server built into the world server (see
    /// Voice.VoiceServer). Leave the section out, or set Enabled to false, and squads are told
    /// voice is unavailable - the client then never tries to connect, exactly as before there
    /// was a voice server. Enabled, Address, the talk-time values and the timeouts are picked up
    /// by a config reload; changing BindAddress or Port restarts the listener.
    /// </summary>
    public class VoiceConfig
    {
        /// <summary>Whether squads are offered voice chat at all. Off when the section is missing.</summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The host name or address clients are told to connect to, without the port. Empty uses
        /// GameConfig.PublicAddress, which is right whenever the voice server is reached at the
        /// same address as the world.
        /// </summary>
        public string PublicAddress { get; set; } = "";

        /// <summary>The local address the UDP socket binds to. Empty or "0.0.0.0" binds every interface.</summary>
        public string BindAddress { get; set; } = "0.0.0.0";

        /// <summary>The UDP port clients connect to. It has to be reachable from them: forward it as UDP.</summary>
        public int Port { get; set; } = 8103;

        /// <summary>
        /// The talk-time budget shown under the player's portrait, in seconds: how long someone can
        /// talk before the bar empties. The client only displays it; nothing cuts a speaker off.
        /// </summary>
        public float MaxTalkTime { get; set; } = 60f;

        /// <summary>Seconds of talk time regained per second of silence (the bar's refill rate).</summary>
        public float TalkTimeRegen { get; set; } = 1f;

        /// <summary>A voice connection nothing has been heard from for this long is dropped. Clients send a heartbeat every few seconds.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>How long the login token handed out with VoiceChatConnectInfo stays valid.</summary>
        public int TokenLifetimeSeconds { get; set; } = 60;

        /// <summary>Logs every voice login, logout and refusal.</summary>
        public bool LogSessions { get; set; } = true;
    }
}
