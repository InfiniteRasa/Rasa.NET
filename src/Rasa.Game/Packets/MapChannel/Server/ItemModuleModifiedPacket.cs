using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_ItemModuleModified(moduleIds) on an item (client/augmentations/item.py): the item's
    /// loot modules are now these. The list is ItemInfo's lootModuleIds - by slot, None for an
    /// empty one - and takes its place whole; the modules an item has of itself
    /// (classModuleIds) are not touched.
    ///
    /// Wired so that it can be sent, and sent by nothing. All the client does with it is store
    /// the list. That is enough for the name, the tooltip and the crafting window, which read
    /// the list each time they draw, but it raises no event: the client does not ask for a new
    /// module's tooltip until the item is next pointed at, and the name under "Modified By"
    /// cannot change with it. The item's ItemInfo sent again does all of that, so that is what
    /// follows a change of modules here (ItemModules).
    ///
    /// Its id, 717, is among the first crafting window's - RequestDisassembleItem 676,
    /// RequestModifyItem 677, the three results 698 to 700 - and well before the 1.16.5 pages'
    /// (844 to 849). There a recipe upgraded a module of an item, or gave it one, where it lay
    /// in the pack, and this is very likely what told the client so. The C++ server only listed
    /// the id.
    /// </summary>
    public class ItemModuleModifiedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemModuleModified;

        /// <summary>The item's four slots, 0 for an empty one (Item.ModuleIds).</summary>
        public IReadOnlyList<uint> ModuleIds { get; }

        public ItemModuleModifiedPacket(IReadOnlyList<uint> moduleIds)
        {
            ModuleIds = moduleIds;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            ItemInfoPacket.WriteLootModuleIds(pw, ModuleIds);
        }
    }
}
