namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;
    using Structures;

    public class GotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GotLoot;

        private readonly ulong _attachedTo;
        private readonly LootItem[] _items;
        private readonly int _credits;

        public GotLootPacket(LootDispenser loot)
        {
            _attachedTo = loot?.AttachedTo ?? 0;
            _items = loot?.LootItems.ConvertAll(LootItem.Capture).ToArray()
                     ?? System.Array.Empty<LootItem>();
            _credits = loot?.Credits ?? 0;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(_attachedTo);
            pw.WriteList(_items.Length);
            foreach (var item in _items)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(item.ItemClassId);
                pw.WriteUInt(item.ItemQuantity);
                pw.WriteULong(item.EntityId);
            }
            pw.WriteInt(_credits);
        }
    }
}
