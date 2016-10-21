using System.Collections.Generic;

namespace Rasa.AuthConfig
{
    using Communicator;

    public class Config
    {
        public string DatabaseConnectionString { get; set; }
        public Dictionary<string, string> Servers { get; set; }
        public SocketConfig SocketConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
    }

    public class SocketConfig
    {
        public int Port { get; set; }
        public int BackLog { get; set; }
        public int BufferSize { get; set; }
        public int MaxClients { get; set; }
        public int ConcurrentOperationsByClient { get; set; }
    }
}
