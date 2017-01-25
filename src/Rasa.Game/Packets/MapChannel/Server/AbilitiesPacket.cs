using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AbilitiesPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Abilities;

        public Dictionary<int, SkillsData> AbilityList = new Dictionary<int, SkillsData>();

        public AbilitiesPacket(Dictionary<int, SkillsData> abilityList)
        {
            foreach (var entry in abilityList)
                if (abilityList[entry.Key].AbilityId != -1)   // don't insert if there is no ablilityId
                    AbilityList.Add(entry.Key, new SkillsData { AbilityId = entry.Value.AbilityId, SkillLevel = entry.Value.SkillLevel } );
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(AbilityList.Count);
            foreach (var entry in AbilityList)
            {
                pw.WriteTuple(2);
                pw.WriteInt(entry.Value.AbilityId);
                pw.WriteInt(entry.Value.SkillLevel);
            }
        }
    }
}
