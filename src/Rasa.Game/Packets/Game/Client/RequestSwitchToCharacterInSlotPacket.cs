using System;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    public class RequestSwitchToCharacterInSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public int SlotNum { get; set; }
        public int CanSkipBootcamp { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("RequestSwitchToCharacterInSlot Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
            pr.ReadTuple();
            SlotNum = pr.ReadInt();
            CanSkipBootcamp = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(SlotNum);
            pw.WriteInt(CanSkipBootcamp);
            Console.WriteLine("RequestSwitchToCharacterInSlot Read\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
