namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class InventoryRemoveItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryRemoveItem;

        public InventoryType InventoryType { get; set; }
        public int EntityId { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt((int)InventoryType);
            pw.WriteInt(EntityId);
        }
    }
}
