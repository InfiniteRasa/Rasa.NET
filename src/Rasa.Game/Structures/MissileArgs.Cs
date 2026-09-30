using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class MissileArgs
    {
        public List<ulong> HitEntities = new List<ulong>();
        public List<ulong> MisstEntities = new List<ulong>();
        public List<HitData> HitData = new List<HitData>();
        public List<uint> Missdata = new List<uint>();

        public MissileArgs()
        {
        }
    }

    public class HitData
    {
        public ulong EntityId { get; set; }
        public DamageType DamageType { get; set; }
        public uint Reflected { get; set; }
        public uint Filtered { get; set; }
        public uint Absorbed { get; set; }
        public uint Resisted { get; set; }
        public long FinalAmt { get; set; }
        public int IsCritical { get; set; }
        public int DeathBlow { get; set; }
        /// <summary>The share of the hit that got past cover (Managers.Cover): 1.0 for none in the way; the client colours its hit display by it.</summary>
        public double CoverModifier { get; set; } = 1.0;
        public int WasImune { get; set; }
        public List<uint> TargetEffectIds { get; set; } // ToDo
        public List<uint> SourceEffectIds { get; set; } // ToDo
        public List<object> OnHitData { get; set; }           // ToDo
        /// <summary>A creature's lightning: the arcs and extra damage this hit set off, the arcData its onHitData carries (CreatureLightning).</summary>
        public List<Packets.MapChannel.Server.TickEntry> Arcs { get; } = new List<Packets.MapChannel.Server.TickEntry>();

        /// <summary>A Linker's hand blast: the power it took off the player hit, and what it healed itself for (the Drain recovery shape).</summary>
        public int PowerDrained { get; set; }
        public int Healed { get; set; }
    }
}
