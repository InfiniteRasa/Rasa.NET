namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class LevelSkillsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LevelSkills;
        
        public int ListLenght { get; set; }
        public int[] SkillIds { get; set; }
        public int[] SkillLevels { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ListLenght = pr.ReadList();
            SkillIds = new int[ListLenght];
            SkillLevels = new int[ListLenght];
            for (var i = 0; i < ListLenght; i++)
            {
                pr.ReadTuple();
                SkillIds[i] = pr.ReadInt();
                SkillLevels[i] = pr.ReadInt();
            }
        }
    }
}
