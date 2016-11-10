namespace Rasa.Config
{
    public class Config
    {
        public string WorldDatabaseConnectionString { get; set; }
        public string CharDatabaseConnectionString { get; set; }
        public SocketAsyncConfig SocketAsyncConfig { get; set; }
        public GameConfig GameConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
        public ServerInfoConfig ServerInfoConfig { get; set; }
        public QueueConfig QueueConfig { get; set; }
    }
}
