using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class UpdateRegionsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateRegions;

        public List<int> RegionsList { get; set; }
        public int RegionIdList { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(1);
            pw.WriteInt(RegionIdList);
           /* pw.WriteList(RegionsList.Count);
            foreach ( var region in RegionsList)
                pw.WriteInt(region);
                */
        }
    }
}
