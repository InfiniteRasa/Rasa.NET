using System.Net;

namespace Rasa.Structures
{
    public class ServerInfo
    {
        public byte ServerId { get; set; }
        public IPAddress Ip { get; set; }
        public int QueuePort { get; set; }
        public int GamePort { get; set; }
        public byte AgeLimit { get; set; }
        public byte PKFlag { get; set; }
        public ushort CurrentPlayers { get; set; }
        public ushort MaxPlayers { get; set; }
        public byte Status { get; set; }

        public void Setup(IPAddress address, int queuePort, int gamePort, byte ageLimit, byte pkFlag, ushort currPlayers, ushort maxPlayers)
        {
            Ip = address;
            QueuePort = queuePort;
            GamePort = gamePort;
            AgeLimit = ageLimit;
            PKFlag = pkFlag;
            CurrentPlayers = currPlayers;
            MaxPlayers = maxPlayers;
            Status = 1;
        }

        public void Clear()
        {
            Ip = IPAddress.None;
            QueuePort = 0;
            GamePort = 0;
            AgeLimit = 0;
            PKFlag = 0;
            CurrentPlayers = 0;
            MaxPlayers = 0;
            Status = 0;
        }
    }
}
