using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class PreloadDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreloadData;

        public uint WeaponId { get; set; }
        public Dictionary <int, AbilityDrawerData> AbilitiesList { get; set; } = new Dictionary<int, AbilityDrawerData>();
        
        public PreloadDataPacket(uint weaponId, Dictionary<int, AbilityDrawerData> abilitiesList)
        {
            WeaponId = weaponId;
            AbilitiesList = abilitiesList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WeaponId);
            pw.WriteList(AbilitiesList.Count);
            foreach (var entry in AbilitiesList)
            {
                var ability = entry.Value;

                pw.WriteTuple(2);
                pw.WriteInt(ability.AbilityId);
                pw.WriteInt(ability.AbilityLevel);
            }
        }
    }
}
