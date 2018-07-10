using System;
using System.IO;

namespace Rasa.Packets
{
    using Data;
    using Memory;

    public abstract class PythonPacket : IOpcodedPacket<GameOpcode>
    {
        public abstract GameOpcode Opcode { get; }

        public void Read(BinaryReader br)
        {
            using (var pr = new PythonReader(br))
                Read(pr);
        }

        public void Write(BinaryWriter bw)
        {
            using (var pw = new PythonWriter(bw))
                Write(pw);
        }

        public abstract void Read(PythonReader pr);
        public abstract void Write(PythonWriter pw);
    }

    public abstract class ClientPythonPacket : PythonPacket
    {
        public override sealed void Write(PythonWriter pw)
        {
            throw new InvalidOperationException();
        }
    }

    public abstract class ServerPythonPacket : PythonPacket
    {
        public override sealed void Read(PythonReader pw)
        {
            throw new InvalidOperationException();
        }
    }
}
