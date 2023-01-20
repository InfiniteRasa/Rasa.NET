namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ItemTemplateTooltipInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemTemplateTooltipInfo;

        private ItemTemplate ItemTemplate { get; set; }
        private EntityClass EntityClass { get; set; }

        public ItemTemplateTooltipInfoPacket(ItemTemplate itemTemplate, EntityClass entityClass)
        {
            ItemTemplate = itemTemplate;
            EntityClass = entityClass;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ItemTemplate.ItemTemplateId);
            pw.WriteUInt((uint)ItemTemplate.Class);
            pw.WriteDictionary(EntityClass.Augmentations.Count);
            foreach (var augumentation in EntityClass.Augmentations)
            {
                switch (augumentation)
                {
                    case AugmentationType.Weapon:
                        pw.WriteInt((int)AugmentationType.Weapon);
                        pw.WriteTuple(16);
                        pw.WriteInt(EntityClass.WeaponClassInfo.MinDamage);
                        pw.WriteInt(EntityClass.WeaponClassInfo.MaxDamage);
                        pw.WriteUInt((uint)EntityClass.WeaponClassInfo.AmmoClassId);
                        pw.WriteUInt(EntityClass.WeaponClassInfo.ClipSize);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.AmmoPerShot);
                        pw.WriteInt(EntityClass.WeaponClassInfo.DamageType);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.Windup);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.Recovery);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.Refire);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.ReloadTime);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.Range);
                        pw.WriteUInt(ItemTemplate.WeaponInfo.AeRadius);

                        if (ItemTemplate.WeaponInfo.AeType == 0)
                            pw.WriteNoneStruct();
                        else
                            pw.WriteUInt(ItemTemplate.WeaponInfo.AeType);

                        if (ItemTemplate.WeaponInfo.WeaponAltInfo != null)
                        {
                            pw.WriteTuple(5);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.WeaponAltInfo.AltMaxDamage);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.WeaponAltInfo.AltDamageType);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.WeaponAltInfo.AltRange);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.WeaponAltInfo.AltAeRadius);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.WeaponAltInfo.AltAeType);
                        }
                        else
                            pw.WriteNoneStruct();

                        pw.WriteInt((int)ItemTemplate.WeaponInfo.AttackType);
                        pw.WriteInt((int)ItemTemplate.WeaponInfo.ToolType);

                        break;

                    case AugmentationType.Equipable:
                        pw.WriteInt((int)AugmentationType.Equipable);
                        pw.WriteTuple(2);
                        if (ItemTemplate.EquipableInfo != null)                             // skillRequirement data
                        {
                            pw.WriteTuple(2);
                            pw.WriteInt(ItemTemplate.EquipableInfo.SkillId);
                            pw.WriteInt(ItemTemplate.EquipableInfo.SkillLevel);
                        }
                        else
                            pw.WriteNoneStruct();

                        if (ItemTemplate.EquipableInfo != null)                             // resistance data
                        {
                            pw.WriteList(ItemTemplate.EquipableInfo.ResistList.Count);
                            foreach (var resistance in ItemTemplate.EquipableInfo.ResistList)
                            {
                                pw.WriteTuple(2);
                                pw.WriteInt((int)resistance.ResistanceType);
                                pw.WriteInt(resistance.ResistanceAmmount);
                            }
                        }
                        else
                            pw.WriteNoneStruct();

                        break;

                    case AugmentationType.Item:
                        pw.WriteInt((int)AugmentationType.Item);
                        pw.WriteTuple(6);
                        pw.WriteBool(!ItemTemplate.ItemInfo.Tradable);
                        pw.WriteInt(EntityClass.ItemClassInfo.MaxHitPoints);
                        pw.WriteInt(ItemTemplate.ItemInfo.BuyBackPrice);
                        pw.WriteList(ItemTemplate.ItemInfo.Requirements.Count);
                        foreach (var requirement in ItemTemplate.ItemInfo.Requirements)
                        {
                            pw.WriteTuple(2);
                            pw.WriteInt((int)requirement.Key);
                            pw.WriteInt(requirement.Value);
                        }
                        pw.WriteNoneStruct();                                       // kItemIdx_ModuleIds		= 4      ToDo
                        if (ItemTemplate.ItemInfo.RaceReq != 0)
                        {
                            pw.WriteList(1);
                            pw.WriteInt(ItemTemplate.ItemInfo.RaceReq);
                        }
                        else
                            pw.WriteNoneStruct();
                        break;

                    case AugmentationType.Armor:
                        pw.WriteInt((int)AugmentationType.Armor);
                        pw.WriteTuple(1);
                        pw.WriteInt(EntityClass.ArmorClassInfo.RegenRate);
                        break;

                    case AugmentationType.Customization:
                        pw.WriteInt((int)AugmentationType.Customization);
                        pw.WriteNoneStruct();
                        break;

                    default:
                        Logger.WriteLog(LogType.Error, $"ItemTemplateTooltipInfoPacket:\n recived unsuported augumentationType {augumentation}");
                        break;
                }
            }
        }

    }
}
