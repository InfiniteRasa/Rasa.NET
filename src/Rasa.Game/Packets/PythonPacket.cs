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
}
