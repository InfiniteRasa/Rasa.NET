namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class ClanLockbox_DepositItemInTabPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanLockbox_DepositItemInTab;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public long Quantity { get; set; }
        public bool ManagePersonalSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            
            if (pr.PeekType() == PythonType.Int)
                DestSlot = pr.ReadInt();
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
