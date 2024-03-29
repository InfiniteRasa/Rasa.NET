﻿namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    public class AddBuybackItemPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddBuybackItem;

        public ulong EntityId { get; set; }
        public int BuyBackPrice { get; set; }
        public int Sequence { get; set; }

        public AddBuybackItemPacket(ulong entityId, int buybackPrice, int sequence)
        {
            EntityId = entityId;
            BuyBackPrice = buybackPrice;
            Sequence = sequence;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(EntityId);
            pw.WriteInt(BuyBackPrice);
            pw.WriteInt(Sequence);
        }
    }
}
