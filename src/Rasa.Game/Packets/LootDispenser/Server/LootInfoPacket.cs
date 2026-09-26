using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class LootInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LootInfo;

        private readonly LootItem[] _lootItems;
        
        public LootInfoPacket(List<LootItem> lootItems)
        {
            _lootItems = lootItems?.ConvertAll(LootItem.Capture).ToArray()
                         ?? System.Array.Empty<LootItem>();
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(_lootItems.Length);
            foreach(var item in _lootItems)
            {
                pw.WriteULong(item.EntityId);
                pw.WriteTuple(4);
                pw.WriteUInt(item.ItemTemplateId);
                pw.WriteUInt(item.ItemQuantity);
                pw.WriteULong(item.ActorId);
                if (item.PartyId > 0)
                    pw.WriteUInt(item.PartyId);
                else
                    pw.WriteNoneStruct();
            }
        }
    }
}
