using System.IO;

namespace Rasa.Packets.Communicator;

using Rasa.Data;

public class RedirectResponsePacket : IOpcodedPacket<CommOpcode>
{
    public CommOpcode Opcode { get; } = CommOpcode.RedirectResponse;
    public RedirectResult Response { get; set; }
    public uint AccountId { get; set; }

    public void Read(BinaryReader br)
    {
        Response = (RedirectResult) br.ReadByte();
        AccountId = br.ReadUInt32();
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write((byte) Opcode);
        bw.Write((byte) Response);
        bw.Write(AccountId);
    }
}
