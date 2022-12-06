using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ActorGotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorGotLoot;
        
        public LootDispenser Loot { get; set; }

        public ActorGotLootPacket(LootDispenser loot)
        {
            Loot = loot;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(Loot.AttachedTo);
            pw.WriteList(Loot.LootItems.Count);
            foreach (var item in Loot.LootItems)
            {
                pw.WriteTuple(1);
                pw.WriteULong(item.EntityId);
            }
        }
    }
}
