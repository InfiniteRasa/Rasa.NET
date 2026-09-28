namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ItemTemplateTooltipInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemTemplateTooltipInfo;

        // Templates already reported as weapons without an itemtemplate_weapon row: once each,
        // not on every tooltip request. Most are tools (ToolActionManager: 519 of the 719 tool
        // templates have none), which work without one, so this is a note and not an error.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<uint, bool> ReportedWithoutWeaponInfo = new();

        private ItemTemplate ItemTemplate { get; set; }
        private EntityClass EntityClass { get; set; }

        public ItemTemplateTooltipInfoPacket(ItemTemplate itemTemplate, EntityClass entityClass)
        {
            ItemTemplate = itemTemplate;
            EntityClass = entityClass;
        }

        /// <summary>
        /// The augmentations this packet has an entry for. The dictionary's length goes out
        /// before its entries, so it has to count only what is written: a Recipe item (16) - the
        /// dye recipes, every crafting recipe - announced two entries and wrote one, and the
        /// client refused the whole reply ("Unable to unpack args for methodName=
        /// ItemTemplateTooltipInfo"), so the item had no tooltip. The client reads a recipe's
        /// tooltip from its own recipe data, not from here.
        /// </summary>
        private static bool Written(AugmentationType augmentation) => augmentation switch
        {
            AugmentationType.Weapon => true,
            AugmentationType.Equipable => true,
            AugmentationType.Item => true,
            AugmentationType.Armor => true,
            AugmentationType.Customization => true,
            _ => false
        };

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ItemTemplate.ItemTemplateId);
            pw.WriteUInt((uint)ItemTemplate.Class);

            var written = 0;
            foreach (var augmentation in EntityClass.Augmentations)
                if (Written(augmentation))
                    written++;

            pw.WriteDictionary(written);
            foreach (var augumentation in EntityClass.Augmentations)
            {
                if (!Written(augumentation))
                    continue;

                switch (augumentation)
                {
                    case AugmentationType.Weapon:
                        pw.WriteInt((int)AugmentationType.Weapon);
                        pw.WriteTuple(16);
                        pw.WriteInt(EntityClass.WeaponClassInfo.MinDamage);
                        pw.WriteInt(EntityClass.WeaponClassInfo.MaxDamage);
                        pw.WriteUInt((uint)EntityClass.WeaponClassInfo.AmmoClassId);
                        pw.WriteUInt(EntityClass.WeaponClassInfo.ClipSize);

                        if (ItemTemplate.WeaponInfo != null)
                        {
                            pw.WriteUInt(ItemTemplate.WeaponInfo.AmmoPerShot);
                            pw.WriteInt(EntityClass.WeaponClassInfo.DamageType);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.Windup);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.Recovery);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.Refire);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.ReloadTime);
                            pw.WriteUInt(ItemTemplate.WeaponInfo.Range);
                            // Shotguns and propellant guns as cones, as WeaponInfo sends them (Managers.ConeWeapons).
                            var (aeType, aeRadius) = Managers.ConeWeapons.AeOf(ItemTemplate.WeaponInfo, EntityClass.WeaponClassInfo);

                            pw.WriteUInt(aeRadius);

                            if (aeType == 0)
                                pw.WriteNoneStruct();
                            else
                                pw.WriteUInt(aeType);

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
                        }
                        else
                        {
                            // The entity class is augmented as a weapon, but this item template has no
                            // matching row from ItemManager's weapon-items load (data gap). Send zeroed
                            // weapon stats instead of crashing the write so the tooltip still opens.
                            if (ReportedWithoutWeaponInfo.TryAdd(ItemTemplate.ItemTemplateId, true))
                                Logger.WriteLog(LogType.Debug, $"ItemTemplateTooltipInfoPacket: item template {ItemTemplate.ItemTemplateId} is augmented as a weapon but has no WeaponInfo; sending zeroed weapon stats");
                            pw.WriteUInt(0);
                            pw.WriteInt(EntityClass.WeaponClassInfo.DamageType);
                            pw.WriteUInt(0);
                            pw.WriteUInt(0);
                            pw.WriteUInt(0);
                            pw.WriteUInt(0);
                            pw.WriteUInt(0);
                            pw.WriteUInt(0);
                            pw.WriteNoneStruct();
                            pw.WriteNoneStruct();
                            pw.WriteInt(0);
                            pw.WriteInt(0);
                        }

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
                        // Unreachable: Written() leaves every other augmentation out.
                        break;
                }
            }
        }

    }
}
