using System;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class UpdateRegionsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateRegions;

        public int RegionIdList { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("UpdateRegions Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(1);
            pw.WriteInt(RegionIdList);
            Console.WriteLine("UpdateRegions Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
