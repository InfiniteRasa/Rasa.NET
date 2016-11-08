using System;
using System.IO;
using Rasa.Extensions;

namespace Rasa.Packets.Queue.Server
{
    using Data;

    public class ClientKeyOkPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.ClientKeyOk;

        public void Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public void Write(BinaryWriter bw)
        {
            bw.WriteUtf8String("ENC OK");
        }
    }
}
