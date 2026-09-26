using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// LootDispenser.Recv_ActorGotLoot(actorId, lootEntityIds), on the dispenser: when actorId is
    /// the recipient's own manifestation it plays the pick-up sound for the best of the item ids,
    /// and does nothing else. It is not sent: GotLoot plays the same sound with the loot messages,
    /// and both would play it twice. Kept with the layout the client reads - the looter's id (it
    /// was written as the corpse's, so the sound never played) and bare item ids (they were
    /// one-element tuples).
    /// </summary>
    public class ActorGotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorGotLoot;

        public ulong ActorId { get; }
        public List<ulong> ItemIds { get; }

        public ActorGotLootPacket(ulong actorId, List<ulong> itemIds)
        {
            ActorId = actorId;
            ItemIds = itemIds == null ? new List<ulong>() : new List<ulong>(itemIds);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(ActorId);
            pw.WriteList(ItemIds.Count);
            foreach (var id in ItemIds)
                pw.WriteULong(id);
        }
    }
}
