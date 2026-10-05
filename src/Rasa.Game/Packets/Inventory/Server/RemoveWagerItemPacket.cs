namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>client/inventory.py Recv_RemoveWagerItem(entityId): the wager slot emptied. The id is the item that was in it; the client logs an error when it is not.</summary>
    public class RemoveWagerItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveWagerItem;

        public ulong EntityId { get; set; }

        public RemoveWagerItemPacket(ulong entityId)
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
