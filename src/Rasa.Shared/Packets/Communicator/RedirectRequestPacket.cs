using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class RedirectRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.RedirectRequest;
        public uint AccountId { get; set; }
        public uint OneTimeKey { get; set; }

        public void Read(BinaryReader br)
        {
            AccountId = br.ReadUInt32();
            OneTimeKey = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(AccountId);
            bw.Write(OneTimeKey);
        }
    }
}
