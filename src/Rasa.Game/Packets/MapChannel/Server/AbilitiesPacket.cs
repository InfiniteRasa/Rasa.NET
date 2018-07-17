using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AbilitiesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Abilities;

        public Dictionary<int, SkillsData> AbilityList = new Dictionary<int, SkillsData>();

        public AbilitiesPacket(Dictionary<int, SkillsData> abilityList)
        {
            foreach (var ability in abilityList)
                if (abilityList[ability.Key].AbilityId != -1)   // don't insert if there is no ablilityId
                    AbilityList.Add(ability.Key, new SkillsData(ability.Value.SkillId, ability.Value.AbilityId, ability.Value.SkillLevel));
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
