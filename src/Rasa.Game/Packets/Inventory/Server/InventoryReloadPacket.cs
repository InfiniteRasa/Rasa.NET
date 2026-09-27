using System.Collections.Generic;

namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// InventoryReload (879), on SysEntity.ClientInventoryManagerId: <c>(inventoryType, itemList,
    /// maxSize)</c> - one inventory's whole contents again. client/inventory.py
    /// Recv_InventoryReload calls CreateInventory, as InventoryCreate does: the inventory is reset,
    /// maxSize kept for the personal, home and clan inventories (home also asks for its lockbox tab
    /// permissions again), each entry added, and the inventory's window told to redraw.
    ///
    /// itemList is (entityId, slot) pairs for the slotted inventories; for the auction, inbox and
    /// wager inventories, which CreateInventory reads without slots, it is bare entity ids.
    ///
    /// Empty slots are left out. The server keeps a slotted inventory as a list indexed by slot
    /// with 0 in the empty ones, and sending it whole put an (0, slot) entry in for each: the client
    /// files entity 0 like any other, moving it from slot to slot until it rested in the last empty
    /// one, where the lockbox drew an item that is not there (and a weapon drawer asked for the
    /// assets of an entity with no class) - and a 500-slot clan lockbox went out as 500 entries to
    /// every online member at every change.
    /// </summary>
    public class InventoryReloadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryReload;

        public InventoryType InventoryType { get; }

        /// <summary>By slot, 0 for an empty slot; or, for a slotless inventory, the entity ids in order.</summary>
        public IReadOnlyList<ulong> Items { get; }

        public int MaxSize { get; }

        public InventoryReloadPacket(InventoryType inventoryType, IReadOnlyList<ulong> items, int maxSize)
        {
            InventoryType = inventoryType;
            Items = items ?? new List<ulong>();
            MaxSize = maxSize;
        }

        /// <summary>CreateInventory's reading: the auction, inbox and wager inventories carry bare entity ids.</summary>
        public static bool IsSlotless(InventoryType type) =>
            type is InventoryType.AuctionInventory or InventoryType.InboxInventory or InventoryType.WagerInventory;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)InventoryType);
            WriteItems(pw, InventoryType, Items);
            pw.WriteInt(MaxSize);
        }

        /// <summary>The itemList InventoryCreate and InventoryReload share, empty slots left out.</summary>
        internal static void WriteItems(PythonWriter pw, InventoryType type, IReadOnlyList<ulong> items)
        {
            var count = 0;
            foreach (var entityId in items)
                if (entityId != 0)
                    count++;

            pw.WriteList(count);

            for (var slot = 0; slot < items.Count; slot++)
            {
                if (items[slot] == 0)
                    continue;

                if (IsSlotless(type))
                {
                    pw.WriteULong(items[slot]);
                    continue;
                }

                pw.WriteTuple(2);
                pw.WriteULong(items[slot]);
                pw.WriteInt(slot);
            }
        }
    }
}
