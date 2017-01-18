namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class WeaponDrawerInventory_MoveItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponDrawerInventory_MoveItem;
        
        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            DestSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
