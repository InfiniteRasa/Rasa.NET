namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestSwapAbilitySlotsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwapAbilitySlots;
        
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FromSlot = (int)pr.ReadLong();
            ToSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
