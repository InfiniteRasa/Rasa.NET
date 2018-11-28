namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class InventoryAddItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryAddItem;
        
        public InventoryType Type { get; set; }
        public uint EntityId { get; set; }
        public uint SlotId { get; set; }

        public InventoryAddItemPacket(InventoryType type, uint entityId, uint slotId)
        {
            Type = type;
            EntityId = entityId;
            SlotId = slotId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteUInt(EntityId);
            pw.WriteUInt(SlotId);

        }
    }
}
