using System.IO;
using System.Net;
using System.Net.Sockets;

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
            PublicAddress = new IPAddress(br.ReadBytes(br.ReadByte()));
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(ServerId);
            bw.WriteLengthedString(Password);
            bw.Write((byte) (PublicAddress.AddressFamily == AddressFamily.InterNetwork ? 4 : 16));
            bw.Write(PublicAddress.GetAddressBytes());
        }
    }
}
