using System.IO;

using Rasa.Packets;

namespace Rasa.Communicator.Packets
{
    public class RedirectResponsePacket : IOpcodedPacket<CommunicatorOpcode>
    {
        public CommunicatorOpcode Opcode { get; } = CommunicatorOpcode.RedirectResponse;
        public CommunicatorActionResult Result { get; set; }
        public uint AccountId { get; set; }

        public void Read(BinaryReader br)
        {
            Result = (CommunicatorActionResult)br.ReadByte();
            AccountId = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Opcode);
            bw.Write((byte)Result);
            bw.Write(AccountId);
        }
    }
}
