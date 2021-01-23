using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorRepairPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorRepair;

        public ulong VendorEntityId { get; set; }
        public List<ulong> ItemEntitesId = new List<ulong>();

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadULong();
            var count = pr.ReadList();
            for (var i = 0; i < count; i++)
                ItemEntitesId.Add(pr.ReadULong());
        }
    }
}
