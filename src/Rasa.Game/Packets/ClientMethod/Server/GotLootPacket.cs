namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;
    using Structures;

    public class GotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GotLoot;

        public LootDispenser Loot { get; set; }

        public GotLootPacket(LootDispenser loot)
        {
            Loot = loot;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(Loot.AttachedTo);
            pw.WriteList(Loot.LootItems.Count);
            foreach (var item in Loot.LootItems)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(item.ItemClassId);
                pw.WriteUInt(item.ItemQuantity);
                pw.WriteULong(item.EntityId);
            }
            pw.WriteInt(Loot.Credits);
        }
    }
}
