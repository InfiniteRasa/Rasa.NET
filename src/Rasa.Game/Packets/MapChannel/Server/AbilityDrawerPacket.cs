using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AbilityDrawerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbilityDrawer;

        public Dictionary<int, AbilityDrawerData> Abilities = new Dictionary<int, AbilityDrawerData>();

        public AbilityDrawerPacket(Dictionary<int, AbilityDrawerData> abilities)
        {
            Abilities = abilities;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(Abilities.Count);
            foreach (var entry in Abilities)
            {
                pw.WriteInt(entry.Value.AbilitySlotId); // slotId            
                pw.WriteTuple(3);
                pw.WriteInt(entry.Value.AbilityId);     // abilityId
                pw.WriteUInt(entry.Value.AbilityLevel);  // abilityLevel
                pw.WriteNoneStruct();                   // itemId ( unknown purpose ) <<= c++  krssrb =>> if you drag 'n' drop usable iteme from inventory
            }
        }
    }
}
