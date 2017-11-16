namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    public class RequestDeleteCharacterInSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestDeleteCharacterInSlot;

        public uint SlotId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotId = pr.ReadUInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
