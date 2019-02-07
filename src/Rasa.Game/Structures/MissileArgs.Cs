using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class MissileArgs
    {
        public List<uint> HitEntities = new List<uint>();
        public List<uint> MisstEntities = new List<uint>();
        public List<HitData> HitData = new List<HitData>();
        public List<uint> Missdata = new List<uint>();

        public MissileArgs()
        {
        }
    }

    public class HitData
    {
        public uint EntityId { get; set; }
        public DamageType DamageType { get; set; }
        public uint Reflected { get; set; }
        public uint Filtered { get; set; }
        public uint Absorbed { get; set; }
        public uint Resisted { get; set; }
        public long FinalAmt { get; set; }
        public bool IsCritical { get; set; }
        public uint DeathBlow { get; set; }
        public uint CoverModifier { get; set; }
        public bool WasImune { get; set; }
        public List<uint> TargetEffectIds { get; set; } // ToDo
        public List<uint> SourceEffectIds { get; set; } // ToDo
        public object OnHitData { get; set; }           // ToDo
    }
}
