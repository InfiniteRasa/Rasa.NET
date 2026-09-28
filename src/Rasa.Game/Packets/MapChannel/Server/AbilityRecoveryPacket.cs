using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// PerformRecovery(actionId, actionArgId, hits, misses, missdata, hitdata) for an ability:
    /// the server has resolved it, here is who it hit and what it did to them. Sent on the
    /// performer's entity to everyone who can see them, the performer included - their client's
    /// DoAction runs the ability's own DoAbility over these lists to show the numbers.
    ///
    /// The shape of each hitdata entry is the ability class's business (client/actions/
    /// abilities): damage abilities (DamageBase) want (rawInfo, onHitData) where rawInfo is the
    /// same 12-tuple a weapon hit carries; heals (HealBase, HealAbility) want a bare amount;
    /// abilities that attach effects and name them (ReconstructionAction) want
    /// (entityId, effectTypeId) pairs, which their DoAbility announces. Kind picks which. hits
    /// and hitdata must be the same length - DamageBase.DoAbility indexes one with the other.
    ///
    /// Buff abilities with no DoAbility of their own still need their targets in hits:
    /// TargetedAction.OnServerResolution announces the class's targetGameEffect on every hit and
    /// its sourceGameEffect on the performer when there was at least one, and that announcement
    /// is what plays the effect's attach FX and shows its icon.
    /// </summary>
    public class AbilityRecoveryPacket : ServerPythonPacket
    {
        public enum HitDataKind { None, Damage, Heal, EffectAttach }

        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public HitDataKind Kind { get; }
        public List<AbilityHit> Hits { get; } = new List<AbilityHit>();
        public List<ulong> Misses { get; } = new List<ulong>();

        /// <summary>For lightning: OnHitData is (arcData,), the arcs to draw. Empty for everything else.</summary>
        public bool ArcData { get; set; }
        private AbilityHit[] _capturedHits;
        private ulong[] _capturedMisses;

        public AbilityRecoveryPacket(ActionId actionId, uint actionArgId, HitDataKind kind)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Kind = kind;
        }

        public override void Write(PythonWriter pw)
        {
            _capturedHits ??= Hits.Select(AbilityHit.Capture).ToArray();
            _capturedMisses ??= Misses.ToArray();

            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);

            pw.WriteList(_capturedHits.Length);
            foreach (var hit in _capturedHits)
                pw.WriteULong(hit.EntityId);

            pw.WriteList(_capturedMisses.Length);
            foreach (var miss in _capturedMisses)
                pw.WriteULong(miss);

            pw.WriteList(0);                    // missdata

            pw.WriteList(Kind == HitDataKind.None ? 0 : _capturedHits.Length);

            foreach (var hit in _capturedHits)
            {
                switch (Kind)
                {
                    case HitDataKind.Heal:
                        pw.WriteInt(hit.Amount);
                        break;
                    case HitDataKind.EffectAttach:
                        pw.WriteTuple(2);
                        pw.WriteULong(hit.EntityId);
                        pw.WriteInt(hit.EffectTypeId);
                        break;
                    case HitDataKind.Damage:
                        pw.WriteTuple(2);
                        WriteRaw(pw, hit);
                        if (ArcData)
                        {
                            pw.WriteTuple(1);               // onHitData = (arcData,)
                            pw.WriteList(hit.Arcs.Count);
                            foreach (var arc in hit.Arcs)
                            {
                                pw.WriteTuple(2);
                                pw.WriteULong(arc.EntityId);
                                WriteRaw(pw, arc);
                            }
                        }
                        else
                            pw.WriteNoneStruct();           // onHitData
                        break;
                }
            }
        }

        private static void WriteRaw(PythonWriter pw, AbilityHit hit)
        {
            DamageInfoWriter.WriteRawInfo(
                pw,
                hit.DamageType,
                hit.Amount,
                hit.Resisted,
                hit.IsCritical,
                hit.DeathBlow);
        }
    }

    public class AbilityHit
    {
        public ulong EntityId { get; set; }
        public int Amount { get; set; }
        public int Resisted { get; set; }
        public DamageType DamageType { get; set; }
        public bool IsCritical { get; set; }
        public bool DeathBlow { get; set; }
        public List<AbilityHit> Arcs { get; } = new List<AbilityHit>();

        internal static AbilityHit Capture(AbilityHit source)
        {
            var captured = new AbilityHit
            {
                EntityId = source.EntityId,
                Amount = source.Amount,
                Resisted = source.Resisted,
                DamageType = source.DamageType,
                IsCritical = source.IsCritical,
                DeathBlow = source.DeathBlow,
                EffectTypeId = source.EffectTypeId
            };

            foreach (var arc in source.Arcs)
                captured.Arcs.Add(Capture(arc));

            return captured;
        }

        /// <summary>For HitDataKind.EffectAttach: the gameeffectdata id the client announces on this entity.</summary>
        public int EffectTypeId { get; set; }
    }
}
