using System.IO;

namespace Rasa.Packets.Login.Server
{
    using Data;
    using Extensions;

    public class ClientKeyOkPacket : IOpcodedPacket<LoginOpcode>
    {
        public LoginOpcode Opcode { get; } = LoginOpcode.ClientKeyOk;

        public void Read(BinaryReader br)
        {
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((short) 2);
            bw.WriteUtf8String("ENC OK");
        }
    }
}
