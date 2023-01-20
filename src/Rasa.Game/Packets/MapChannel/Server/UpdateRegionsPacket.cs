using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class UpdateRegionsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateRegions;

        public List<uint> RegionsList { get; set; }
        public uint RegionIdList { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(1);
            pw.WriteUInt(RegionIdList);
           /* pw.WriteList(RegionsList.Count);
            foreach ( var region in RegionsList)
                pw.WriteInt(region);
                */
        }
    }
}
