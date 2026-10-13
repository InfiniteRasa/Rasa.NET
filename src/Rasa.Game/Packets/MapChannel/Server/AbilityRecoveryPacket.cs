using System.Collections.Generic;

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
        public enum HitDataKind { None, Damage, Heal, EffectAttach, CureLists, TypeIds, RawInfo, HealRepair, DamagePair }

        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public HitDataKind Kind { get; }
        public List<AbilityHit> Hits { get; } = new List<AbilityHit>();
        public List<ulong> Misses { get; } = new List<ulong>();

        /// <summary>For lightning: OnHitData is (arcData,), the arcs to draw. Empty for everything else.</summary>
        public bool ArcData { get; set; }

        /// <summary>
        /// For HitDataKind.CureLists: hitdata is not a list of per-hit entries at all but the pair
        /// CureAction.DoAbility unpacks - (reviveList, effectList), who was brought back and who
        /// the debuff guard goes on.
        /// </summary>
        public List<ulong> ReviveIds { get; } = new List<ulong>();
        public List<ulong> EffectIds { get; } = new List<ulong>();

        /// <summary>
        /// For HitDataKind.TypeIds: hitdata is a plain list of gameeffectdata ids, which
        /// PolymorphAction.DoAbility announces on the performer one by one.
        /// </summary>
        public List<int> TypeIds { get; } = new List<int>();

        public AbilityRecoveryPacket(ActionId actionId, uint actionArgId, HitDataKind kind)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Kind = kind;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);

            pw.WriteList(Hits.Count);
            foreach (var hit in Hits)
                pw.WriteULong(hit.EntityId);

            pw.WriteList(Misses.Count);
            foreach (var miss in Misses)
                pw.WriteULong(miss);

            pw.WriteList(0);                    // missdata

            if (Kind == HitDataKind.CureLists)
            {
                pw.WriteTuple(2);               // hitdata = (reviveList, effectList)
                pw.WriteList(ReviveIds.Count);

                foreach (var entityId in ReviveIds)
                    pw.WriteULong(entityId);

                pw.WriteList(EffectIds.Count);

                foreach (var entityId in EffectIds)
                    pw.WriteULong(entityId);

                return;
            }

            if (Kind == HitDataKind.TypeIds)
            {
                pw.WriteList(TypeIds.Count);

                foreach (var typeId in TypeIds)
                    pw.WriteInt(typeId);

                return;
            }

            pw.WriteList(Kind == HitDataKind.None ? 0 : Hits.Count);

            foreach (var hit in Hits)
            {
                switch (Kind)
                {
                    case HitDataKind.RawInfo:
                        // hitdata[i] is the rawInfo itself (CrabMineDeathAbility.DoAbility).
                        DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, hit.IsCritical, hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.WasImmune, targetEffectIds: hit.TargetEffectIds);
                        break;
                    case HitDataKind.Heal:
                        pw.WriteInt(hit.Amount);
                        break;
                    case HitDataKind.DamagePair:
                        // (clientInfo, extraClientInfo) - MiasmaCoalesceAbility.DoAbility: the
                        // hit, and its extra damage of another type or None.
                        pw.WriteTuple(2);
                        DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, hit.IsCritical, hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.WasImmune, targetEffectIds: hit.TargetEffectIds);
                        if (hit.Extra != null)
                            DamageInfoWriter.WriteRawInfo(pw, hit.Extra.DamageType, hit.Extra.Amount, hit.Extra.Resisted, hit.Extra.IsCritical, hit.Extra.DeathBlow, absorbed: hit.Extra.Absorbed, wasImmune: hit.Extra.WasImmune);
                        else
                            pw.WriteNoneStruct();
                        break;
                    case HitDataKind.HealRepair:
                        // (healAmount, repairAmount) - TechnicianHealAbility.DoAbility.
                        pw.WriteTuple(2);
                        pw.WriteInt(hit.Amount);
                        pw.WriteInt(hit.Repair);
                        break;
                    case HitDataKind.EffectAttach:
                        pw.WriteTuple(2);
                        pw.WriteULong(hit.EntityId);
                        pw.WriteInt(hit.EffectTypeId);
                        break;
                    case HitDataKind.Damage:
                        pw.WriteTuple(2);
                        DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, hit.IsCritical, hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.WasImmune, targetEffectIds: hit.TargetEffectIds);
                        if (ArcData)
                        {
                            // onHitData = (arcData,): [(entityId, rawInfo), ...], which
                            // LightningAbility.OnAbility hands to an ARC_EFFECT on the target -
                            // it floats each as damage and draws an arc to each.
                            pw.WriteTuple(1);
                            pw.WriteList(hit.Arcs.Count);

                            foreach (var arc in hit.Arcs)
                            {
                                pw.WriteTuple(2);
                                pw.WriteULong(arc.EntityId);
                                DamageInfoWriter.WriteRawInfo(pw, arc.DamageType, arc.Amount, arc.Resisted, arc.IsCritical, arc.DeathBlow, absorbed: arc.Absorbed, wasImmune: arc.WasImmune);
                            }
                        }
                        else
                            pw.WriteNoneStruct();           // onHitData
                        break;
                }
            }
        }
    }

    public class AbilityHit
    {
        public ulong EntityId { get; set; }
        /// <summary>What got through to armour and health: after resistance and after a shield.</summary>
        public int Amount { get; set; }
        public int Resisted { get; set; }

        /// <summary>What a shield on the target took of the hit (ActorManager.Damage).</summary>
        public int Absorbed { get; set; }

        /// <summary>The target was immune to the hit (Managers.DamageImmunity): "Immune" in place of the damage.</summary>
        public bool WasImmune { get; set; }

        /// <summary>For HitDataKind.HealRepair: the armour repaired, beside Amount healed.</summary>
        public int Repair { get; set; }

        public DamageType DamageType { get; set; }
        public bool IsCritical { get; set; }
        public bool DeathBlow { get; set; }

        /// <summary>For HitDataKind.DamagePair: the extra damage of another type the hit carries, or null for none.</summary>
        public AbilityHit Extra { get; set; }

        /// <summary>For HitDataKind.EffectAttach: the gameeffectdata id the client announces on this entity.</summary>
        public int EffectTypeId { get; set; }

        /// <summary>For lightning (ArcData): the further damage this hit sets off - arcs, extra damage - as the client's arcData.</summary>
        public List<TickEntry> Arcs { get; } = new List<TickEntry>();

        /// <summary>
        /// The types of the effects the hit put on its target quietly, for the client to announce
        /// as it plays the hit (Managers.HitEffects): in the rawInfo of a Damage, RawInfo or
        /// DamagePair entry, which the client's DoAbility hands to AnnounceDamage.
        /// </summary>
        public List<uint> TargetEffectIds { get; } = new List<uint>();
    }
}
