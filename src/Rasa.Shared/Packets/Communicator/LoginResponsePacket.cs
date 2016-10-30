using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class LoginResponsePacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.LoginResponse;
        public CommLoginReason Response { get; set; }

        public void Read(BinaryReader br)
        {
            Response = (CommLoginReason) br.ReadByte();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write((byte) Response);
        }
    }
}
