using System;
namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class HomeInventory_DestroyItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.HomeInventory_DestroyItem;

        public long EntityId { get; set; }
        public long Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadLong();
            Quantity = pr.ReadLong();
        }
    }
}
