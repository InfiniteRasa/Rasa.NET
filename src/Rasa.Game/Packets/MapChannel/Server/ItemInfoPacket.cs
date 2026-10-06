using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ItemInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemInfo;

        public Item Item { get; set; }
        public EntityClass ClassInfo { get; set; }

        public ItemInfoPacket(Item item, EntityClass classInfo)
        {
            Item = item;
            ClassInfo = classInfo;
        }
                
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(15);
            pw.WriteInt(Item.CurrentHitPoints);      // 'currentHitPoints' --> Displayed as "Armor: x" in case of armor
            pw.WriteInt(ClassInfo.ItemClassInfo.MaxHitPoints);
            if (Item.Crafter != "" && Item.Crafter != null)
                pw.WriteString(Item.Crafter);
            else
                pw.WriteNoneStruct();
            pw.WriteUInt(Item.ItemTemplate.ItemTemplateId);
            pw.WriteBool(Item.ItemTemplate.HasSellableFlag);
            pw.WriteBool(Item.ItemTemplate.HasCharacterUniqueFlag);
            pw.WriteBool(Item.ItemTemplate.HasAccountUniqueFlag);
            pw.WriteBool(Item.ItemTemplate.HasBoEFlag);
            // 'classModuleIds': the modules an item has of itself, which the client adds to its
            // tooltip and leaves out of its name and of the crafting station. No item template
            // is known to have any - that was the server's to know - so none is sent.
            pw.WriteList(0);
            WriteLootModuleIds(pw, Item.ModuleIds);     // 'lootModuleIds'
            pw.WriteInt(Item.ItemTemplate.QualityId);
            pw.WriteBool(Item.IsBound);         // 'boundToCharacter': this item, not its template
            pw.WriteBool(Item.ItemTemplate.ItemInfo.Tradable);
            pw.WriteBool(Item.ItemTemplate.NotPlaceableInLockbox);
            pw.WriteInt((int)Item.ItemTemplate.InventoryCategory);
        }

        /// <summary>
        /// An item's module slots (Item.ModuleIds), as the client holds them: a list by slot,
        /// with None for an empty one, that may stop short of four. craftingnew.
        /// ItemHasEmptyModuleSlot takes either for empty - "len(moduleIds) &lt; 4", then "moduleId
        /// is None" - and the crafting station's extraction and integration pages take a
        /// module's place in the list for its slot. So the slots are sent up to the last full
        /// one, and an item with no module is sent the empty list it always was.
        ///
        /// The client names the item by these (gameuiutil.GetItemName): by the module of the
        /// highest moduleclasspriority, and only if the first slot is not empty.
        /// </summary>
        internal static void WriteLootModuleIds(PythonWriter pw, IReadOnlyList<uint> moduleIds)
        {
            var count = moduleIds.Count;

            while (count > 0 && moduleIds[count - 1] == 0)
                count--;

            pw.WriteList(count);

            for (var slot = 0; slot < count; slot++)
            {
                if (moduleIds[slot] != 0)
                    pw.WriteUInt(moduleIds[slot]);
                else
                    pw.WriteNoneStruct();
            }
        }
    }
}
