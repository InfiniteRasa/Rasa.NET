using System.IO;
using System.Text;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class LoginRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.LoginRequest;
        public byte ServerId { get; set; }
        public string Password { get; set; }

        public void Read(BinaryReader br)
        {
            ServerId = br.ReadByte();
            Password = Encoding.UTF8.GetString(br.ReadBytes(br.ReadInt32()));
        }

        public void Write(BinaryWriter bw)
        {
            var pwBytes = Encoding.UTF8.GetBytes(Password);

            bw.Write((byte) Opcode);
            bw.Write(ServerId);
            bw.Write(pwBytes.Length);
            bw.Write(pwBytes);
        }
    }
}
