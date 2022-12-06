using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class TakenInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TakenInfo;

        public List<LootItem> LootItems { get; set; } = new List<LootItem>();

        public TakenInfoPacket(ulong actorId, List<LootItem> lootItems)
        {
            LootItems = lootItems;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(LootItems.Count);
            foreach (var item in LootItems)
            {
                pw.WriteULong(item.EntityId);
                pw.WriteBool(true);
            }
        }
    }
}
