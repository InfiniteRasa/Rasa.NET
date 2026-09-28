using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ActorGotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorGotLoot;
        
        public ulong AttachedTo { get; }
        public ulong[] ItemEntityIds { get; }

        public ActorGotLootPacket(LootDispenser loot)
        {
            AttachedTo = loot?.AttachedTo ?? 0;
            ItemEntityIds = loot?.LootItems.ConvertAll(item => item.EntityId).ToArray()
                            ?? System.Array.Empty<ulong>();
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(AttachedTo);
            pw.WriteList(ItemEntityIds.Length);
            foreach (var entityId in ItemEntityIds)
            {
                pw.WriteTuple(1);
                pw.WriteULong(entityId);
            }
        }
    }
}
