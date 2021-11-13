namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class WeaponDrawerInventory_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponDrawerInventory_MoveItem;
        
        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            DestSlot = pr.ReadUInt();
        }
    }
}
