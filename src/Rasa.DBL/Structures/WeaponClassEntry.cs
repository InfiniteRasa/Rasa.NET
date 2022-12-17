using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class WeaponClassEntry
    {
        public uint ClassId { get; set; }
        public short WeaponTemplateid { get; set; }
        public short WeaponAttackActionId { get; set; }
        public uint WeaponAttackArgId { get; set; }
        public short DrawActionId { get; set; }
        public short StowActionId { get; set; }
        public short ReloadActionId { get; set; }
        public int AmmoClassId { get; set; }
        public short ClipSize { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public short DamageType { get; set; }
        public short Velocity { get; set; }
        public short WeaponAnimConditionCode { get; set; }
        public bool WindupOverride { get; set; }
        public bool RecoveryOverride { get; set; }
        public bool ReuseOverride { get; set; }
        public bool ReloadOverride { get; set; }
        public bool RangeType { get; set; }
        public bool UnkArg1 { get; set; }
        public bool UnkArg2 { get; set; }
        public bool UnkArg3 { get; set; }
        public bool UnkArg4 { get; set; }
        public bool UnkArg5 { get; set; }
        public short UnkArg6 { get; set; }
        public bool UnkArg7 { get; set; }
        public short UnkArg8 { get; set; }

        public static WeaponClassEntry Read(MySqlDataReader reader)
        {
            return new WeaponClassEntry
            {
                ClassId = reader.GetUInt32("classId"),
                WeaponTemplateid = reader.GetInt16("weaponTemplateid"),
                WeaponAttackActionId = reader.GetInt16("weaponAttackActionId"),
                WeaponAttackArgId = reader.GetUInt32("weaponAttackArgId"),
                DrawActionId = reader.GetInt16("drawActionId"),
                StowActionId = reader.GetInt16("stowActionId"),
                ReloadActionId = reader.GetInt16("reloadActionId"),
                AmmoClassId = reader.GetInt32("ammoClassId"),
                ClipSize = reader.GetInt16("clipSize"),
                MinDamage = reader.GetInt32("minDamage"),
                MaxDamage = reader.GetInt32("maxDamage"),
                DamageType = reader.GetInt16("damageType"),
                Velocity = reader.GetInt16("velocity"),
                WeaponAnimConditionCode = reader.GetInt16("weaponAnimConditionCode"),
                WindupOverride = reader.GetBoolean("windupOverride"),
                RecoveryOverride = reader.GetBoolean("recoveryOverride"),
                ReuseOverride = reader.GetBoolean("reuseOverride"),
                ReloadOverride = reader.GetBoolean("reloadOverride"),
                RangeType = reader.GetBoolean("rangeType"),
                UnkArg1 = reader.GetBoolean("unkArg1"),
                UnkArg2 = reader.GetBoolean("unkArg2"),
                UnkArg3 = reader.GetBoolean("unkArg3"),
                UnkArg4 = reader.GetBoolean("unkArg4"),
                UnkArg5 = reader.GetBoolean("unkArg5"),
                UnkArg6 = reader.GetInt16("unkArg6"),
                UnkArg7 = reader.GetBoolean("unkArg7"),
                UnkArg8 = reader.GetInt16("unkArg8"),
            };
        }
    }
}
