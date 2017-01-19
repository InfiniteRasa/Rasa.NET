namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PersonalInventory_DestroyItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PersonalInventory_DestroyItem;

        public long ItemId { get; set; }
        public long Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ItemId = pr.ReadLong();
            Quantity = pr.ReadLong();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
