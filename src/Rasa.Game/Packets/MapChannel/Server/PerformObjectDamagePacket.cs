using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_PerformObjectAbility for OBJ_DAMAGE (102): an object blows up, plays the FX family of
    /// its arg (OBJ_DAMAGE_EXPLOSION_SMALL 1, _LARGE 2, _LIGHTNING 3, _MINE 4, _BOMB 5,
    /// _ARIEKI_GEYSER_ERUPTION 6, _ARIEKI_HORNITO_ERUPTION 7) and hurts whoever it hit. The
    /// client's ObjDamageBase.DoAbility(actor, hitdata, sourceId) takes, after the hits and the
    /// misses, the damage of each hit (shared/damageinfo.py rawInfo, DamageInfoWriter) and the id
    /// of whoever is to be named as its source, and announces each on its target.
    /// </summary>
    public class PerformObjectDamagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformObjectAbility;

        public uint ActionArgId { get; }
        public ulong SourceId { get; }
        public List<Hit> Hits { get; } = new List<Hit>();

        public sealed class Hit
        {
            public ulong TargetId { get; init; }
            public DamageType DamageType { get; init; }
            public long Amount { get; init; }
            public int Resisted { get; init; }
            public int Absorbed { get; init; }
            public bool WasImmune { get; init; }
            public bool DeathBlow { get; init; }
        }

        public PerformObjectDamagePacket(uint actionArgId, ulong sourceId)
        {
            ActionArgId = actionArgId;
            SourceId = sourceId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId.ObjDamage);
            pw.WriteUInt(ActionArgId);

            pw.WriteList(Hits.Count);
            foreach (var hit in Hits)
                pw.WriteULong(hit.TargetId);

            pw.WriteList(0);

            pw.WriteList(Hits.Count);
            foreach (var hit in Hits)
                DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, deathBlow: hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.WasImmune);

            pw.WriteULong(SourceId);
        }
    }
}
