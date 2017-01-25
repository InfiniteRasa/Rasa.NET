namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponDrawerSlotPacket : PythonPacket
    {
        // 574 Recv_WeaponDrawerSlot(self, slotNum, bRequested = True):
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponDrawerSlot;

        public int RequestedWeaponDrawerSlot { get; set; }
        // public bool Requested { get; set;}   // do we need send this too?

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(RequestedWeaponDrawerSlot);
        }
    }
}
