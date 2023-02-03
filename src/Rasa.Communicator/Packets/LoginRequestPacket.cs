using System.IO;
using System.Net;
using System.Net.Sockets;

using Rasa.Packets;

namespace Rasa.Communicator.Packets;

using Rasa.Extensions;

public class LoginRequestPacket : IOpcodedPacket<CommunicatorOpcode>
{
    public CommunicatorOpcode Opcode { get; } = CommunicatorOpcode.LoginRequest;
    public ServerData Data { get; set; }

    public LoginRequestPacket()
    {
        Data = new();
    }

    public LoginRequestPacket(ServerData info)
    {
        Data = info;
    }

    public void Read(BinaryReader br)
    {
        Data.Id = br.ReadByte();
        Data.Password = br.ReadLengthedString();
        Data.Address = new IPAddress(br.ReadBytes(br.ReadByte()));
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write((byte)Opcode);
        bw.Write(Data.Id);
        bw.WriteLengthedString(Data.Password);
        bw.Write((byte)(Data.Address.AddressFamily == AddressFamily.InterNetwork ? 4 : 16));
        bw.Write(Data.Address.GetAddressBytes());
    }
}
