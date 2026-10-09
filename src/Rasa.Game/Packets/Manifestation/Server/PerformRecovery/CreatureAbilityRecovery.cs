namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// The shape each entry of a recovery's hitdata takes, which is whatever the client class of
    /// the action unpacks - it is not the same for every class that extends TargetedAction:
    ///
    ///  - Weapon: (entityId, rawInfo, onHitData), BaseWeaponAttack.DoHits - weapon pairs;
    ///  - RawInfo: the bare rawInfo, indexed by hit - KaelSmashAbility and nearly every other
    ///    creature class under client/actions/abilities/ai;
    ///  - Damage: (rawInfo, onHitData), DamageBase.DoAbility - the player modules some creature
    ///    actions use (abilities.knockback, .stun, .tectonicstrike, .shrapnel, .deathdamage);
    ///  - DamageArcs: the same with onHitData (arcData,), which LightningAbility.OnAbility unpacks;
    ///  - EntityRawInfo: (entityId, rawInfo), KaelRushingBlowAbility.DoAbility;
    ///  - Drain: (rawInfo, powerAmount, healAmount), LinkerHandBlastAbility.DoAbility.
    /// </summary>
    public enum RecoveryShape { Weapon, RawInfo, Damage, DamageArcs, EntityRawInfo, Drain }

    /// <summary>
    /// PerformRecovery for a creature attack whose client class is an ability rather than a
    /// weapon attack: the missile's hits and misses as WeaponAttackRecovery writes them, with each
    /// hitdata entry in the shape the class unpacks (RecoveryShape). Sent in the weapon's shape, a
    /// creature ability's DoAbility raised unpacking it - "too many values to unpack" - after the
    /// animation and FX had played, so the damage landed but its number never showed and nothing
    /// after it in OnServerResolution ran.
    /// </summary>
    public class CreatureAbilityRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public Missile Missile { get; }
        public RecoveryShape Shape { get; }

        public CreatureAbilityRecovery(Missile missile, RecoveryShape shape)
        {
            Missile = missile;
            Shape = shape;
        }

        public override void Write(PythonWriter pw)
        {
            var args = Missile.Args;

            pw.WriteTuple(6);
            pw.WriteUInt((uint)Missile.ActionId);
            pw.WriteUInt(Missile.ActionArgId);

            pw.WriteList(args.HitEntities.Count);
            foreach (var entity in args.HitEntities)
                pw.WriteULong(entity);

            pw.WriteList(args.MisstEntities.Count);
            foreach (var entity in args.MisstEntities)
                pw.WriteULong(entity);

            DamageInfoWriter.WriteMissTypes(pw, args.Missdata);

            pw.WriteList(args.HitData.Count);
            foreach (var hit in args.HitData)
            {
                switch (Shape)
                {
                    case RecoveryShape.RawInfo:
                        WriteRawInfo(pw, hit);
                        break;
                    case RecoveryShape.Damage:
                        pw.WriteTuple(2);
                        WriteRawInfo(pw, hit);
                        pw.WriteNoneStruct();       // onHitData
                        break;
                    case RecoveryShape.DamageArcs:
                        pw.WriteTuple(2);
                        WriteRawInfo(pw, hit);
                        pw.WriteTuple(1);           // onHitData = (arcData,): [(entityId, rawInfo), ...]
                        pw.WriteList(hit.Arcs.Count);

                        foreach (var arc in hit.Arcs)
                        {
                            pw.WriteTuple(2);
                            pw.WriteULong(arc.EntityId);
                            DamageInfoWriter.WriteRawInfo(pw, arc.DamageType, arc.Amount, arc.Resisted, arc.IsCritical, arc.DeathBlow, absorbed: arc.Absorbed, wasImmune: arc.WasImmune);
                        }
                        break;
                    case RecoveryShape.EntityRawInfo:
                        pw.WriteTuple(2);
                        pw.WriteULong(hit.EntityId);
                        WriteRawInfo(pw, hit);
                        break;
                    case RecoveryShape.Drain:
                        pw.WriteTuple(3);
                        WriteRawInfo(pw, hit);
                        pw.WriteInt(hit.PowerDrained);  // powerAmount, floated off the player hit
                        pw.WriteInt(hit.Healed);        // healAmount, summed and floated on the Linker
                        break;
                    default:
                        pw.WriteTuple(3);
                        pw.WriteULong(hit.EntityId);
                        WriteRawInfo(pw, hit);
                        pw.WriteTuple(1);
                        pw.WriteList(0);
                        break;
                }
            }
        }

        private void WriteRawInfo(PythonWriter pw, HitData hit)
        {
            var type = hit.DamageType != 0 ? hit.DamageType : Missile.DamageType;

            DamageInfoWriter.WriteRawInfo(pw, hit, type == 0 ? DamageType.Physical : type);
        }
    }
}
