namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ClanLockbox_DepositItemInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanLockbox_DepositItemInSlot;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public long Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            DestSlot = pr.ReadUInt();

            if (pr.PeekType() == PythonType.Long)
                Quantity = pr.ReadLong();
            else
                Quantity = pr.ReadInt();
        }
    }
}
