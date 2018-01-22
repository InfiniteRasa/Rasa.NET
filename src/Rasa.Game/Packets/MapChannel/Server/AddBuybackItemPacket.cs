namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AddBuybackItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddBuybackItem;

        public uint EntityId { get; set; }
        public int BuyBackPrice { get; set; }
        public int Sequence { get; set; }

        public AddBuybackItemPacket(uint entityId, int buybackPrice, int sequence)
        {
            EntityId = entityId;
            BuyBackPrice = buybackPrice;
            Sequence = sequence;
        }


        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(EntityId);
            pw.WriteInt(BuyBackPrice);
            pw.WriteInt(Sequence);
        }
    }
}
