using System;

namespace Rasa.Structures
{
    using Packets.Communicator;

    public class LoginAccountEntry
    {
        public uint Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public DateTime ExpireTime { get; set; }
        public uint OneTimeKey { get; set; }

        public LoginAccountEntry(RedirectRequestPacket packet)
        {
            Id = packet.AccountId;
            Email = packet.Email;
            Name = packet.Username;
            OneTimeKey = packet.OneTimeKey;
            ExpireTime = DateTime.Now.AddMinutes(1);
        }
    }
}
