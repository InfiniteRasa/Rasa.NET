using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// The skills the player's own client has (actor.Recv_Skills), as (skill id, level). Copied
    /// when the packet is made: the packet is encoded when it goes out, and the list it was made
    /// from was a static property every SkillsPacket shared, so two made before either went out
    /// both carried the second one's skills.
    /// </summary>
    public class SkillsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Skills;

        public IReadOnlyList<(SkillId SkillId, int SkillLevel)> Skills { get; }

        public SkillsPacket(Dictionary<SkillId, SkillsData> skillsData)
        {
            Skills = skillsData.Values.Select(skill => (skill.SkillId, skill.SkillLevel)).ToList();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Skills.Count);
            foreach (var (skillId, skillLevel) in Skills)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)skillId);
                pw.WriteInt(skillLevel);
            }
        }
    }
}
