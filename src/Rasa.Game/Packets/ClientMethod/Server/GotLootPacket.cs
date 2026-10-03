using System.Collections.Generic;

namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// clientmethod.Recv_GotLoot(creatureEntityId, entityClassIdList, moneyAmount): what one take
    /// from a corpse gave this player. Each (classId, quantity, itemId) is a "You received N X."
    /// in the loot filter and a pick-up on the status updater, the credits are "You received N
    /// credits.", and the pick-up sound is played for the best of them. It says only what this
    /// player was given by this take: the items they took, and their share of the credits.
    ///
    /// The client names each item itself, from the class id, in its own language
    /// (GetEntityClassName), and never reads creatureEntityId; so this is also how a vendor
    /// purchase is announced (NpcManager.RequestVendorPurchase), with the vendor as the source.
    /// </summary>
    public class GotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GotLoot;

        public ulong CreatureEntityId { get; }
        public List<LootItem> Items { get; }
        public int Credits { get; }

        // Queued packets are written later; what they say is taken when they are made.
        private readonly List<(uint ClassId, uint Quantity, ulong EntityId)> _entries = new List<(uint, uint, ulong)>();

        public GotLootPacket(ulong creatureEntityId, List<LootItem> items, int credits)
        {
            CreatureEntityId = creatureEntityId;
            Items = items == null ? new List<LootItem>() : new List<LootItem>(items);
            Credits = credits;

            foreach (var item in Items)
                _entries.Add((item.ItemClassId, item.ItemQuantity, item.EntityId));
        }

        /// <summary>One item that did not come from a corpse: <paramref name="quantity"/> of the class, and the item it went to (0 for none in particular).</summary>
        public GotLootPacket(ulong sourceEntityId, uint classId, uint quantity, ulong itemEntityId)
        {
            CreatureEntityId = sourceEntityId;
            Items = new List<LootItem>();
            Credits = 0;
            _entries.Add((classId, quantity, itemEntityId));
        }

        /// <summary>What goes out: (classId, quantity, itemId) for each item.</summary>
        public IReadOnlyList<(uint ClassId, uint Quantity, ulong EntityId)> Entries => _entries;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(CreatureEntityId);
            pw.WriteList(_entries.Count);
            foreach (var (classId, quantity, entityId) in _entries)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(classId);
                pw.WriteUInt(quantity);
                pw.WriteULong(entityId);
            }
            pw.WriteInt(Credits);
        }
    }
}
