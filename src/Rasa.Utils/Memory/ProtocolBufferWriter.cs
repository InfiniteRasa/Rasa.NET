using System;
using System.IO;

namespace Rasa.Memory
{
    public class ProtocolBufferWriter : IDisposable
    {
        public BinaryWriter Writer { get; }
        public byte Flags { get; }

        public ProtocolBufferWriter(BinaryWriter writer, byte flags)
        {
            Writer = writer;
            Flags = flags;
        }

        public void WriteProtocolFlags()
        {

        }

        public void WriteUInt(uint value)
        {

        }

        public void WriteString(string value)
        {

        }

        public void Dispose()
        {
        }
    }
}
