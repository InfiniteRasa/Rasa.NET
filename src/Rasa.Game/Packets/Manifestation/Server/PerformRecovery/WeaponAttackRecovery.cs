namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using Data;
    using Memory;
    using Structures;

    public class WeaponAttackRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public Missile Missile { get; set; }

        public WeaponAttackRecovery(Missile missile)
        {
            Missile = missile;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)Missile.ActionId);   // actionId
            pw.WriteUInt(Missile.ActionArgId);      // actionargId
            pw.WriteList(Missile.Args.HitEntities.Count);    // Hits
            foreach (var entity in Missile.Args.HitEntities)
                pw.WriteULong(entity);
            pw.WriteList(Missile.Args.MisstEntities.Count);  // misses
            foreach (var entity in Missile.Args.MisstEntities)
                pw.WriteULong(entity);
            pw.WriteList(Missile.Args.Missdata.Count);       // missdata: a misstype for each miss
            foreach (var missType in Missile.Args.Missdata)
                pw.WriteUInt(missType);
            pw.WriteList(Missile.Args.HitData.Count);
            foreach (var hit in Missile.Args.HitData)
            {
                pw.WriteTuple(3);
                pw.WriteULong(hit.EntityId);         // target entityid
                pw.WriteTuple(12);              // rawinfo start
                    pw.WriteUInt((uint)TypeOf(hit));    // self.damagetype
                    pw.WriteUInt(hit.Reflected);        // self.reflected
                    pw.WriteUInt(hit.Filtered);         // self.filtered
                    pw.WriteUInt(hit.Absorbed);         // self.absorbed
                    pw.WriteUInt(hit.Resisted);         // self.resisted
                    pw.WriteLong(hit.FinalAmt);         // self.finalamt: each hit its own (a launcher's splash)
                    pw.WriteInt(hit.IsCritical);        // self.iscrit
                    pw.WriteInt(hit.DeathBlow);         // self.deathblow
                    pw.WriteDouble(hit.CoverModifier);    // self.covermodifier
                    pw.WriteInt(hit.WasImune);          // self.wasimmune
                    pw.WriteList(0);                    // todo: targeteffectids
                    pw.WriteList(0);                    // todo: sourceeffectids
                pw.WriteTuple(1);                   // OnHitData
                pw.WriteList(0);
            }
        }

        private DamageType TypeOf(HitData hit)
        {
            var type = hit.DamageType != 0 ? hit.DamageType : Missile.DamageType;

            return type == 0 ? DamageType.Physical : type;
        }
    }
}
