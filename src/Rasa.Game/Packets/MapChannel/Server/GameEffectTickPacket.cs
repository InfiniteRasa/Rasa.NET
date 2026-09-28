using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_GameEffectTick(effectId, *args): the effect did something, here is what. Sent on the
    /// entity that holds the effect; the client hands the arguments to the effect class's OnTick,
    /// and each class wants its own shape:
    ///
    ///  - DamageOverTime.OnTick(target, damageData): damageData is [(targetId, rawInfo), ...]
    ///    and the client announces each as damage from the effect's source (Ruin, the
    ///    Reconstruction harm effect).
    ///  - HealOverTime.OnTick(target, healData): [(targetId, amount), ...], announced as healing.
    ///  - RageSourceEffect.OnTick(target, targetIds): the ids of the squad members the aura just
    ///    reached, announced as attaches of the RAGE effect on them.
    ///  - StormEffect.OnTick(target, dotData, arcData): lightning P5's storm - dotData announced
    ///    as damage on the holder, arcData ([(targetId, rawInfo)]) floated and drawn as arcs from it.
    ///  - TrapDeathEffect.OnTick(target, killerId): the one who destroyed the trap.
    ///  - HortimonculusSourceEffect.OnTick(target, buffIds, healData): the squad members newly
    ///    given HORTIMONCULUS_BUFF (announced as its attach on each), then [(targetId, amount)]
    ///    healed by the plant, announced as healing from it.
    ///  - BaseGameEffect.OnTick(target): nothing but the tick marker on the effect's FX.
    ///
    /// Kind picks the shape. Entries is empty for the bare tick.
    /// </summary>
    public class GameEffectTickPacket : ServerPythonPacket
    {
        public enum TickKind { Bare, Damage, Heal, EntityIds, Storm, EntityId, Hortimonculus }

        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectTick;

        public int EffectId { get; }
        public TickKind Kind { get; }
        public List<TickEntry> Entries { get; } = new List<TickEntry>();

        /// <summary>For TickKind.Storm: the arcs, written after Entries (the damage on the holder).</summary>
        public List<TickEntry> ArcEntries { get; } = new List<TickEntry>();

        /// <summary>For TickKind.Hortimonculus: the buffIds, written before Entries (the healing).</summary>
        public List<ulong> BuffIds { get; } = new List<ulong>();

        public GameEffectTickPacket(int effectId, TickKind kind)
        {
            EffectId = effectId;
            Kind = kind;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(Kind == TickKind.Bare ? 1 : Kind == TickKind.Storm || Kind == TickKind.Hortimonculus ? 3 : 2);
            pw.WriteInt(EffectId);

            if (Kind == TickKind.Bare)
                return;

            // TrapDeathEffect.OnTick(target, killerId): one bare entity id.
            if (Kind == TickKind.EntityId)
            {
                pw.WriteULong(Entries.Count > 0 ? Entries[0].EntityId : 0);
                return;
            }

            if (Kind == TickKind.Hortimonculus)
            {
                pw.WriteList(BuffIds.Count);

                foreach (var buffId in BuffIds)
                    pw.WriteULong(buffId);

                WriteHeal(pw, Entries);
                return;
            }

            if (Kind == TickKind.Storm)
            {
                WriteDamage(pw, Entries);
                WriteDamage(pw, ArcEntries);
                return;
            }

            pw.WriteList(Entries.Count);

            foreach (var entry in Entries)
            {
                switch (Kind)
                {
                    case TickKind.Damage:
                        pw.WriteTuple(2);
                        pw.WriteULong(entry.EntityId);
                        DamageInfoWriter.WriteRawInfo(pw, entry.DamageType, entry.Amount, entry.Resisted, entry.IsCritical, entry.DeathBlow, absorbed: entry.Absorbed);
                        break;
                    case TickKind.Heal:
                        pw.WriteTuple(2);
                        pw.WriteULong(entry.EntityId);
                        pw.WriteInt(entry.Amount);
                        break;
                    case TickKind.EntityIds:
                        pw.WriteULong(entry.EntityId);
                        break;
                }
            }
        }

        private static void WriteHeal(PythonWriter pw, List<TickEntry> entries)
        {
            pw.WriteList(entries.Count);

            foreach (var entry in entries)
            {
                pw.WriteTuple(2);
                pw.WriteULong(entry.EntityId);
                pw.WriteInt(entry.Amount);
            }
        }

        private static void WriteDamage(PythonWriter pw, List<TickEntry> entries)
        {
            pw.WriteList(entries.Count);

            foreach (var entry in entries)
            {
                pw.WriteTuple(2);
                pw.WriteULong(entry.EntityId);
                DamageInfoWriter.WriteRawInfo(pw, entry.DamageType, entry.Amount, entry.Resisted, entry.IsCritical, entry.DeathBlow, absorbed: entry.Absorbed);
            }
        }
    }

    public class TickEntry
    {
        public ulong EntityId { get; set; }

        /// <summary>What got through to armour and health: after resistance and after a shield.</summary>
        public int Amount { get; set; }
        public int Resisted { get; set; }

        /// <summary>What a shield on the target took of the hit (ActorManager.Damage).</summary>
        public int Absorbed { get; set; }
        public DamageType DamageType { get; set; }
        public bool IsCritical { get; set; }
        public bool DeathBlow { get; set; }
    }
}
