using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformWindupPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformWindup;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public List<int> Args { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(ActionId);
            pw.WriteInt(ActionArgId);
            pw.WriteNoneStruct();
            //pw.WriteList(Args.Count);   // ToDo
        }
    }
}
