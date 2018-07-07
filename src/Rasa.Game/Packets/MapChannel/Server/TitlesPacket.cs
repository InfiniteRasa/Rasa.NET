using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TitlesPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Titles;

        public List<uint> Titles { get; set; }

        public TitlesPacket(List<uint> titles)
        {
            Titles = titles;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Titles.Count);
            foreach (var title in Titles)
                pw.WriteUInt(title);
        }
    }
}