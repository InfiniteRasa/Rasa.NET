using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class HomeInventory_MoveItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.HomeInventory_MoveItem;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            DestSlot = pr.ReadInt();
            if (pr.PeekType() == PythonType.Int)
                Quantity = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Long)
                Quantity = (int)pr.ReadLong();
            else
                throw new Exception("HomeInventory_MoveItem: unsuported PythonType");
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
