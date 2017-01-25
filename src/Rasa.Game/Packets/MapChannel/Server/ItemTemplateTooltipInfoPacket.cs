namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ItemTemplateTooltipInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemTemplateTooltipInfo;
        
        public ItemTemplate ItemTemplate { get; set; }

        public override void Read(PythonReader pr)
        {            
        }

        // ToDo tooltip dont show all data, need more work on this
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)ItemTemplate.ItemTemplateId);  // itemTemplateId
            pw.WriteInt(ItemTemplate.ClassId);              // itemClas

            if (ItemTemplate.ItemType == (int)ItemTypes.Armor)
            {
                pw.WriteDictionary(1);
                pw.WriteInt((int)Augmentation.Item);        // Dictionary 1
                pw.WriteTuple(6);
                pw.WriteBool(!ItemTemplate.NotTradable);    // kItemIdx_Tradable		= 0
                pw.WriteInt(ItemTemplate.MaxHitPoints);     // kItemIdx_MaxHPs			= 1
                pw.WriteInt(ItemTemplate.SellPrice);        // kItemIdx_BuybackPrice	= 2
                if (ItemTemplate.ReqLevel > 0)
                {
                    pw.WriteList(1);                            // kItemIdx_Requirements	= 3
                    pw.WriteTuple(2);
                    pw.WriteInt(1);
                    pw.WriteInt(ItemTemplate.ReqLevel); // Require level
                }
                else
                    pw.WriteNoneStruct();               // no requirements
                pw.WriteNoneStruct();                       // kItemIdx_ModuleIds		= 4
                pw.WriteNoneStruct();                      // kItemIdx_RaceIds			= 5

                /* armor specific augmentation data
                pw.WriteInt((int)Augmentation.Armor);
                pw.WriteTuple(1);
                pw.WriteInt(ItemTemplate.Armor.RegenRate); // regen rate
                */

                /* Equipable data
                pw.WriteInt((int)Agumentation.Equipable);
                pw.WriteTuple(2);
                pw.WriteTuple(2);   // kEquipableIdx_SkillInfo = 0 (SkillId, MinSkillLvl)
                if (ItemTemplate.Equipment.RequiredSkillId <= 0 || ItemTemplate.Equipment.RequiredSkillMinVal <= 0)
                {
                    pw.WriteNoneStruct(); // Skill ID
                    pw.WriteNoneStruct(); // Skill Level
                }
                else
                {
                    pw.WriteInt(ItemTemplate.Equipment.RequiredSkillId);
                    pw.WriteInt(ItemTemplate.Equipment.RequiredSkillMinVal);
                }
                pw.WriteTuple(1);// kEquipableIdx_ResistList = 1 (damageType, resistValue)
                pw.WriteNoneStruct();   // ToDo*/
            }

            else if (ItemTemplate.ItemType == (int)ItemTypes.Weapon)
            {
                pw.WriteDictionary(2);
                pw.WriteInt((int)Augmentation.Item);        // Dictionary 1
                pw.WriteTuple(6);
                pw.WriteBool(!ItemTemplate.NotTradable);    // kItemIdx_Tradable		= 0
                pw.WriteInt(ItemTemplate.MaxHitPoints);     // kItemIdx_MaxHPs			= 1
                pw.WriteInt(ItemTemplate.SellPrice);        // kItemIdx_BuybackPrice	= 2
                if (ItemTemplate.ReqLevel > 0)
                {
                    pw.WriteList(1);                        // kItemIdx_Requirements	= 3
                    pw.WriteTuple(2);
                    pw.WriteInt(1);
                    pw.WriteInt(ItemTemplate.ReqLevel); // Require level
                }
                else
                    pw.WriteNoneStruct();               // no requirements         
                pw.WriteNoneStruct();                       // kItemIdx_ModuleIds		= 4
                pw.WriteNoneStruct();                       // kItemIdx_RaceIds			= 5

                //weapon specific augmentation data
                pw.WriteInt((int)Augmentation.Weapon);
                pw.WriteTuple(16);
                pw.WriteInt(ItemTemplate.Weapon.MinDamage);           //kWeaponIdx_MinDamage    = 0
                pw.WriteInt(ItemTemplate.Weapon.MaxDamage);           //kWeaponIdx_MaxDamage    = 1
                pw.WriteInt(ItemTemplate.Weapon.AmmoClassId);         //kWeaponIdx_AmmoClassId  = 2
                pw.WriteInt(ItemTemplate.Weapon.ClipSize);            //kWeaponIdx_ClipSize     = 3
                pw.WriteInt(ItemTemplate.Weapon.AmmoPerShot);         //kWeaponIdx_AmmoPerShot  = 4
                pw.WriteInt(ItemTemplate.Weapon.DamageType);          //kWeaponIdx_DamageType   = 5
                pw.WriteInt(ItemTemplate.Weapon.WindupTime);          //kWeaponIdx_WindupTime   = 6
                pw.WriteInt(ItemTemplate.Weapon.RecoveryTime);        //kWeaponIdx_RecoveryTime = 7
                pw.WriteInt(ItemTemplate.Weapon.RefireTime);          //kWeaponIdx_RefireTime   = 8
                pw.WriteInt(ItemTemplate.Weapon.ReloadTime);          //kWeaponIdx_ReloadTime   = 9
                pw.WriteInt(ItemTemplate.Weapon.Range);               //kWeaponIdx_Range        = 10
                pw.WriteInt(ItemTemplate.Weapon.AeRadius);            //kWeaponIdx_AERadius     = 11

                if (ItemTemplate.Weapon.AeType == 0)
                    pw.WriteNoneStruct();
                else
                    pw.WriteInt(ItemTemplate.Weapon.AeType);        //kWeaponIdx_AEType         = 12
                                                                    //kWeaponIdx_AltFire		= 13
                if (ItemTemplate.Weapon.AltActionId != 0)
                {
                    pw.WriteTuple(5);
                    pw.WriteInt(ItemTemplate.Weapon.AltMaxDamage);  // kWeaponAltIdx_MaxDamage	= 0
                    pw.WriteInt(ItemTemplate.Weapon.AltDamageType); // kWeaponAltIdx_DamageType = 1
                    pw.WriteInt(ItemTemplate.Weapon.AltRange);      // kWeaponAltIdx_Range		= 2
                    pw.WriteInt(ItemTemplate.Weapon.AltAERadius);   // kWeaponAltIdx_AERadius	= 3
                    pw.WriteInt(ItemTemplate.Weapon.AltAEType);     // kWeaponAltIdx_AEType		= 4
                }
                else
                {
                    pw.WriteTuple(1);
                    pw.WriteNoneStruct();
                }

                pw.WriteInt(ItemTemplate.Weapon.AttackType);        //kWeaponIdx_AttackType		= 14 // ranged
                pw.WriteInt(ItemTemplate.Weapon.ToolType);          //kWeaponIdx_ToolType		= 15 // rifle
            }
            // else other items
            else
            {
                pw.WriteDictionary(1);
                pw.WriteInt((int)Augmentation.Item);        // Dictionary 1
                pw.WriteTuple(6);
                pw.WriteBool(!ItemTemplate.NotTradable);    // kItemIdx_Tradable		= 0
                pw.WriteInt(ItemTemplate.MaxHitPoints);     // kItemIdx_MaxHPs			= 1
                pw.WriteInt(ItemTemplate.SellPrice);        // kItemIdx_BuybackPrice	= 2
                if (ItemTemplate.ReqLevel > 0)
                {
                    pw.WriteList(1);                            // kItemIdx_Requirements	= 3
                    pw.WriteTuple(2);
                    pw.WriteInt(1);
                    pw.WriteInt(ItemTemplate.ReqLevel); // Require level
                }
                else
                    pw.WriteNoneStruct();               // no requirements
                pw.WriteNoneStruct();                       // kItemIdx_ModuleIds		= 4
                pw.WriteNoneStruct();                      // kItemIdx_RaceIds			= 5
            }
        }
    }
}
