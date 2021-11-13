namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class ClanLockbox_WithdrawItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanLockbox_WithdrawItem;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public long Quantity { get; set; }
        public bool ManagePersonalSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            
            if (pr.PeekType() == PythonType.Int)
                DestSlot = pr.ReadUInt();
            else
            {
                ManagePersonalSlot = true;
                pr.ReadNoneStruct();
            }

            if (pr.PeekType() == PythonType.Long)
                Quantity = pr.ReadLong();
            else
                Quantity = pr.ReadInt();
        }
    }
}
