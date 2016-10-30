using System.Collections.Generic;

namespace Rasa.Config
{
    public class Config
    {
        public string DatabaseConnectionString { get; set; }
        public Dictionary<string, string> Servers { get; set; }
        public SocketAsyncConfig SocketAsyncConfig { get; set; }
        public AuthSocketConfig AuthSocketConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
    }

    public class AuthSocketConfig
    {
        public int Port { get; set; }
        public int Backlog { get; set; }
    }
}
