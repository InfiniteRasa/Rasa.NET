namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestTakeItemFromHomeInventoryPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTakeItemFromHomeInventory;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            DestSlot = pr.ReadInt();
            Quantity = pr.ReadInt();
        }
    }
}
