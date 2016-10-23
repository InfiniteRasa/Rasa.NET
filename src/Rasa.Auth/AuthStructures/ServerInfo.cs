using System.Net;

namespace Rasa.AuthStructures
{
    public class ServerInfo
    {
        public byte ServerId { get; set; }
        public IPAddress Ip { get; set; }
        public uint QueuePort { get; set; }
        public uint GamePort { get; set; }
        public byte AgeLimit { get; set; }
        public byte PKFlag { get; set; }
        public ushort CurrentPlayers { get; set; }
        public ushort MaxPlayers { get; set; }
        public byte Status { get; set; }
    }
}
