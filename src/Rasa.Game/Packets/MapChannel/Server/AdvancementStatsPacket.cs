using System;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AdvancementStatsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AdvancementStats;

        public int Level { get; set; }
        public int Experience { get; set; }
        public int Attributes { get; set; }
        public int TrainPts { get; set; }
        public int SkillPts { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("AdvancementStats Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteInt(Level);
            pw.WriteInt(Experience);
            pw.WriteInt(Attributes);
            pw.WriteInt(TrainPts);  // not used by client ???
            pw.WriteInt(SkillPts);
            Console.WriteLine("AdvancementStats Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
