using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AbilitiesPacket :PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Abilities;

        public int AbilityId { get; set; }
        public int SkillLevel { get; set; }

        public static List<int> AbilityIdList { get; set; } = new List<int>();
        public static List<int> SkillIndexLevelList { get; set; } = new List<int>();

        private static readonly int[] AbilityById = {
            1,8,14,19,20,21,22,23,24,
            25,26,28,30,31,32,34,35,
            36,37,39,40,43,47,48,49,
            50,54,55,57,58,63,66,67,
            68,72,73,77,79,80,82,89,
            92,102,110,111,113,114,121,135,
            136,147,148,149,150,151,152,153,
            154,155,156,157,158,159,160,161,
            162,163,164,165,166,172,173,174
        };

        // ToDo read from DB skill lv
        private static readonly int[] SkillIByIndex = {
            0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
        };

        public AbilitiesPacket()
        {
            AbilityIdList.AddRange(AbilityById);
            SkillIndexLevelList.AddRange(SkillIByIndex);
        }
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(73);
            for (var i = 0; i < 73; i++)
            {
                pw.WriteTuple(2);
                pw.WriteInt(AbilityIdList[i]);
                pw.WriteInt(SkillIndexLevelList[i]);
            }
        }
    }
}
