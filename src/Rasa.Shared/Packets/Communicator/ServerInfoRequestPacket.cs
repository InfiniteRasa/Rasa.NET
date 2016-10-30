using System;
using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    public class ServerInfoRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.ServerInfoRequest;

        public void Read(BinaryReader br)
        {
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
        }
    }
}
