using System;
using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class RedirectRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.RedirectRequest;

        public void Read(BinaryReader br)
        {

        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Opcode);
        }
    }
}
