using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SkillsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Skills;

        public static Dictionary<int, SkillsData> SkillsData { get; set; } = new Dictionary<int, SkillsData>();
        
        public SkillsPacket(Dictionary<int, SkillsData> skillsData)
        {
            SkillsData = skillsData;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(SkillsData.Count);
            foreach(var entry in SkillsData)
            {
                pw.WriteTuple(2);
                pw.WriteInt(entry.Value.SkillId);
                pw.WriteInt(entry.Value.SkillLevel);
            }
        }
    }
}
