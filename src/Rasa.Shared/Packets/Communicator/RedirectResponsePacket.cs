using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class RedirectResponsePacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.RedirectResponse;
        public byte Response { get; set; }

        public void Read(BinaryReader br)
        {
            Response = br.ReadByte();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(Response);
        }
    }
}
