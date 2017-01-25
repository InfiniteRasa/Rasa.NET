using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class WeaponTemplateEntry
    {
        public int ItemTemplateId { get; set; }        
        public int ClipSize { get; set; }
        public int CurrentAmmo { get; set; }    // this sould be elsewhere i gues :)
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

        public static WeaponTemplateEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new WeaponTemplateEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateId"),
                ClipSize = reader.GetInt32("clipSize"),
                CurrentAmmo = reader.GetInt32("currentAmmo"),
                AimRate = reader.GetDouble("aimRate"),
                ReloadTime = reader.GetInt32("reloadTime"),
                AltActionId = reader.GetInt32("altActionId"),
                AltActionArg = reader.GetInt32("altActionArg"),
                AeType = reader.GetInt32("aeType"),
                AeRadius = reader.GetInt32("aeRadius"),
                RecoilAmount = reader.GetInt32("recoilAmount"),
                ReuseOverride = reader.GetInt32("ReuseOverride"),
                CoolRate = reader.GetInt32("coolRate"),
                HeatPerShot = reader.GetDouble("heatPerShot"),
                ToolType = reader.GetInt32("toolType"),
                AmmoPerShot = reader.GetInt32("ammoPerShot"),
                MinDamage = reader.GetInt32("minDamage"),
                MaxDamage = reader.GetInt32("maxDamage"),
                AmmoClassId = reader.GetInt32("ammoClassId"),
                DamageType = reader.GetInt32("damageType"),
                WindupTime = reader.GetInt32("windupTime"),
                RecoveryTime = reader.GetInt32("recoveryTime"),
                RefireTime = reader.GetInt32("refireTime"),
                Range = reader.GetInt32("range"),
                AltMaxDamage = reader.GetInt32("altMaxDamage"),
                AltDamageType = reader.GetInt32("altDamageType"),
                AltRange = reader.GetInt32("altRange"),
                AltAERadius = reader.GetInt32("altAERadius"),
                AltAEType = reader.GetInt32("altAEType"),
                AttackType = reader.GetInt32("attackType"),
            };
        }
    }
}
