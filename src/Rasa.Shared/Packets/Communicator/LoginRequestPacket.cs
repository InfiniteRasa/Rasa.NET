using System.IO;
using System.Net;

namespace Rasa.Packets.Communicator
{
    using Data;
    using Extensions;

    public class LoginRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.LoginRequest;
        public byte ServerId { get; set; }
        public string Password { get; set; }
        public IPAddress PublicAddress { get; set; }

        public void Read(BinaryReader br)
        {
            ServerId = br.ReadByte();
            Password = br.ReadLengthedString();
            PublicAddress = IPAddress.Parse(br.ReadLengthedString());
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(ServerId);
            bw.WriteLengthedString(Password);
            bw.WriteLengthedString(PublicAddress.ToString());
        }
    }
}
