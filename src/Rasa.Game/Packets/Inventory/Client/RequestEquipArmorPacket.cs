namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class RequestEquipArmorPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipArmor;

        public uint SrcSlot { get; set; }        // Source Slot
        public InventoryType SrcInventory { get; set; }   // Source Inventory
        public uint DestSlot { get; set; }       // Destination Slot

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            SrcInventory = (InventoryType)pr.ReadInt();
            DestSlot = pr.ReadUInt();
        }
    }
}