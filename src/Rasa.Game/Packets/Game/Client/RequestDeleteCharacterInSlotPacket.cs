using System;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    public class RequestDeleteCharacterInSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestDeleteCharacterInSlot;

        public int SlotNum { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("RequestDeleteCharacterInSlot Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
            pr.ReadTuple();
            SlotNum = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(SlotNum);
            Console.WriteLine("RequestDeleteCharacterInSlot Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
