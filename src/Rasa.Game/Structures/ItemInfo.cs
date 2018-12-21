using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class ItemInfo
    {
        public bool Tradable { get; set; }
        public int BuyBackPrice { get; set; }
        // there is only reqXPLevel data in phyton files, but (reqBody, reqMind, reqSpirit, reqXpLevel) can be added too
        public Dictionary<RequirementsType, int> Requirements = new Dictionary<RequirementsType, int>();
        public List<int> ModuleIds { get; set; }
        // there is only 1 race requirement per itemClass in phyton files, but list can be added too
        public int RaceReq { get; set; }

        public ItemInfo(bool tradable, int buyBackPrice, List<int> moduleIds, int raceReq)
        {
            Tradable = tradable;
            BuyBackPrice = buyBackPrice;
            ModuleIds = moduleIds;
            RaceReq = raceReq;
        }

        public ItemInfo()
        {
        }
    }

    public class EquipableInfo
    {
        public int SkillId { get; set; }
        public int SkillLevel { get; set; }
        public List<ResistanceData> ResistList = new List<ResistanceData>();

        public EquipableInfo(int skillId, int skillLevel)
        {
            SkillId = skillId;
            SkillLevel = skillLevel;
        }
    }

    public class WeaponInfo
    {
        public string WeaponName { get; set; }
        public double AimRate { get; set; }
        public int ReloadTime { get; set; }
        public int AEType { get; set; }
        public int AltActionId { get; set; }
        public int AltActionArg { get; set; }
        public int AERadius { get; set; }
        public int RecoilAmount { get; set; }
        public int ReuseOverride { get; set; }
        public int CoolRate { get; set; }
        public double HeatPerShot { get; set; }
        public int ToolType { get; set; }
        public uint AmmoPerShot { get; set; }
        public int WindupTime { get; set; }
        public int RecoveryTime { get; set; }
        public int RefireTime { get; set; }
        public int Range { get; set; }
        public int AttackType { get; set; }
        public WeaponAltInfo WeaponAltInfo { get; set; }

        public WeaponInfo(WeaponTemplateEntry weapon)
        {
            AimRate = weapon.AimRate;
            ReloadTime = weapon.ReloadTime;
            AEType = weapon.AEType;
            AltActionId = weapon.AltActionId;
            AltActionArg = weapon.AltActionArg;
            AERadius = weapon.AERadius;
            RecoilAmount = weapon.RecoilAmount;
            ReuseOverride = weapon.ReuseOverride;
            CoolRate = weapon.CoolRate;
            HeatPerShot = weapon.HeatPerShot;
            ToolType = weapon.ToolType;
            AmmoPerShot = weapon.AmmoPerShot;
            WindupTime = weapon.WindupTime;
            RecoveryTime = weapon.RecoveryTime;
            RefireTime = weapon.RefireTime;
            Range = weapon.Range;
            AttackType = weapon.AttackType;

            if (AltActionId > 0)
                WeaponAltInfo = new WeaponAltInfo(weapon);
        }
    }

    public class WeaponAltInfo
    {
        public int MaxDamage { get; set; }      // kWeaponAltIdx_MaxDamage = 0
        public int DamageType { get; set; }     // kWeaponAltIdx_DamageType = 1
        public int Range { get; set; }          // kWeaponAltIdx_Range = 2
        public int AERadius { get; set; }       // kWeaponAltIdx_AERadius = 3
        public int AEType { get; set; }         // kWeaponAltIdx_AEType = 4

        public WeaponAltInfo(WeaponTemplateEntry weapon)
        {
            MaxDamage = weapon.AltMaxDamage;
            DamageType = weapon.AltDamageType;
            Range = weapon.AltRange;
            AERadius = weapon.AltAERadius;
            AEType = weapon.AltAEType;
        }
    }

    /*public class ItemRequirements
    {
        public RequirementsType Type { get; set; }
        public int Value { get; set; }

        public ItemRequirements(RequirementsType type, int value)
        {
            Type = type;
            Value = value;
        }
    }*/
}
