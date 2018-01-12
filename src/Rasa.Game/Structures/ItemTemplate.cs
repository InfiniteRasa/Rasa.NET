namespace Rasa.Structures
{
    public class ItemTemplate
    {
        public ItemTemplate(ItemTemplateEntry template)
        {
            ItemTemplateId = template.ItemTemplateId;
            ClassId = template.ClassId;
            QualityId = template.QualityId;
            ItemType = template.ItemType;
            HasSellableFlag = template.HasSellableFlag;
            NotTradable = template.NotTradeableFlag;
            HasCharacterUniqueFlag = template.HasCharacterUniqueFlag;
            HasAccountUniqueFlag = template.HasAccountUniqueFlag;
            HasBoEFlag = template.HasBoEFlag;
            BoundToCharacter = template.BoundToCharacterFlag;
            NotPlaceableInLockbox = template.NotPlaceableInLockBoxFlag;
            InventoryCategory = template.InventoryCategory;
            ReqLevel = template.ReqLevel;
            BuyPrice = template.BuyPrice;
            SellPrice = template.SellPrice;
            Stacksize = template.StackSize;
        }

        // item augmentation and general item info
        public int ClassId { get; set; }
        public int ItemTemplateId { get; set; }
        public int ItemType { get; set; }
        // general stuff
        public int CurrentHitPoints { get; set; }
        public int MaxHitPoints { get; set; }
        // other
        public string CrafterName { get; set; }        
        public bool HasSellableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public int[] ClassModuleIds { get; set; }
        public int[] LootModuleIds { get; set; }
        public bool HasBoEFlag { get; set; }
        public int QualityId { get; set; }
        public bool BoundToCharacter { get; set; }
        public bool NotTradable { get; set; }
        public bool NotPlaceableInLockbox { get; set; }
        public int InventoryCategory { get; set; }
        //
        public int ReqLevel { get; set; }   // required level
        public int ReqLevelMax { get; set; } // Max level that can use item
        public int ReqBody { get; set; }    // required body
        public int ReqMind { get; set; }    // required mind
        public int ReqSpirit { get; set; }  // required spirit
        public int ReqRace { get; set; }    // required race
        public int BuyPrice { get; set; }   // price in credits to buy the item from a vendor
        public int SellPrice { get; set; }  // price in credits to sell the item to a vendor
        public int Stacksize { get; set; }  // max stacksize
        // armor specific        
        public ArmorTupple Armor { get; set; }
        // equipment data
        public EquipmentTupple Equipment { get; set; }
        // weapon specific
        public WeaponTupple Weapon { get; set; }
    }

    public class ArmorTupple
    {
        public int ArmorValue { get; set; } // body armor
        public int DamageAbsorbed { get; set; } // damage absorbed
        public int RegenRate { get; set; } // regen rate        
    }

    public class EquipmentTupple
    {
        public int EquiptmentSlotType { get; set; }
        public int RequiredSkillId { get; set; } // <=0 if not used 
        public int RequiredSkillMinVal { get; set; } // level of skill required, <= 0 if not used
    }

    public class WeaponTupple
    {
        // constant data from DB
        public int ItemTemplateId { get; set; }
        public int ClipSize { get; set; }
        public double AimRate { get; set; }
        public int ReloadTime { get; set; }
        public int AltActionId { get; set; }
        public int AltActionArg { get; set; }
        public int AeType { get; set; }
        public int AeRadius { get; set; }
        public int RecoilAmount { get; set; }
        public int ReuseOverride { get; set; }
        public int CoolRate { get; set; }
        public double HeatPerShot { get; set; }
        public int ToolType { get; set; }
        public int AmmoPerShot { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int AmmoClassId { get; set; }
        public int DamageType { get; set; }
        public int WindupTime { get; set; }
        public int RecoveryTime { get; set; }
        public int RefireTime { get; set; }
        public int Range { get; set; }
        public int AltMaxDamage { get; set; }
        public int AltDamageType { get; set; }
        public int AltRange { get; set; }
        public int AltAERadius { get; set; }
        public int AltAEType { get; set; }
        public int AttackType { get; set; }
        // used by Recv_WeaponInfo
        public string WeaponName { get; set; }
        public bool IsJammed { get; set; }
        public int CammeraProfile { get; set; }

        public WeaponTupple(WeaponTemplateEntry weapon)
        {
            ClipSize = weapon.ClipSize;
            AimRate = weapon.AimRate;
            ReloadTime = weapon.ReloadTime;
            AltActionId = weapon.AltActionId;
            AltActionArg = weapon.AltActionArg;
            AeType = weapon.AeType;
            AeRadius = weapon.AeRadius;
            RecoilAmount = weapon.RecoilAmount;
            ReuseOverride = weapon.ReuseOverride;
            CoolRate = weapon.CoolRate;
            HeatPerShot = weapon.HeatPerShot;
            ToolType = weapon.ToolType;
            AmmoPerShot = weapon.AmmoPerShot;
            MinDamage = weapon.MinDamage;
            MaxDamage = weapon.MaxDamage;
            AmmoClassId = weapon.AmmoClassId;
            DamageType = weapon.DamageType;
            WindupTime = weapon.WindupTime;
            RecoveryTime = weapon.RecoveryTime;
            RefireTime = weapon.RefireTime;
            Range = weapon.Range;
            AltMaxDamage = weapon.AltMaxDamage;
            AltDamageType = weapon.AltDamageType;
            AltRange = weapon.AltRange;
            AltAERadius = weapon.AltAERadius;
            AltAEType = weapon.AltAEType;
            AttackType = weapon.AttackType;
        }
    }
}
