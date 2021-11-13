namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class RequestEquipWeaponPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipWeapon;

        public uint SrcSlot { get; set; }
        public InventoryType InventoryType { get; set; }
        public uint DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            InventoryType = (InventoryType)pr.ReadInt();
            DestSlot = pr.ReadUInt();
        }
    }
}
