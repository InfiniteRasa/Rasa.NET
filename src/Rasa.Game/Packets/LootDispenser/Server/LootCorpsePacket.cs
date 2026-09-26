using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// Opens the corpse window. lootdispenser.Recv_LootCorpse(actorId, lootItems) stores the
    /// items and, when actorId is the receiver's own manifestation and they are still within use
    /// range of the corpse, posts UI_SHOW_CORPSELOOT.
    ///
    /// So actorId is the looting player's manifestation id, not the corpse's, and this is sent
    /// on the dispenser's entity id. Sending it with anyone else's actorId is how a bystander is
    /// told what is on a corpse without their window opening.
    ///
    /// lootItems has the shape lootdispenser.GetLootItemDict documents:
    /// entityId : (itemTemplateId, itemQuantity, actorId, partyId).
    /// </summary>
    public class LootCorpsePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LootCorpse;

        public ulong ActorId { get; }
        private readonly LootItem[] _lootItems;

        public LootCorpsePacket(ulong actorId, List<LootItem> lootItems)
        {
            ActorId = actorId;
            _lootItems = lootItems?.ConvertAll(LootItem.Capture).ToArray()
                         ?? System.Array.Empty<LootItem>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(ActorId);
            pw.WriteDictionary(_lootItems.Length);

            foreach (var item in _lootItems)
            {
                pw.WriteULong(item.EntityId);
                pw.WriteTuple(4);
                pw.WriteUInt(item.ItemTemplateId);
                pw.WriteUInt(item.ItemQuantity);
                pw.WriteULong(item.ActorId);

                // The client unpacks partyId but only compares it; 0 is not a party, so None.
                if (item.PartyId > 0)
                    pw.WriteUInt(item.PartyId);
                else
                    pw.WriteNoneStruct();
            }
        }
    }
}
