namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponDrawerSlotPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponDrawerSlot;

        public uint SlotNum { get; set; }
        public bool Requested { get; set;}

        public WeaponDrawerSlotPacket(uint slotNum, bool requested)
        {
            SlotNum = slotNum;
            Requested = requested;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(SlotNum);
            pw.WriteBool(Requested);
        }
    }
}
