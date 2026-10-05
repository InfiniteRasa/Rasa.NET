namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/inventory.py Recv_AddWagerItem(entityId): the item in the wager slot of the prestige
    /// window. The client holds one, and a second replaces the first.
    /// </summary>
    public class AddWagerItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddWagerItem;

        public ulong EntityId { get; set; }

        public AddWagerItemPacket(ulong entityId)
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
