namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class InventoryAddItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryAddItem;
        
        public InventoryType Type { get; set; }
        public uint EntityId { get; set; }
        public int SlotId { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteInt((int)EntityId);
            pw.WriteInt(SlotId);

        }
    }
}
