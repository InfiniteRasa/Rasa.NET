using System.Net;

namespace Rasa.Communicator
{
    public class CommunicatorConfig
    {
        public int ListenPort { get; set; }
        public int ConnectPort { get; set; }
        public IPAddress ConnectAddress { get; set; }
    }
}
