using System.IO;

using Rasa.Packets;

namespace Rasa.Communicator.Packets
{
    public class LoginResponsePacket : IOpcodedPacket<CommunicatorOpcode>
    {
        public CommunicatorOpcode Opcode { get; } = CommunicatorOpcode.LoginResponse;
        public CommunicatorActionResult Result { get; set; }

        public void Read(BinaryReader br)
        {
            Result = (CommunicatorActionResult)br.ReadByte();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Opcode);
            bw.Write((byte)Result);
        }
    }
}
