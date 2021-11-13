using System;

namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class RequestMoveItemToHomeInventoryPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestMoveItemToHomeInventory;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();
            DestSlot = pr.ReadUInt();
            if (pr.PeekType() == PythonType.Int)
                Quantity = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Long)
                Quantity = (int)pr.ReadLong();
            else
                throw new Exception("RequestMoveItemToHomeInventory: unsuported PythonType");
        }
    }
}
