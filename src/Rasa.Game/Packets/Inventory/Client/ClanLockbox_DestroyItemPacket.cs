namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class ClanLockbox_DestroyItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanLockbox_DestroyItem;

        public ulong EntityId { get; set; }
        public uint Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            Quantity = (uint)pr.ReadLong();
        }
    }
}
