using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class CanLootItemsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CanLootItems;

        public bool CanLootItems { get; }
        private readonly ulong[] _itemEntityIds;
        
        public CanLootItemsPacket(bool canLootItems, List<LootItem> lootItems)
        {
            CanLootItems = canLootItems;
            _itemEntityIds = lootItems?.ConvertAll(item => item.EntityId).ToArray()
                             ?? System.Array.Empty<ulong>();
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteBool(CanLootItems);
            if (CanLootItems)
            {
                pw.WriteDictionary(_itemEntityIds.Length);
                foreach (var entityId in _itemEntityIds)
                {
                    pw.WriteULong(entityId);
                    pw.WriteTuple(1);
                    pw.WriteBool(CanLootItems);
                }
            }
            else
                pw.WriteNoneStruct();
        }
    }
}
