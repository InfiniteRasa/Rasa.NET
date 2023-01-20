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
            pw.WriteList(0);                    // 'classModuleIds'         // ToDo
            pw.WriteList(0);                    // 'lootModuleIds'          // ToDo
            pw.WriteInt(Item.ItemTemplate.QualityId);
            pw.WriteBool(Item.ItemTemplate.BoundToCharacter);
            pw.WriteBool(Item.ItemTemplate.ItemInfo.Tradable);
            pw.WriteBool(Item.ItemTemplate.NotPlaceableInLockbox);
            pw.WriteInt((int)Item.ItemTemplate.InventoryCategory);
        }
    }
}
