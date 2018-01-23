namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponDrawerSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponDrawerSlot;

        public int SlotNum { get; set; }
        public bool Requested { get; set;}

        public WeaponDrawerSlotPacket(int slotNum, bool requested)
        {
            SlotNum = slotNum;
            Requested = requested;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(SlotNum);
            pw.WriteBool(Requested);
        }
    }
}
