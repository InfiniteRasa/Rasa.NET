using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;
    using Extensions;

    public class RedirectRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.RedirectRequest;

        public uint Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public uint OneTimeKey { get; set; }

        public void Read(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Username = br.ReadLengthedString();
            Email = br.ReadLengthedString();
            OneTimeKey = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(Id);
            bw.WriteLengthedString(Username);
            bw.WriteLengthedString(Email);
            bw.Write(OneTimeKey);
        }
    }
}
