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

        /// <summary>The entity id of the item behind each item slot, found in the player's pack when the drawer is sent (AbilityDrawerItems).</summary>
        public Dictionary<int, ulong> ItemEntities = new Dictionary<int, ulong>();

        /// <param name="player">Whose drawer: an item slot is sent with the entity id of the item in their pack.</param>
        public AbilityDrawerPacket(Dictionary<int, AbilityDrawerData> abilities, Manifestation player)
        {
            foreach (var entry in abilities)
            {
                Abilities[entry.Key] = new AbilityDrawerData(
                    entry.Value.AbilitySlotId,
                    entry.Value.AbilityId,
                    entry.Value.AbilityLevel,
                    entry.Value.ItemId);

                if (Managers.AbilityDrawerItems.EntityOf(player, entry.Value) is ulong itemEntityId)
                    ItemEntities[entry.Key] = itemEntityId;
            }
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

                // itemId: the item a usable item's slot fires with, None for a skill's ability.
                if (ItemEntities.TryGetValue(entry.Key, out var itemEntityId))
                    pw.WriteULong(itemEntityId);
                else
                    pw.WriteNoneStruct();
            }
        }
    }
}
