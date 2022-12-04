namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AdvancementStatsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AdvancementStats;

        public int Level { get; set; }
        public int Experience { get; set; }
        public int Attributes { get; set; }
        public int TrainPts { get; set; }
        public int SkillPts { get; set; }

        public AdvancementStatsPacket(int level, int experience, int attributes, int trainPts, int skillPts)
        {
            Level = level;
            Experience = experience;
            Attributes = attributes;
            TrainPts = trainPts;
            SkillPts = skillPts;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteInt(Level);
            pw.WriteInt(Experience);
            pw.WriteInt(Attributes);
            pw.WriteInt(TrainPts);  // not used by client ???
            pw.WriteInt(SkillPts);
        }
    }
}
