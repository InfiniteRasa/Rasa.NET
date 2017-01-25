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
            pr.ReadTuple();
            SlotNum = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
