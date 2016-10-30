namespace Rasa.Config
{
    public class Config
    {
        public string DatabaseConnectionString { get; set; }
        public SocketAsyncConfig SocketAsyncConfig { get; set; }
        public GameSocketConfig GameSocketConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
        public ServerInfoConfig ServerInfoConfig { get; set; }
    }
}
