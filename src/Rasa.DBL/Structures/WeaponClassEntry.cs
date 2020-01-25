using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class WeaponClassEntry
    {
        public uint ClassId { get; set; }
        public short WeaponTemplateid { get; set; }
        public short WeaponAttackActionId { get; set; }
        public short WeaponAttackArgId { get; set; }
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
    }
}
