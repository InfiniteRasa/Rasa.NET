using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SkillsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Skills;

        public static Dictionary<SkillId, SkillsData> SkillsData { get; set; } = new Dictionary<SkillId, SkillsData>();
        
        public SkillsPacket(Dictionary<SkillId, SkillsData> skillsData)
        {
            SkillsData = skillsData;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(SkillsData.Count);
            foreach(var entry in SkillsData)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)entry.Value.SkillId);
                pw.WriteInt(entry.Value.SkillLevel);
            }
        }
    }
}
