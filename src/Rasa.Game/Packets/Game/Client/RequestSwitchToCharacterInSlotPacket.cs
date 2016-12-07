namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    public class RequestSwitchToCharacterInSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public int SlotNum { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotNum = pr.ReadInt();
            pr.ReadZeroStruct();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
