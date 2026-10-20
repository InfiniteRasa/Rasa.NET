using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// What a client should load ahead for an actor: the weapon in its hand and its abilities
    /// (actor.py Recv_PreloadData(weaponId, abilities)). The weapon goes to
    /// gameclient.PreloadAssetsForClassId, as every other caller's class id does: it is the held
    /// item's class, not its entity id, and 0 - nothing to load - for an empty hand.
    /// </summary>
    public class PreloadDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreloadData;

        public uint WeaponClassId { get; set; }
        public Dictionary <int, AbilityDrawerData> AbilitiesList { get; set; } = new Dictionary<int, AbilityDrawerData>();
        
        public PreloadDataPacket(uint weaponClassId, Dictionary<int, AbilityDrawerData> abilitiesList)
        {
            WeaponClassId = weaponClassId;
            AbilitiesList = abilitiesList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WeaponClassId);
            pw.WriteList(AbilitiesList.Count);
            foreach (var entry in AbilitiesList)
            {
                var ability = entry.Value;

                pw.WriteTuple(2);
                pw.WriteInt(ability.AbilityId);
                pw.WriteUInt(ability.AbilityLevel);
            }
        }
    }
}
