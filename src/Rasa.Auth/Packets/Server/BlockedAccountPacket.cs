using System.IO;

namespace Rasa.Packets.Server
{
    using Data;

    public class BlockedAccountPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public uint Reason { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.BlockedAccount;

        public void Read(BinaryReader reader)
        {
            Reason = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(Reason);
        }

        public override string ToString()
        {
            return $"BlockedAccountPacket({Reason})";
        }
    }
}
