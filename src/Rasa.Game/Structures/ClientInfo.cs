using System;

namespace Rasa.Structures
{
    public class ClientInfo
    {
        public uint OneTimeKey { get; set; }
        public uint AccountId { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}
