using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class PreloadDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreloadData;

        public int WeaponId { get; set; }
        public List<AbilityDrawerData> AbilitiesList { get; set; } = new List<AbilityDrawerData>(); //ToDo

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(WeaponId);
            pw.WriteList(AbilitiesList.Count);
            foreach (var ability in AbilitiesList)
            {
                pw.WriteTuple(2);
                pw.WriteInt(ability.AbilityId);
                pw.WriteInt(ability.AbilityLevel);
            }
        }
    }
}
