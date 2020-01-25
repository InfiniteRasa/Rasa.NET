using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class WeaponTemplateEntry
    {
        public uint ItemTemplateId { get; set; }
        public double AimRate { get; set; }
        public int ReloadTime { get; set; }
        public int AltActionId { get; set; }
        public int AltActionArg { get; set; }
        public int AEType { get; set; }
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
        public int AltMaxDamage { get; set; }
        public int AltDamageType { get; set; }
        public int AltRange { get; set; }
        public int AltAERadius { get; set; }
        public int AltAEType { get; set; }
        public int AttackType { get; set; }
    }
}
