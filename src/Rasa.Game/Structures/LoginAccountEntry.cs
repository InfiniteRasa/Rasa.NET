using System;

namespace Rasa.Structures
{
    using Packets.Communicator;

    public class LoginAccountEntry : AccountEntry
    {
        public DateTime ExpireTime { get; set; }
        public uint OneTimeKey { get; set; }

        public LoginAccountEntry(RedirectRequestPacket packet)
        {
            Id = packet.Id;
            Username = packet.Username;
            Level = packet.Level;
            OneTimeKey = packet.OneTimeKey;
            ExpireTime = DateTime.Now.AddMinutes(1);
        }
    }
}
