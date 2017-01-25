namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AbilityDrawerSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbilityDrawerSlot;

        public int AbilityDrawerSlot { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(AbilityDrawerSlot);
        }
    }
}
