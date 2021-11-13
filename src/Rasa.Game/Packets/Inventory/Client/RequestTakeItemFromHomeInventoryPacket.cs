namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class RequestTakeItemFromHomeInventoryPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTakeItemFromHomeInventory;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            DestSlot = pr.ReadUInt();
            Quantity = pr.ReadInt();
        }
    }
}
