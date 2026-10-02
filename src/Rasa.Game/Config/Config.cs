using System.Collections.Generic;

namespace Rasa.Config
{
    public class Config
    {
        public SocketAsyncConfig SocketAsyncConfig { get; set; }
        public GameConfig GameConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
        public ServerInfoConfig ServerInfoConfig { get; set; }
        public QueueConfig QueueConfig { get; set; }
        public GameDataConfig GameDataConfig { get; set; }
        public VoiceConfig VoiceConfig { get; set; }
        public MessageOfTheDayConfig MessageOfTheDay { get; set; } = new MessageOfTheDayConfig();

        /// <summary>The REST API and the status port (Rasa.Api); both off without the section.</summary>
        public ApiConfig ApiConfig { get; set; } = new ApiConfig();

        /// <summary>The numbers of the Edmund Range match (Managers.Battlegrounds).</summary>
        public BattlegroundConfig Battleground { get; set; } = new BattlegroundConfig();

        /// <summary>
        /// The maps that run in several shared copies, by map context id. Edmund Range (2374) is
        /// one without an entry in the file; an entry there for it replaces this one.
        /// </summary>
        public Dictionary<string, MapInstanceConfig> MapInstances { get; set; } = new Dictionary<string, MapInstanceConfig>
        {
            ["2374"] = new MapInstanceConfig()
        };
    }
}
