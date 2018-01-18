namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestEquipWeaponPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipWeapon;

        public int SrcSlot { get; set; }
        public InventoryType InventoryType { get; set; }
        public int DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            InventoryType = (InventoryType)pr.ReadInt();
            DestSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
