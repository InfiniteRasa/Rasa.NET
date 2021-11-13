namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    public class RemoveBuybackItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveBuybackItem;

        public ulong EntityId { get; set; }

        public RemoveBuybackItemPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(EntityId);
        }
    }
}
