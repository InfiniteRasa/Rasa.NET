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

                        // A weapon that takes no ammunition has no ammo class, and the tooltip leaves
                        // its ammo line out for None alone (_AddWeaponAmmo): 0 is looked up as a class.
                        if (EntityClass.WeaponClassInfo.AmmoClassId == 0)
                            pw.WriteNoneStruct();
                        else
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
                                pw.WriteTuple(0);                                   // no alt fire: see below

                            pw.WriteInt((int)ItemTemplate.WeaponInfo.AttackType);
                            pw.WriteInt((int)ItemTemplate.WeaponInfo.ToolType);
                        }
                        else
                        {
                            // The entity class is augmented as a weapon, but this item template has no
                            // matching row from ItemManager's weapon-items load (data gap). Send zeroed
                            // weapon stats instead of crashing the write so the tooltip still opens.
                            //
                            // In the shapes the client's tooltip reads them (tooltipwindow.py,
                            // gameuiutil.py). The range is None, which leaves the range line out: it is
                            // not known, and 0 reads "Self Only". The alt fire is an empty tuple:
                            // GetWeaponAltFireInfo takes its len(), and len(None) is a TypeError that
                            // left the Snowball Launcher (131482) with no tooltip at all.
                            if (ReportedWithoutWeaponInfo.TryAdd(ItemTemplate.ItemTemplateId, true))
                                Logger.WriteLog(LogType.Debug, $"ItemTemplateTooltipInfoPacket: item template {ItemTemplate.ItemTemplateId} is augmented as a weapon but has no WeaponInfo; sending zeroed weapon stats");
                            pw.WriteUInt(0);
                            pw.WriteInt(EntityClass.WeaponClassInfo.DamageType);
                            pw.WriteUInt(0);                                        // windup
                            pw.WriteUInt(0);                                        // recovery
                            pw.WriteUInt(0);                                        // refire
                            pw.WriteUInt(0);                                        // reload
                            pw.WriteNoneStruct();                                   // range
                            pw.WriteUInt(0);                                        // AE radius
                            pw.WriteNoneStruct();                                   // AE type
                            pw.WriteTuple(0);                                       // alt fire
                            pw.WriteInt(0);                                         // attack type
                            pw.WriteInt(0);                                         // tool type
                        }

                        break;

                    case AugmentationType.Equipable:
                        pw.WriteInt((int)AugmentationType.Equipable);
                        pw.WriteTuple(2);

                        // The client unpacks the skill requirement as a pair and walks the resistances
                        // as a list wherever it shows an equipable - the inventory icon
                        // (_LoadElementalIconForItemTemplate, _GetClassPowerLevel) and the tooltip
                        // (_AddSkillSlot, _AddResists, _AddEquipableRequirement) - and tests only the
                        // skill id for None. A template with no skill requirement row (3,115 of the
                        // 22,678 equipable ones, the Space Helmet and the Snowball Launcher among them)
                        // was sent None for both: a TypeError on every icon redraw, and no tooltip.
                        pw.WriteTuple(2);                                                   // skillRequirement data
                        if (ItemTemplate.EquipableInfo != null)
                        {
                            pw.WriteInt(ItemTemplate.EquipableInfo.SkillId);
                            pw.WriteInt(ItemTemplate.EquipableInfo.SkillLevel);
                        }
                        else
                        {
                            pw.WriteNoneStruct();
                            pw.WriteNoneStruct();
                        }

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
                            pw.WriteList(0);

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
                        // kItemIdx_ModuleIds = 4. None yet (ToDo), as the empty list the client reads
                        // it as: HandleReceiveItemInfo hands it to a loop, "for moduleId in moduleIds",
                        // when the tooltip of an item it has no entity for was waiting on this reply.
                        pw.WriteList(0);
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
