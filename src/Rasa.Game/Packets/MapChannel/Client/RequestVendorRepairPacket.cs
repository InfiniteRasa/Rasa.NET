using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorRepairPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorRepair;

        public long VendorEntityId { get; set; }
        public List<long> ItemEntitesId = new List<long>();

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            var count = pr.ReadList();
            for (var i = 0; i < count; i++)
                ItemEntitesId.Add(pr.ReadInt());
        }
    }
}
