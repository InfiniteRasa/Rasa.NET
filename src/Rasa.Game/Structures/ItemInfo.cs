using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using World;

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
        public uint ReloadTime { get; set; }
        public uint AeType { get; set; }
        public uint AltActionId { get; set; }
        public uint AltActionArgId { get; set; }
        public uint AeRadius { get; set; }
        public uint RecoilAmount { get; set; }
        public uint ReuseOverride { get; set; }
        public uint CoolRate { get; set; }
        public double HeatPerShot { get; set; }
        public ToolType ToolType { get; set; }
        public uint AmmoPerShot { get; set; }
        public uint Windup { get; set; }
        public uint Recovery { get; set; }
        public uint Refire { get; set; }
        public uint Range { get; set; }
        public AttackType AttackType { get; set; }
        public WeaponAltInfo WeaponAltInfo { get; set; }

        public WeaponInfo(ItemTemplateWeaponEntry weapon)
        {
            AimRate = weapon.AimRate;
            ReloadTime = weapon.ReloadTime;
            AeType = weapon.AeType;
            AltActionId = weapon.AltActionId;
            AltActionArgId = weapon.AltActionArgId;
            AeRadius = weapon.AeRadius;
            RecoilAmount = weapon.RecoilAmount;
            ReuseOverride = weapon.ReuseOverride;
            CoolRate = weapon.CoolRate;
            HeatPerShot = weapon.HeatPerShot;
            ToolType = (ToolType)weapon.ToolType;
            AmmoPerShot = weapon.AmmoPerShot;
            Windup = weapon.Windup;
            Recovery = weapon.Recovery;
            Refire = weapon.Refire;
            Range = weapon.Range;
            AttackType = (AttackType)weapon.AttackType;

            if (AltActionId > 0)
                WeaponAltInfo = new WeaponAltInfo(weapon);
        }
    }

    public class WeaponAltInfo
    {
        public uint AltMaxDamage { get; set; }      // kWeaponAltIdx_MaxDamage = 0
        public uint AltDamageType { get; set; }     // kWeaponAltIdx_DamageType = 1
        public uint AltRange { get; set; }          // kWeaponAltIdx_Range = 2
        public uint AltAeRadius { get; set; }       // kWeaponAltIdx_AERadius = 3
        public uint AltAeType { get; set; }         // kWeaponAltIdx_AEType = 4

        public WeaponAltInfo(ItemTemplateWeaponEntry weapon)
        {
            AltMaxDamage = weapon.AltMaxDamage;
            AltDamageType = weapon.AltDamageType;
            AltRange = weapon.AltRange;
            AltAeRadius = weapon.AltAeRadius;
            AltAeType = weapon.AltAeType;
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
