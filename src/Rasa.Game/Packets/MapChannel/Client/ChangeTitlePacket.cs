using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeTitlePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeTitle;

        public int TitleId { get; set; }
        public int EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("changetitle\n{0}", pr.ToString());
            pr.ReadTuple();
            if (pr.PeekType() == PythonType.Int)
                TitleId = pr.ReadInt();
            else
                pr.ReadNoneStruct();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(TitleId);
            pw.WriteInt((int)EntityId);
                  
        }
    }
}
